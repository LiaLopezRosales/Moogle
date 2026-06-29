namespace MoogleEngine;

public class Stemmer : IStemmer
{
  public string Stem(string word) => SpanishStemmer.Stem(word);
}
