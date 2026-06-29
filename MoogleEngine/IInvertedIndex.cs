namespace MoogleEngine;

public interface IInvertedIndex
{
  List<string> Vocabulary { get; }
  Dictionary<string, int> WordToId { get; }
  List<int>[] Postings { get; }
  string[] DocNames { get; }
  string[] DocPaths { get; }
  int[] DocLengths { get; }
  double[] Idf { get; }
  double IdfThreshold { get; }
  int DocCount { get; }
  int VocabCount { get; }
}
