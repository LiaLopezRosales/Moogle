using System.Text.RegularExpressions;

namespace MoogleEngine;

class Auxiliaries
{
  static int LevenshteinDistance(string a, string b)
  {
    int m = a.Length, n = b.Length;
    if (m == 0) return n;
    if (n == 0) return m;
    int[] prev = new int[n + 1];
    int[] curr = new int[n + 1];
    for (int j = 0; j <= n; j++) prev[j] = j;
    for (int i = 1; i <= m; i++)
    {
      curr[0] = i;
      for (int j = 1; j <= n; j++)
      {
        int cost = a[i - 1] == b[j - 1] ? 0 : 1;
        curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
      }
      (curr, prev) = (prev, curr);
    }
    return prev[n];
  }

  internal static Tuple<string, Dictionary<string, string>> Suggestion(string search, string[] wordsLevenshtein, string[] listwords, int similaritythreshold)
  {
    string[] normsearch = DataBase.NormalizeString(search);
    string oldsearch = DataBase.NormalizeExpression(search);
    string suggestion = "";
    var similarwords = new Dictionary<string, string>();
    for (int i = 0; i < wordsLevenshtein.Length; i++)
    {
      string temporal = wordsLevenshtein[i];
      string tempword = "";
      int tempmin = int.MaxValue;
      int lenTemporal = temporal.Length;
      for (int j = 0; j < listwords.Length; j++)
      {
        int lenVocab = listwords[j].Length;
        if (Math.Abs(lenTemporal - lenVocab) > similaritythreshold) continue;
        if (LevenshteinDistance(temporal, listwords[j]) < tempmin)
        {
          tempmin = LevenshteinDistance(temporal, listwords[j]);
          tempword = listwords[j];
        }
      }
      if (tempmin <= similaritythreshold)
        similarwords.Add(temporal, tempword);
    }
    if (similarwords.Count >= 1)
    {
      for (int k = 0; k < normsearch.Length; k++)
      {
        foreach (var word in similarwords)
        {
          if (normsearch[k] == word.Key)
            normsearch[k] = word.Value;
        }
        suggestion += normsearch[k] + " ";
      }
    }
    if (oldsearch == suggestion) suggestion = "";
    return Tuple.Create(suggestion, similarwords);
  }

  public static string Snippet(string[] documentx, HashSet<string> importantwords)
  {
    string result = "";
    int inicialindex = 0;
    int finalindex = 0;
    int count = 0;

    if (documentx.Length >= 50)
    {
      for (int i = 0, j = 50; i < j; i++)
      {
        if (importantwords.Contains(documentx[i])) count++;
        finalindex = j;
      }
    }
    else
    {
      for (int i = 0, j = documentx.Length / 2; i < j; i++)
      {
        if (importantwords.Contains(documentx[i])) count++;
        finalindex = j;
      }
    }
    int temporal = count;
    int tempinicialindex = inicialindex;
    int tempfinalindex = finalindex;

    for (int k = inicialindex + 1, m = finalindex; m < documentx.Length; k++, m++)
    {
      if ((!importantwords.Contains(documentx[m])) && (!importantwords.Contains(documentx[k])))
        continue;

      if (!importantwords.Contains(documentx[k]) && importantwords.Contains(documentx[m]))
        temporal++;
      if (!importantwords.Contains(documentx[m]) && importantwords.Contains(documentx[k]))
        temporal--;
      if (importantwords.Contains(documentx[m]) && importantwords.Contains(documentx[k]))
      {
        temporal++;
        if (temporal > count)
        {
          count = temporal;
          tempinicialindex = k;
          tempfinalindex = m;
        }
        temporal--;
      }
      if (temporal > count)
      {
        count = temporal;
        tempinicialindex = k;
        tempfinalindex = m;
      }
    }
    inicialindex = tempinicialindex;
    finalindex = tempfinalindex;

    for (int n = inicialindex; n < finalindex + 1; n++)
    {
      string word = documentx[n];
      string stemmed = SpanishStemmer.Stem(DataBase.NormalizeExpression(word));
      if (importantwords.Contains(stemmed))
        result += "<strong>" + word + "</strong>";
      else
        result += word;
      result += " ";
    }
    return result;
  }
}
