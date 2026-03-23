using System.Net;
using Newtonsoft.Json;

namespace SongDataService;

public class GetIDs
{
    private static Dictionary<long, long> _sortScore = [];
    public static async Task<ResponseData> Search(RequestData request, string? title = null, string? subtitle = null, int? genre = null, int? diff = null, int? level = null, bool? useAlias = null, bool? includeSayonara = null, bool? titleComparison = null, int? limit = null)
    {
        if (!APIExtensions.ContentTypeIsAllowed(request)) return APIExtensions.UnsupportedContentType;

        ResponseData response = new();
        _sortScore = [];

        if (title is null && subtitle is null && genre is null && diff is null && level is null)
        {
            response.Body = "[]";
            response.StatusCode = (int)HttpStatusCode.OK;
            return response;
        }

        if (genre != null && (genre < 1 || genre > 8))
        {
            response.StatusCode = (int)HttpStatusCode.UnprocessableContent;
            response.Body = JsonConvert.SerializeObject(new ErrorData(response.StatusCode, $"'{genre}' is not a valid genre ID."));
            return response;
        }
        if (diff != null && (diff < 1 || diff > 5))
        {
            response.StatusCode = (int)HttpStatusCode.UnprocessableContent;
            response.Body = JsonConvert.SerializeObject(new ErrorData(response.StatusCode, $"'{diff}' is not a valid difficulty ID."));
            return response;
        }
        if (level != null && (level < 1 || level > 10))
        {
            response.StatusCode = (int)HttpStatusCode.UnprocessableContent;
            response.Body = JsonConvert.SerializeObject(new ErrorData(response.StatusCode, $"'{level}' is not a valid level."));
            return response;
        }

        DatabaseHandler database = new();
        int song_limit = Math.Clamp(limit ?? APISettings.SONG_LIMIT, 0, APISettings.SONG_LIMIT);

        long[] title_ids = title != null ? SearchByTitle(ref database, title, useAlias ?? true) : [];
        long[] subtitle_ids = subtitle != null ? SearchBySubtitle(ref database, subtitle) : [];
        long[] genre_ids = genre != null ? SearchByGenre(ref database, genre ?? 0) : [];
        long[] diff_ids = diff != null ? SearchByDifficulty(ref database, diff ?? 4) : [];
        long[] level_ids = level != null ? SearchByLevel(ref database, level ?? 0) : [];

        List<long> results = FilterSayonara(ref database, includeSayonara is not null and true).ToList();
        if (title != null) results = results.Intersect(title_ids).ToList();
        if (subtitle != null) results = (titleComparison is not null and false) && (title != null) ? results.Union(subtitle_ids).ToList() : results.Intersect(subtitle_ids).ToList();
        if (genre != null) results = results.Intersect(genre_ids).ToList();
        if (diff != null) results = results.Intersect(diff_ids).ToList();
        if (level != null) results = results.Intersect(level_ids).ToList();

        results.Sort();
        results = results.Take(song_limit).ToList();

        database.Dispose();

        response.Body = JsonConvert.SerializeObject(results);
        response.StatusCode = (int)HttpStatusCode.OK;

        return response;
    }
    public static long[] SearchByTitle(ref DatabaseHandler database, string title, bool useAlias)
    {
        string query = @$"
        SELECT DISTINCT title.id FROM title
        WHERE title.id IN
        (
        SELECT title.id FROM title
        LEFT JOIN alias ON title.id = alias.id
        WHERE title.'ja' LIKE '%{title.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        OR title.'en-US' LIKE '%{title.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        OR title.'ko' LIKE '%{title.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        OR title.'zh-TW' LIKE '%{title.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        OR title.'zh-CN' LIKE '%{title.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        OR title.id IN
        (SELECT alias.id FROM alias WHERE
        alias.title LIKE '%{title.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}')
        )
        ";

        if (!useAlias) query = @$"
        SELECT DISTINCT title.id FROM title
        WHERE title.id IN
        (
        SELECT title.id FROM title
        WHERE title.'ja' LIKE '%{title.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        OR title.'en-US' LIKE '%{title.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        OR title.'ko' LIKE '%{title.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        OR title.'zh-TW' LIKE '%{title.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        OR title.'zh-CN' LIKE '%{title.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        )
        ";

        return ProcessResult(database.Query(query));
    }

    public static long[] SearchBySubtitle(ref DatabaseHandler database, string subtitle)
    {
        string query = @$"
        SELECT subtitle.id FROM subtitle
        WHERE subtitle.'ja' LIKE '%{subtitle.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        OR subtitle.'en-US' LIKE '%{subtitle.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        OR subtitle.'ko' LIKE '%{subtitle.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        OR subtitle.'zh-TW' LIKE '%{subtitle.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        OR subtitle.'zh-CN' LIKE '%{subtitle.Sqlite_EscapeForLike()}%' ESCAPE '{SqliteExtensions.Sqlite_EscapeChar}'
        ";

        return ProcessResult(database.Query(query));
    }

    public static long[] SearchByGenre(ref DatabaseHandler database, int genre)
    {
        string query = @$"
        SELECT genre.id FROM genre
        WHERE genre.genre IS {genre}
        OR genre.subgenre IS {genre}
        OR genre.subgenre2 IS {genre}
        ";
        return ProcessResult(database.Query(query));
    }

    public static long[] SearchByDifficulty(ref DatabaseHandler database, int difficulty)
    {
        string query = @$"
        SELECT DISTINCT chart.id FROM chart
        WHERE chart.diff IS {difficulty}
        ";

        return ProcessResult(database.Query(query));
    }

    public static long[] SearchByLevel(ref DatabaseHandler database, int level)
    {
        string query = @$"
        SELECT DISTINCT level.id FROM level
        WHERE level.level = {level}
        ";

        return ProcessResult(database.Query(query));
    }

    public static long[] FilterSayonara(ref DatabaseHandler database, bool include_sayonara)
    {
        string query = @$"
        SELECT DISTINCT region.id FROM region
        WHERE (region.japan IS NOT 0 OR region.asia IS NOT 0 OR region.oceania IS NOT 0 OR region.china IS NOT 0 OR region.'united-states' IS NOT 0)";

        if (include_sayonara)
        query = @$"
        SELECT DISTINCT region.id FROM region";

        return ProcessResult(database.Query(query));
    }

    private static long[] ProcessResult(Dictionary<long, Dictionary<string, object?>> result)
    {
        if (result.Count == 0)
            return [];

        List<long> ids = [];
        foreach (var item in result.Values)
        {
            if (item["id"] != null)
            ids.Add((long)(item["id"] ?? 0));
        }

        return ids.ToArray();
    }
}
