namespace MoogleEngine;

public class SearchItem(string title, string snippet, float score)
{
  public string Title { get; private set; } = title;

  public string Snippet { get; private set; } = snippet;

  public float Score { get; private set; } = score;
}
