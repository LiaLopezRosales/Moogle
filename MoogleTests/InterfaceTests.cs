using MoogleEngine;
using NSubstitute;
using Xunit;

namespace MoogleTests;

public class InterfaceTests
{
  [Fact]
  public void DataBase_CanBeConstructed_WithMockedIndex()
  {
    var mockIndex = Substitute.For<IInvertedIndex>();
    mockIndex.VocabCount.Returns(0);
    mockIndex.DocCount.Returns(0);

    var db = new DataBase(mockIndex);
    Assert.Equal(0, db.Index.VocabCount);
  }

  [Fact]
  public void Query_ReturnsNoResults_WithEmptyIndex()
  {
    var mockIndex = Substitute.For<IInvertedIndex>();
    mockIndex.Vocabulary.Returns([]);
    mockIndex.WordToId.Returns([]);
    mockIndex.Postings.Returns([]);
    mockIndex.DocNames.Returns([]);
    mockIndex.DocPaths.Returns([]);
    mockIndex.DocLengths.Returns([]);
    mockIndex.Idf.Returns([]);

    var db = new DataBase(mockIndex);
    var (items, _) = db.Query("nonexistent");
    Assert.Single(items);
    Assert.Equal(0f, items[0].Score);
  }

  [Fact]
  public void Stemmer_Interface_CanBeMocked()
  {
    var mock = Substitute.For<IStemmer>();
    mock.Stem("testing").Returns("test");

    var result = mock.Stem("testing");
    Assert.Equal("test", result);
  }

  [Fact]
  public void DataBase_Implements_IDataBase()
  {
    var mockIndex = Substitute.For<IInvertedIndex>();
    mockIndex.VocabCount.Returns(0);
    mockIndex.DocCount.Returns(0);
    var db = new DataBase(mockIndex);

    Assert.IsAssignableFrom<IDataBase>(db);
  }
}
