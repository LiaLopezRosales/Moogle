namespace MoogleEngine;


public static class Moogle
{
    public static DataBase data;
    public static SearchResult Query(string query) {

        SearchItem[] items = data.Query(query).Item1;
        string suggestion = data.Query(query).Item2;
        
        return new SearchResult(items, suggestion);
    }
}
