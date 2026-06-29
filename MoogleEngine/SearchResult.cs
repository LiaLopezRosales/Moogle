namespace MoogleEngine;

public class SearchResult(SearchItem[] items, string suggestion = "")
{
  private readonly SearchItem[] items = items ?? throw new ArgumentNullException("items");

  public SearchResult() : this([])
  {

  }

  public string Suggestion { get; private set; } = suggestion;

  public IEnumerable<SearchItem> Items()
  {
    return this.items;
  }

  public int Count { get { return this.items.Length; } }
}
