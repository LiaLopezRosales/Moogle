using System.Text;
using System.Text.RegularExpressions;

namespace MoogleEngine;

public partial class InvertedIndex : IInvertedIndex
{
  public List<string> Vocabulary { get; private set; } = [];
  public Dictionary<string, int> WordToId { get; private set; } = [];
  public List<int>[] Postings { get; private set; } = [];
  public string[] DocNames { get; private set; } = [];
  public string[] DocPaths { get; private set; } = [];
  public int[] DocLengths { get; private set; } = [];
  public double[] Idf { get; private set; } = [];
  public double IdfThreshold { get; private set; }

  public int DocCount => DocNames.Length;
  public int VocabCount => Vocabulary.Count;

  internal static readonly Regex NonAlphaRegex = MyRegex();
  private static readonly char[] separator = new[] { ' ' };

  static string Normalize(string content)
  {
    content = content.ToLower();
    content = NonAlphaRegex.Replace(content.Normalize(NormalizationForm.FormD), "");
    return content;
  }

  public static string[] ProcessText(string text)
  {
    string norm = Normalize(text);
    string[] tokens = norm.Split(separator, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < tokens.Length; i++)
      tokens[i] = SpanishStemmer.Stem(tokens[i]);
    return tokens;
  }

  public static InvertedIndex Build(string[] filePaths)
  {
    int fileCount = filePaths.Length;
    var docNames = new string[fileCount];
    var docPaths = new string[fileCount];

    // Phase 1: Process each file independently in parallel
    var fileWordFreqs = new Dictionary<string, int>[fileCount];

    Parallel.For(0, fileCount, i =>
    {
      docNames[i] = Path.GetFileNameWithoutExtension(filePaths[i]);
      docPaths[i] = filePaths[i];

      string content = File.ReadAllText(filePaths[i]);
      string[] words = ProcessText(content);

      var freq = new Dictionary<string, int>();
      foreach (var w in words)
      {
        freq.TryGetValue(w, out int c);
        freq[w] = c + 1;
      }
      fileWordFreqs[i] = freq;
    });

    // Phase 2: Merge into global vocabulary and postings (single-threaded)
    var vocab = new List<string>();
    var wordToId = new Dictionary<string, int>();
    var postings = new List<List<int>>();
    var docLengths = new int[fileCount];

    for (int docId = 0; docId < fileCount; docId++)
    {
      var freq = fileWordFreqs[docId];
      int totalWords = 0;
      foreach (var kvp in freq)
        totalWords += kvp.Value;
      docLengths[docId] = totalWords;

      foreach (var kvp in freq)
      {
        if (!wordToId.TryGetValue(kvp.Key, out int wordId))
        {
          wordId = vocab.Count;
          vocab.Add(kvp.Key);
          wordToId[kvp.Key] = wordId;
          postings.Add([]);
        }
        postings[wordId].Add(docId);
        postings[wordId].Add(kvp.Value);
      }
    }

    // Phase 3: Compute IDF for all words and determine stop-word threshold
    int totalDocs = fileCount;
    int vocabCount = vocab.Count;
    double idfThreshold = 0;

    double[] idfValues = new double[vocabCount];
    if (vocabCount > 0)
    {
      double idfSum = 0, idfSumSq = 0;
      for (int i = 0; i < vocabCount; i++)
      {
        int df = postings[i].Count / 2;
        double idf = Math.Log10((double)totalDocs / Math.Max(df, 1));
        idfValues[i] = idf;
        idfSum += idf;
        idfSumSq += idf * idf;
      }

      double mean = idfSum / vocabCount;
      double variance = Math.Max(0, idfSumSq / vocabCount - mean * mean);
      double stddev = Math.Sqrt(variance);
      idfThreshold = mean - stddev;
    }

    // Phase 4: Load synonyms
    string synPath = Path.Combine(
      Path.GetDirectoryName(filePaths[0]) ?? ".",
      "synonyms.txt"
    );
    if (File.Exists(synPath))
      Synonyms.Load(synPath);

    return new InvertedIndex
    {
      Vocabulary = vocab,
      WordToId = wordToId,
      Postings = [.. postings],
      DocNames = docNames,
      DocPaths = docPaths,
      DocLengths = docLengths,
      Idf = idfValues,
      IdfThreshold = idfThreshold
    };
  }

  [GeneratedRegex("[^a-zA-Z0-9 -]")]
  private static partial Regex MyRegex();
}
