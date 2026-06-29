using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace MoogleEngine;

public partial class DataBase : IDataBase, IDisposable
{
  volatile IInvertedIndex? _index;
  public IInvertedIndex Index
  {
    get => _index ?? throw new InvalidOperationException("Index not initialized");
    private set => _index = value;
  }

  readonly ILogger<DataBase>? _logger;
  readonly IStemmer _stemmer;
  readonly string? _contentPath;

  const int CacheMaxSize = 100;
  readonly ConcurrentDictionary<string, LinkedListNode<CacheEntry>> _cache = new();
  readonly LinkedList<CacheEntry> _cacheList = new();
  readonly object _cacheLock = new();

  FileSystemWatcher? _watcher;
  Timer? _rebuildTimer;
  readonly object _rebuildLock = new();
  bool _disposed;
  private static readonly char[] separator = new[] { ' ' };

  record CacheEntry(string Query, Tuple<SearchItem[], string> Result);

  Tuple<SearchItem[], string>? TryCache(string query)
  {
    lock (_cacheLock)
    {
      if (_cache.TryGetValue(query, out var node))
      {
        _cacheList.Remove(node);
        _cacheList.AddFirst(node);
        return node.Value.Result;
      }
    }
    return null;
  }

  void AddCache(string query, Tuple<SearchItem[], string> result)
  {
    lock (_cacheLock)
    {
      if (_cache.TryGetValue(query, out var existingNode))
      {
        _cacheList.Remove(existingNode);
        _cache.TryRemove(query, out _);
      }

      var node = _cacheList.AddFirst(new CacheEntry(query, result));
      _cache[query] = node;

      if (_cacheList.Count > CacheMaxSize)
      {
        var last = _cacheList.Last;
        if (last != null)
        {
          _cache.TryRemove(last.Value.Query, out _);
          _cacheList.RemoveLast();
        }
      }
    }
  }

  public DataBase() : this(FindContentPath(), null, new Stemmer()) { }

  public DataBase(ILogger<DataBase>? logger) : this(FindContentPath(), logger, new Stemmer()) { }

  public DataBase(string contentPath) : this(contentPath, null, new Stemmer()) { }

  public DataBase(string contentPath, ILogger<DataBase>? logger)
    : this(contentPath, logger, new Stemmer()) { }

  public DataBase(IInvertedIndex index, IStemmer? stemmer = null, ILogger<DataBase>? logger = null)
  {
    _stemmer = stemmer ?? new Stemmer();
    _logger = logger;
    Index = index;
    _logger?.LogInformation("Indexed {VocabCount} terms from {DocCount} docs", Index.VocabCount, Index.DocCount);
  }

  DataBase(string contentPath, ILogger<DataBase>? logger, IStemmer stemmer)
  {
    _stemmer = stemmer;
    _logger = logger;

    if (!Directory.Exists(contentPath))
    {
      _logger?.LogError("Content directory not found at {ContentPath}", contentPath);
      throw new DirectoryNotFoundException($"Content directory not found at: {contentPath}");
    }

    string[] files = Directory.GetFiles(contentPath, "*txt*", SearchOption.AllDirectories);
    if (files.Length == 0)
    {
      _logger?.LogError("No .txt files found in Content directory at {ContentPath}", contentPath);
      throw new InvalidOperationException($"No .txt files found in Content directory ({contentPath}). Add text documents before running the application.");
    }

    _logger?.LogInformation("Indexing {FileCount} documents from {ContentPath}", files.Length, contentPath);
    var sw = System.Diagnostics.Stopwatch.StartNew();
    Index = InvertedIndex.Build(files);
    sw.Stop();
    _logger?.LogInformation("Indexed {VocabCount} terms from {FileCount} docs in {ElapsedMs}ms",
      Index.VocabCount, files.Length, sw.ElapsedMilliseconds);

    _contentPath = contentPath;
    StartWatching();
  }

  void StartWatching()
  {
    if (_contentPath == null || !Directory.Exists(_contentPath)) return;

    _watcher = new FileSystemWatcher(_contentPath, "*txt*")
    {
      NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
      IncludeSubdirectories = false,
      EnableRaisingEvents = true
    };

    _watcher.Changed += OnContentChanged;
    _watcher.Created += OnContentChanged;
    _watcher.Deleted += OnContentChanged;
    _watcher.Renamed += OnContentChanged;

    _logger?.LogInformation("Watching Content directory for changes: {ContentPath}", _contentPath);
  }

  void OnContentChanged(object sender, FileSystemEventArgs e)
  {
    lock (_rebuildLock)
    {
      _rebuildTimer?.Dispose();
      _rebuildTimer = new Timer(_ => RebuildIndex(), null, TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);
    }
  }

  void RebuildIndex()
  {
    if (_contentPath == null) return;
    try
    {
      _logger?.LogInformation("Content changed — rebuilding index");
      var sw = System.Diagnostics.Stopwatch.StartNew();
      string[] files = Directory.GetFiles(_contentPath, "*txt*", SearchOption.AllDirectories);
      if (files.Length == 0)
      {
        _logger?.LogWarning("No .txt files after content change — index left unchanged");
        return;
      }
      var newIndex = InvertedIndex.Build(files);
      sw.Stop();
      Index = newIndex;
      lock (_cacheLock)
      {
        _cache.Clear();
        _cacheList.Clear();
      }
      _logger?.LogInformation("Re-indexed {VocabCount} terms from {FileCount} docs in {ElapsedMs}ms",
        Index.VocabCount, files.Length, sw.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
      _logger?.LogError(ex, "Failed to rebuild index after content change");
    }
  }

  static string FindContentPath()
  {
    string dir = Directory.GetParent(Path.Combine(Directory.GetCurrentDirectory()))!.FullName!;
    return Path.Combine(dir, "Content");
  }

  public static string[] NormalizeString(string content)
  {
    return InvertedIndex.ProcessText(content);
  }

  public static string NormalizeExpression(string content)
  {
    content = content.ToLower();
    content = InvertedIndex.NonAlphaRegex.Replace(content.Normalize(NormalizationForm.FormD), "");
    return content;
  }

  public Tuple<SearchItem[], string> Query(string query)
  {
    ArgumentNullException.ThrowIfNull(query);

    _logger?.LogDebug("Query: {Query}", query);

    var cached = TryCache(query);
    if (cached != null)
    {
      _logger?.LogDebug("Cache hit for: {Query}", query);
      return cached;
    }

    var sw = System.Diagnostics.Stopwatch.StartNew();
    var idx = Index;
    string[] rawWords = query.Split(separator, StringSplitOptions.RemoveEmptyEntries);

    string[] stemmed = NormalizeString(query);
    if (stemmed.Length == 0)
      return NoResults();

    // Build query weights from stemmed words
    var (Weights, UnknownWords) = BuildQueryWeights(stemmed, idx);

    // Suggestions for unknown words
    string suggestion = "";
    var suggestionInfo = new Dictionary<string, string>();
    if (UnknownWords.Count > 0)
    {
      string[] vocab = [.. idx.Vocabulary];
      var sugResult = Auxiliaries.Suggestion(query, [.. UnknownWords], vocab, 3);
      suggestion = sugResult.Item1;
      suggestionInfo = sugResult.Item2;
      foreach (var kvp in suggestionInfo)
      {
        if (idx.WordToId.TryGetValue(kvp.Value, out int sugWid))
          Weights[sugWid] = Weights.GetValueOrDefault(sugWid) + 1.0;
      }
    }

    // Synonym expansion
    ExpandSynonyms(rawWords, Weights, idx);

    if (Weights.Count == 0)
      return NoResults(suggestion);

    // * operator multiplier
    ApplyAsterisk(query, suggestionInfo, Weights, idx);

    // Query-time stop word filter
    FilterStopWords(Weights, idx);

    // Score documents using inverted index
    var scores = ScoreDocuments(Weights, idx);

    if (scores.Count == 0)
      return NoResults(suggestion);

    // Parse !, ^, ~ operators
    var excludeWords = ParseOpWords(query, @"\!\w+\b", suggestionInfo);
    var includeWords = ParseOpWords(query, @"\^\w+\b", suggestionInfo);
    var closePairs = ParseCloseQuery(query, suggestionInfo);

    // Filter candidates
    var filtered = FilterCandidates(scores, excludeWords, includeWords, closePairs, idx, stemmed.Length);

    if (filtered.Count == 0)
      return NoResults(suggestion);

    // Sort and build results
    filtered.Sort((a, b) => b.score.CompareTo(a.score));
    var items = BuildSearchItems(filtered, idx, Weights);

    string sug = suggestion ?? "";
    sw.Stop();
    _logger?.LogInformation("Query \"{Query}\": {ResultCount} results in {ElapsedMs}ms",
      query, items.Count, sw.ElapsedMilliseconds);

    var result = Tuple.Create(items.ToArray(), sug);
    AddCache(query, result);
    return result;
  }

  static (Dictionary<int, double> Weights, List<string> UnknownWords) BuildQueryWeights(string[] stemmed, IInvertedIndex idx)
  {
    var weights = new Dictionary<int, double>();
    var unknown = new List<string>();

    foreach (var w in stemmed)
    {
      if (idx.WordToId.TryGetValue(w, out int wid))
        weights[wid] = weights.GetValueOrDefault(wid) + 1.0;
      else
        unknown.Add(w);
    }

    return (weights, unknown);
  }

  static void ExpandSynonyms(string[] rawWords, Dictionary<int, double> queryWeights, IInvertedIndex idx)
  {
    foreach (var raw in rawWords)
    {
      string clean = NormalizeExpression(raw);
      var syns = Synonyms.Get(clean);
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
  }

  static void FilterStopWords(Dictionary<int, double> queryWeights, IInvertedIndex idx)
  {
    if (queryWeights.Count == 0) return;

    bool hasRare = false;
    foreach (int wid in queryWeights.Keys)
      if (idx.Idf[wid] >= idx.IdfThreshold) { hasRare = true; break; }

    if (!hasRare) return;

    var rareWeights = new Dictionary<int, double>();
    foreach (var kvp in queryWeights)
      if (idx.Idf[kvp.Key] >= idx.IdfThreshold)
        rareWeights[kvp.Key] = kvp.Value;
    queryWeights.Clear();
    foreach (var kvp in rareWeights)
      queryWeights[kvp.Key] = kvp.Value;
  }

  static Dictionary<int, double> ScoreDocuments(Dictionary<int, double> queryWeights, IInvertedIndex idx)
  {
    var scores = new Dictionary<int, double>();
    foreach (int wid in queryWeights.Keys)
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
    return scores;
  }

  static List<(int docId, double score)> FilterCandidates(
    Dictionary<int, double> scores,
    HashSet<string> excludeWords,
    HashSet<string> includeWords,
    List<(string, string)> closePairs,
    IInvertedIndex idx,
    int queryLen)
  {
    var filtered = new List<(int docId, double score)>();

    foreach (var kvp in scores)
    {
      int docId = kvp.Key;
      double score = kvp.Value / queryLen;

      if (IsExcluded(docId, excludeWords, idx)) continue;
      if (!HasRequiredWords(docId, includeWords, idx)) continue;

      if (closePairs.Count > 0)
        score += ComputeProximityBonus(closePairs, idx.DocPaths[docId]);

      filtered.Add((docId, score));
    }

    return filtered;
  }

  static bool IsExcluded(int docId, HashSet<string> excludeWords, IInvertedIndex idx)
  {
    foreach (string ew in excludeWords)
    {
      if (idx.WordToId.TryGetValue(ew, out int ewId))
      {
        var ep = idx.Postings[ewId];
        for (int pi = 0; pi < ep.Count; pi += 2)
          if (ep[pi] == docId) return true;
      }
    }
    return false;
  }

  static bool HasRequiredWords(int docId, HashSet<string> includeWords, IInvertedIndex idx)
  {
    foreach (string iw in includeWords)
    {
      if (!idx.WordToId.TryGetValue(iw, out int iwId)) return false;
      var ip = idx.Postings[iwId];
      bool found = false;
      for (int pi = 0; pi < ip.Count; pi += 2)
      {
        if (ip[pi] == docId) { found = true; break; }
      }
      if (!found) return false;
    }
    return true;
  }

  static List<SearchItem> BuildSearchItems(List<(int docId, double score)> filtered, IInvertedIndex idx, Dictionary<int, double> queryWeights)
  {
    var items = new List<SearchItem>();
    int maxResults = Math.Min(filtered.Count, 30);

    var importantWords = new HashSet<string>();
    foreach (int wid in queryWeights.Keys)
      importantWords.Add(idx.Vocabulary[wid]);

    for (int i = 0; i < maxResults; i++)
    {
      int docId = filtered[i].docId;
      string title = idx.DocNames[docId];
      string[] docWords = File.ReadAllText(idx.DocPaths[docId])
          .ToLowerInvariant()
          .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

      string snippet = Auxiliaries.Snippet(docWords, importantWords);
      items.Add(new SearchItem(title, snippet, (float)filtered[i].score));
    }

    return items;
  }

  static Tuple<SearchItem[], string> NoResults(string suggestion = "")
  {
    string sug = suggestion ?? "";
    return Tuple.Create(
      new[] { new SearchItem("No se encontraron textos coincidentes", "Por favor realice una busqueda diferente", 0f) },
      sug
    );
  }

  static HashSet<string> ParseOpWords(string search, string pattern, Dictionary<string, string> wordstochange)
  {
    var result = new HashSet<string>();
    Match m = Regex.Match(search, pattern);
    while (m.Success)
    {
      string clean = NormalizeExpression(m.Value);
      string stemmed = SpanishStemmer.Stem(clean);
      if (wordstochange.TryGetValue(clean, out string? sug))
        stemmed = SpanishStemmer.Stem(sug);
      result.Add(stemmed);
      m = m.NextMatch();
    }
    return result;
  }

  static void ApplyAsterisk(string search, Dictionary<string, string> wordstochange, Dictionary<int, double> queryWeights, IInvertedIndex idx)
  {
    Match m = MyRegex().Match(search);
    while (m.Success)
    {
      int starCount = m.Value.Count(c => c == '*');
      string clean = NormalizeExpression(m.Value);
      string stemmed = SpanishStemmer.Stem(clean);
      if (wordstochange.TryGetValue(clean, out string? sug))
        stemmed = SpanishStemmer.Stem(sug);
      if (idx.WordToId.TryGetValue(stemmed, out int wid))
        queryWeights[wid] = queryWeights.GetValueOrDefault(wid) * starCount * 2;
      m = m.NextMatch();
    }
  }

  static List<(string, string)> ParseCloseQuery(string search, Dictionary<string, string> wordstochange)
  {
    var result = new List<(string, string)>();
    string[] parts = search.Split(separator, StringSplitOptions.RemoveEmptyEntries);

    for (int k = 0; k < parts.Length; k++)
    {
      string clean = NormalizeExpression(parts[k]);
      if (wordstochange.TryGetValue(clean, out string? sug))
        parts[k] = sug;
    }

    for (int i = 0; i < parts.Length; i++)
    {
      if (parts[i] == "~" && i > 0 && i < parts.Length - 1)
      {
        string w1 = SpanishStemmer.Stem(NormalizeExpression(parts[i - 1]));
        string w2 = SpanishStemmer.Stem(NormalizeExpression(parts[i + 1]));
        if (w1 != w2)
          result.Add((w1, w2));
      }
    }
    return result;
  }

  static double ComputeProximityBonus(List<(string, string)> pairs, string docPath)
  {
    string[] words = InvertedIndex.ProcessText(File.ReadAllText(docPath));
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

  public void Dispose()
  {
    if (_disposed) return;
    _disposed = true;

    lock (_rebuildLock)
    {
      _rebuildTimer?.Dispose();
      _rebuildTimer = null;
    }

    if (_watcher != null)
    {
      _watcher.EnableRaisingEvents = false;
      _watcher.Dispose();
      _watcher = null;
    }

    GC.SuppressFinalize(this);
  }

  [GeneratedRegex(@"\*+\w+\b")]
  private static partial Regex MyRegex();
}
