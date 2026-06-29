namespace MoogleEngine;

public interface IDataBase
{
  IInvertedIndex Index { get; }
  Tuple<SearchItem[], string> Query(string query);
}
