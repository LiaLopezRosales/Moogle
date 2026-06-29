namespace MoogleEngine;


public static class Moogle
{
    public static DataBase data = null!;
    public static SearchResult Query(string query) {
        var result = data.Query(query);
        return new SearchResult(result.Item1, result.Item2);
    }
}
