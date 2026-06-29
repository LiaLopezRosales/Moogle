using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace MoogleEngine;

public class InvertedIndex
{
  public List<string> Vocabulary { get; private set; } = new();
  public Dictionary<string, int> WordToId { get; private set; } = new();
  public List<int>[] Postings { get; private set; } = Array.Empty<List<int>>();
  public string[] DocNames { get; private set; } = Array.Empty<string>();
  public string[] DocPaths { get; private set; } = Array.Empty<string>();
  public int[] DocLengths { get; private set; } = Array.Empty<int>();
  public double[] Idf { get; private set; } = Array.Empty<double>();

  public int DocCount => DocNames.Length;
  public int VocabCount => Vocabulary.Count;

  static readonly Regex nonAlphaRegex = new("[^a-zA-Z0-9 -]");

  static string Normalize(string content)
  {
    content = content.ToLower();
    content = nonAlphaRegex.Replace(content.Normalize(NormalizationForm.FormD), "");
    return content;
  }

  public static string[] ProcessText(string text)
  {
    string norm = Normalize(text);
    string[] tokens = norm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
          postings.Add(new List<int>());
        }
        postings[wordId].Add(docId);
        postings[wordId].Add(kvp.Value);
      }
    }

    // Phase 3: Stop word removal — filter words with anomalously low IDF
    int totalDocs = fileCount;
    double idfSum = 0, idfSumSq = 0;
    int vocabCount = vocab.Count;

    double[] idfValues = new double[vocabCount];
    for (int i = 0; i < vocabCount; i++)
    {
      int df = postings[i].Count / 2;
      double idf = Math.Log10((double)totalDocs / Math.Max(df, 1));
      idfValues[i] = idf;
      idfSum += idf;
      idfSumSq += idf * idf;
    }

    if (vocabCount > 0)
    {
      double mean = idfSum / vocabCount;
      double stddev = Math.Sqrt(idfSumSq / vocabCount - mean * mean);
      double threshold = mean - stddev;

      // Mark words to keep
      var keep = new bool[vocabCount];
      int keepCount = 0;
      for (int i = 0; i < vocabCount; i++)
      {
        if (idfValues[i] >= threshold)
        {
          keep[i] = true;
          keepCount++;
        }
      }

      // Rebuild compact vocabulary and postings
      var newVocab = new List<string>(keepCount);
      var newWordToId = new Dictionary<string, int>(keepCount);
      var newPostings = new List<List<int>>(keepCount);
      var oldToNew = new int[vocabCount];
      Array.Fill(oldToNew, -1);

      for (int i = 0; i < vocabCount; i++)
      {
        if (keep[i])
        {
          int newId = newVocab.Count;
          oldToNew[i] = newId;
          newVocab.Add(vocab[i]);
          newWordToId[vocab[i]] = newId;
          newPostings.Add(postings[i]);
        }
      }

      // Rebuild IDF array
      var newIdf = new double[keepCount];
      for (int i = 0; i < vocabCount; i++)
      {
        if (keep[i])
          newIdf[oldToNew[i]] = idfValues[i];
      }

      vocab = newVocab;
      wordToId = newWordToId;
      postings = newPostings;
      idfValues = newIdf;
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
      Postings = postings.ToArray(),
      DocNames = docNames,
      DocPaths = docPaths,
      DocLengths = docLengths,
      Idf = idfValues
    };
  }
}
