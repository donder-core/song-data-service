using System.Net;
using Newtonsoft.Json;

namespace SongDataService;

public class SearchIDs
{
    public static async Task<ResponseData> Search(string? title = null, string? subtitle = null, int? genre = null, int? diff = null, int? level = null, bool? useAlias = null)
    {
        ResponseData response = new();
        response.ContentType = "application/json";

        if (title is null && subtitle is null && genre is null && diff is null && level is null)
        {
            response.Body = "[]";
            return response;
        }

        List<long> results = [];

        if (genre != null && (genre < 1 || genre > 8))
        {
            response.StatusCode = (int)HttpStatusCode.UnprocessableContent;
            response.Body = JsonConvert.SerializeObject(new { error = response.StatusCode, message = $"'{genre}' is not a valid genre. Please only use an integer valued between 1~8."});
            return response;
        }
        if (diff != null && (diff < 1 || diff > 5))
        {
            response.StatusCode = (int)HttpStatusCode.UnprocessableContent;
            response.Body = JsonConvert.SerializeObject(new { error = response.StatusCode, message = $"'{diff}' is not a valid difficulty. Please only use an integer valued between 1~5."});
            return response;
        }
        if (level != null && (level < 1 || level > 10))
        {
            response.StatusCode = (int)HttpStatusCode.UnprocessableContent;
            response.Body = JsonConvert.SerializeObject(new { error = response.StatusCode, message = $"'{level}' is not a valid level. Please only use an integer valued between 1~10."});
            return response;
        }

        DatabaseHandler database = new();

        long[] title_ids = title != null ? SearchByTitle(ref database, title, useAlias ?? true) : [];
        long[] subtitle_ids = subtitle != null ? SearchBySubtitle(ref database, subtitle) : [];
        long[] genre_ids = genre != null ? SearchByGenre(ref database, genre ?? 0) : [];
        long[] diff_ids = diff != null ? SearchByDifficulty(ref database, diff ?? 4) : [];
        long[] level_ids = level != null ? SearchByLevel(ref database, level ?? 0) : [];

        foreach (long[] list in new long[][] {title_ids, subtitle_ids, genre_ids, diff_ids, level_ids})
        {
            if (list.Count() > 0)
            {
                results = list.ToList();
                break;
            }
        }
        if (results.Count > 0)
        {
            if (title != null) results = results.Intersect(title_ids).ToList();
            if (subtitle != null) results = results.Intersect(subtitle_ids).ToList();
            if (genre != null) results = results.Intersect(genre_ids).ToList();
            if (diff != null) results = results.Intersect(diff_ids).ToList();
            if (level != null) results = results.Intersect(level_ids).ToList();
        }

        database.Dispose();

        response.StatusCode = (int)HttpStatusCode.OK;
        response.Body = JsonConvert.SerializeObject(results.ToArray());

        return response;
    }
    private static long[] SearchByTitle(ref DatabaseHandler database, string title, bool useAlias = true)
    {
        string query = @$"
        SELECT DISTINCT title.id FROM title
        WHERE title.id IN
        (
        SELECT title.id FROM title
        INNER JOIN alias ON title.id = alias.id
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

    private static long[] SearchBySubtitle(ref DatabaseHandler database, string subtitle)
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

    private static long[] SearchByGenre(ref DatabaseHandler database, int genre)
    {
        string query = @$"
        SELECT genre.id FROM genre
        WHERE genre.genre IS {genre}
        OR genre.subgenre IS {genre}
        OR genre.subgenre2 IS {genre}
        ";

        return ProcessResult(database.Query(query));
    }

    private static long[] SearchByDifficulty(ref DatabaseHandler database, int difficulty)
    {
        string query = @$"
        SELECT DISTINCT chart.id FROM chart
        WHERE chart.diff IS {difficulty}
        ";

        return ProcessResult(database.Query(query));
    }

    private static long[] SearchByLevel(ref DatabaseHandler database, int level)
    {
        string query = @$"
        SELECT DISTINCT chart.id FROM chart
        WHERE chart.level = {level}
        ";

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
