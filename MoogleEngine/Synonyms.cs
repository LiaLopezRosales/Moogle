namespace MoogleEngine;

public static class Synonyms
{
  static readonly Dictionary<string, HashSet<string>> map = [];

  public static void Load(string path)
  {
    if (!File.Exists(path)) return;
    map.Clear();
    foreach (var line in File.ReadLines(path))
    {
      var trimmed = line.Trim();
      if (trimmed.Length == 0 || trimmed.StartsWith("#")) continue;

      var words = trimmed.Split(',')
          .Select(w => w.Trim().ToLowerInvariant())
          .Where(w => w.Length > 0 && !w.Contains(' '))
          .ToArray();

      if (words.Length < 2) continue;

      foreach (var w in words)
      {
        if (!map.ContainsKey(w))
          map[w] = [];
        foreach (var other in words)
          if (other != w)
            map[w].Add(other);
      }
    }
  }

  public static string[]? Get(string word)
  {
    word = word.ToLowerInvariant();
    if (map.TryGetValue(word, out var syns))
      return [.. syns];
    return null;
  }

  public static int Count => map.Count;
}
