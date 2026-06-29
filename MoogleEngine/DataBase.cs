using System.Text;
using System.Text.RegularExpressions;

public class DataBase
{
  public MoogleEngine.InvertedIndex Index { get; private set; }

  public DataBase()
  {
    string ruta = Directory.GetParent(Path.Combine(Directory.GetCurrentDirectory()))!.FullName!;
    string file = Path.Combine(ruta, "Content");
    if (!Directory.Exists(file))
      throw new DirectoryNotFoundException($"La carpeta 'Content' no existe. Se esperaba en: {file}");

    string[] directions = Directory.GetFiles(file, "*txt*", SearchOption.AllDirectories);
    if (directions.Length == 0)
      throw new InvalidOperationException($"No se encontraron archivos .txt en la carpeta 'Content' ({file}). Agrega documentos de texto antes de ejecutar la aplicación.");

    Index = MoogleEngine.InvertedIndex.Build(directions);
  }

  public static string[] NormalizeString(string content)
  {
    return MoogleEngine.InvertedIndex.ProcessText(content);
  }

  static readonly Regex nonAlphaRegex = new("[^a-zA-Z0-9 -]");
  public static string NormalizeExpresion(string content)
  {
    content = content.ToLower();
    content = nonAlphaRegex.Replace(content.Normalize(NormalizationForm.FormD), "");
    return content;
  }

  public Tuple<MoogleEngine.SearchItem[], string> Query(string query)
  {
    var idx = Index;
    string[] rawWords = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

    // 1. Normalize and stem
    string[] stemmed = NormalizeString(query);
    if (stemmed.Length == 0)
      return NoResults();

    // 2. Build query weights: map wordId → weight
    var queryWeights = new Dictionary<int, double>();
    var unknownWords = new List<string>();
    var suggestionInfo = new Dictionary<string, string>();

    foreach (var w in stemmed)
    {
      if (idx.WordToId.TryGetValue(w, out int wid))
        queryWeights[wid] = queryWeights.GetValueOrDefault(wid) + 1.0;
      else
        unknownWords.Add(w);
    }

    // 3. Suggestions for unknown words
    string suggestion = "";
    if (unknownWords.Count > 0)
    {
      string[] vocab = idx.Vocabulary.ToArray();
      var sugResult = Auxiliaries.Suggestion(query, unknownWords.ToArray(), vocab, 3);
      suggestion = sugResult.Item1;
      suggestionInfo = sugResult.Item2;
      foreach (var kvp in suggestionInfo)
      {
        if (idx.WordToId.TryGetValue(kvp.Value, out int sugWid))
          queryWeights[sugWid] = queryWeights.GetValueOrDefault(sugWid) + 1.0;
      }
    }

    // 4. Synonym expansion
    int totalDocs = idx.DocCount;
    foreach (var raw in rawWords)
    {
      string clean = NormalizeExpresion(raw);
      var syns = MoogleEngine.Synonyms.Get(clean);
      if (syns == null) continue;

      string stemSyn = SpanishStemmer.Stem(clean);
      if (!idx.WordToId.TryGetValue(stemSyn, out int origWid)) continue;

      double origIdf = idx.Idf[origWid];
      double groupWeight = 0.3 / syns.Length;
      foreach (var syn in syns)
      {
        string synStem = SpanishStemmer.Stem(syn);
        if (idx.WordToId.TryGetValue(synStem, out int synWid) && synWid != origWid)
        {
          double idfRatio = Math.Min(idx.Idf[synWid] / Math.Max(origIdf, 0.001), 1.0);
          queryWeights[synWid] = queryWeights.GetValueOrDefault(synWid) + groupWeight * idfRatio;
        }
      }
    }

    if (queryWeights.Count == 0)
      return NoResults(suggestion);

    // 5. Apply * operator multiplier
    ApplyAsterisk(query, suggestionInfo, queryWeights, idx);

    // 6. Score candidate documents using inverted index (sparse)
    var scores = new Dictionary<int, double>();
    int[] queryWordIds = queryWeights.Keys.ToArray();
    foreach (int wid in queryWordIds)
    {
      double weight = queryWeights[wid];
      var postings = idx.Postings[wid];
      for (int p = 0; p < postings.Count; p += 2)
      {
        int docId = postings[p];
        int freq = postings[p + 1];
        double tf = (double)freq / idx.DocLengths[docId];
        scores[docId] = scores.GetValueOrDefault(docId) + weight * tf * idx.Idf[wid];
      }
    }

    if (scores.Count == 0)
      return NoResults(suggestion);

    // 7. Parse ! and ^ operators from the raw query
    var excludeWords = ParseOpWords(query, @"\!\w+\b", suggestionInfo);
    var includeWords = ParseOpWords(query, @"\^\w+\b", suggestionInfo);
    var closePairs = ParseCloseQuery(query, suggestionInfo);

    // 8. Filter and finalize candidates
    var filtered = new List<(int docId, double score)>();

    foreach (var kvp in scores)
    {
      int docId = kvp.Key;
      double score = kvp.Value;

      // Normalize by query length
      score /= stemmed.Length;

      // ! operator: exclude docs that contain the excluded word
      bool excluded = false;
      foreach (string ew in excludeWords)
      {
        if (idx.WordToId.TryGetValue(ew, out int ewId))
        {
          var ep = idx.Postings[ewId];
          for (int pi = 0; pi < ep.Count; pi += 2)
          {
            if (ep[pi] == docId) { excluded = true; break; }
          }
        }
        if (excluded) break;
      }
      if (excluded) continue;

      // ^ operator: only keep docs that contain all required words
      bool hasAll = true;
      foreach (string iw in includeWords)
      {
        if (!idx.WordToId.TryGetValue(iw, out int iwId)) { hasAll = false; break; }
        var ip = idx.Postings[iwId];
        bool found = false;
        for (int pi = 0; pi < ip.Count; pi += 2)
        {
          if (ip[pi] == docId) { found = true; break; }
        }
        if (!found) { hasAll = false; break; }
      }
      if (!hasAll) continue;

      // ~ proximity bonus
      if (closePairs.Count > 0)
        score += ComputeProximityBonus(closePairs, idx.DocPaths[docId]);

      filtered.Add((docId, score));
    }

    if (filtered.Count == 0)
      return NoResults(suggestion);

    // 9. Sort by score descending
    filtered.Sort((a, b) => b.score.CompareTo(a.score));

    // 10. Build SearchItems with snippets (re-read top docs from disk)
    var items = new List<MoogleEngine.SearchItem>();
    int maxResults = Math.Min(filtered.Count, 30);
    for (int i = 0; i < maxResults; i++)
    {
      int docId = filtered[i].docId;
      string title = idx.DocNames[docId];
      string[] docWords = File.ReadAllText(idx.DocPaths[docId])
          .ToLowerInvariant()
          .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

      // Build important-words set for snippet from query weights
      var importantWords = new HashSet<string>();
      foreach (int wid in queryWeights.Keys)
        importantWords.Add(idx.Vocabulary[wid]);
      string snippet = Auxiliaries.Snippet(query, docWords, idx.DocPaths, importantWords);
      items.Add(new MoogleEngine.SearchItem(title, snippet, (float)filtered[i].score));
    }

    string sug = suggestion ?? "";

    return Tuple.Create(items.ToArray(), sug);
  }

  static Tuple<MoogleEngine.SearchItem[], string> NoResults(string suggestion = "")
  {
    string sug = suggestion ?? "";
    return Tuple.Create(
      new[] { new MoogleEngine.SearchItem("No se encontraron textos coincidentes", "Por favor realice una busqueda diferente", 0f) },
      sug
    );
  }

  // Parse ! or ^ operators: returns stemmed words (with suggestion substitution)
  static HashSet<string> ParseOpWords(string search, string pattern, Dictionary<string, string> wordstochange)
  {
    var result = new HashSet<string>();
    Match m = Regex.Match(search, pattern);
    while (m.Success)
    {
      string clean = NormalizeExpresion(m.Value);
      string stemmed = SpanishStemmer.Stem(clean);
      if (wordstochange.TryGetValue(clean, out string? sug))
        stemmed = SpanishStemmer.Stem(sug);
      result.Add(stemmed);
      m = m.NextMatch();
    }
    return result;
  }

  // Parse * operator: multiply weights in the queryWeights dictionary
  static void ApplyAsterisk(string search, Dictionary<string, string> wordstochange, Dictionary<int, double> queryWeights, MoogleEngine.InvertedIndex idx)
  {
    Match m = Regex.Match(search, @"\*+\w+\b");
    while (m.Success)
    {
      int starCount = m.Value.Count(c => c == '*');
      string clean = NormalizeExpresion(m.Value);
      string stemmed = SpanishStemmer.Stem(clean);
      if (wordstochange.TryGetValue(clean, out string? sug))
        stemmed = SpanishStemmer.Stem(sug);
      if (idx.WordToId.TryGetValue(stemmed, out int wid))
        queryWeights[wid] = queryWeights.GetValueOrDefault(wid) * starCount * 2;
      m = m.NextMatch();
    }
  }

  // Parse ~ operator: returns list of (word1, word2) pairs (stemmed)
  static List<(string, string)> ParseCloseQuery(string search, Dictionary<string, string> wordstochange)
  {
    var result = new List<(string, string)>();
    string[] parts = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

    // Apply suggestion substitution
    for (int k = 0; k < parts.Length; k++)
    {
      string clean = NormalizeExpresion(parts[k]);
      if (wordstochange.TryGetValue(clean, out string? sug))
        parts[k] = sug;
    }

    for (int i = 0; i < parts.Length; i++)
    {
      if (parts[i] == "~" && i > 0 && i < parts.Length - 1)
      {
        string w1 = SpanishStemmer.Stem(NormalizeExpresion(parts[i - 1]));
        string w2 = SpanishStemmer.Stem(NormalizeExpresion(parts[i + 1]));
        if (w1 != w2)
          result.Add((w1, w2));
      }
    }
    return result;
  }

  // Proximity bonus: re-read doc from disk and compute min distance
  static double ComputeProximityBonus(List<(string, string)> pairs, string docPath)
  {
    string[] words = MoogleEngine.InvertedIndex.ProcessText(File.ReadAllText(docPath));
    double totalBonus = 0;

    foreach (var (w1, w2) in pairs)
    {
      int minDist = int.MaxValue;
      for (int i = 0; i < words.Length; i++)
      {
        if (words[i] == w1)
        {
          for (int j = 0; j < words.Length; j++)
          {
            if (words[j] == w2 && i != j)
            {
              int dist = Math.Abs(i - j);
              if (dist < minDist) minDist = dist;
            }
          }
        }
      }
      if (minDist < int.MaxValue)
        totalBonus += 2.0 / (minDist + 4);
    }
    return totalBonus;
  }
}
