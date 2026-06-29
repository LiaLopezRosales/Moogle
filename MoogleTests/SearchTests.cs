using MoogleEngine;
using Xunit;

namespace MoogleTests;

public class SearchTests
{
  private readonly DataBase _db;
  private readonly string _contentDir;

  public SearchTests()
  {
    _contentDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData"));
    _db = new DataBase(_contentDir);
  }

  [Fact]
  public void Query_ReturnsResults_ForExistingTerm()
  {
    var (items, _) = _db.Query("burbuja");
    Assert.NotEmpty(items);
  }

  [Fact]
  public void Query_ReturnsResultsInScoreOrder()
  {
    var (items, _) = _db.Query("algoritmos");
    Assert.NotEmpty(items);
    for (int i = 1; i < items.Length; i++)
      Assert.True(items[i - 1].Score >= items[i].Score - 0.001f);
  }

  [Fact]
  public void Query_ExcludeOperator_ExcludesDocuments()
  {
    var (items, _) = _db.Query("algoritmos !redes");
    Assert.NotEmpty(items);
    foreach (var item in items)
      Assert.DoesNotContain("redes", item.Title, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void Query_RequireOperator_OnlyIncludesRequired()
  {
    var (items, _) = _db.Query("^burbuja");
    Assert.NotEmpty(items);
    foreach (var item in items)
      Assert.Contains("algoritmos", item.Title, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void Query_ReturnsNoResults_ForNonsenseQuery()
  {
    var (items, _) = _db.Query("xyzzy");
    Assert.Single(items);
    Assert.Equal(0f, items[0].Score);
  }

  [Fact]
  public void Query_ReturnsSuggestion_ForMisspelledWord()
  {
    var (items, suggestion) = _db.Query("hepsort");
    Assert.NotEmpty(suggestion);
  }

  [Fact]
  public void Query_SnippetContainsQueryTerms()
  {
    var (items, _) = _db.Query("burbuja");
    Assert.NotEmpty(items);
    Assert.Contains("burbuja", items[0].Snippet, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void Query_SnippetContainsStrongTags()
  {
    var (items, _) = _db.Query("burbuja");
    Assert.NotEmpty(items);
    Assert.Contains("<strong>", items[0].Snippet);
    Assert.Contains("</strong>", items[0].Snippet);
  }
}
