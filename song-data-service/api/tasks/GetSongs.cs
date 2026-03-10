using System;
using System.Net;
using Newtonsoft.Json;

namespace SongDataService;

public class GetSong
{
    private struct Chart
    {
        public long normal;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long? expert;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long? master;

        public Chart() { normal = 0; expert = null; master = null; }
    }
    private struct Diff
    {
        public long level;
        public Dictionary<long, Chart> style_list;
        public Dictionary<string, string> source_list;

        public Diff() { level = 0; style_list = []; source_list = []; }
    }
    private struct Song
    {
        public Dictionary<string, string> title_list;
        public Dictionary<string, string> subtitle_list;
        public Dictionary<string, string> alias_list;

        public long genre;
        public List<long> genre_list;

        public Dictionary<string, long> region_list;
        
        public Dictionary<long, Diff> chart_list;

        public Song() { title_list = []; subtitle_list = []; alias_list = []; genre = 0; genre_list = []; region_list = []; chart_list = []; }
    }

    public static async Task<ResponseData> Songs(RequestData request, long[] ids)
    {
        ResponseData response = new();

        if (request.Headers.TryGetValue("accept", out string? type) && !type.Contains("application/json") && !type.Contains("*/*"))
        {
            response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
            response.Body = JsonConvert.SerializeObject(new ErrorData(response.StatusCode, "None of the content type(s) requested are supported."));
            return response;
        }

        if (ids.Count() == 0)
        {
            response.Body = "[]";
            return response;
        }

        ids = ids.Take(APISettings.SONG_LIMIT).ToArray();
        string id_list = string.Join(',', ids);

        using (DatabaseHandler database = new())
        {
            Dictionary<long, Song> songs = [];
            long FetchId(Dictionary<string, object?> result) => (long?)result["id"] ?? -1;

            #region Regions
            string query = @$"
            SELECT * FROM region
            WHERE region.id IN ({id_list})";

            var results = database.Query(query);
            // Update ID list with valid results for quicker search results in the future
            ids = results.Values.Select(item => (long?)item["id"] ?? -1).Where(item => item != -1).ToArray();
            id_list = string.Join(',', ids);

            foreach (var result in results.Values.Where(item => (long?)item["id"] is not null))
            {
                long id = FetchId(result);
                Song song = new();

                void AddRegion(string locale)
                {
                    if (result[locale] != null)
                        song.region_list.Add(locale, (long?)result[locale] ?? -1);
                }

                AddRegion("japan");
                AddRegion("asia");
                AddRegion("oceania");
                AddRegion("united-states");
                AddRegion("china");

                songs[id] = song;
            }
            #endregion

            #region Titles
            query = @$"
            SELECT * FROM title
            WHERE title.id IN ({id_list})";

            results = database.Query(query);

            foreach (var result in results.Values)
            {
                long id = FetchId(result);
                Song song = songs[id];

                void AddTitle(string locale)
                {
                    if (result[locale] != null)
                        song.title_list.Add(locale, (string?)result[locale] ?? "");
                }

                AddTitle("ja");
                AddTitle("en-US");
                AddTitle("ko");
                AddTitle("zh-TW");
                AddTitle("zh-CN");

                songs[id] = song;
            }
            #endregion

            #region Subtitles
            query = @$"
            SELECT * FROM subtitle
            WHERE subtitle.id IN ({string.Join(',', ids)})";

            results = database.Query(query);

            foreach (var result in results.Values)
            {
                long id = FetchId(result);
                Song song = songs[id];

                void AddSubtitle(string locale)
                {
                    if (result[locale] != null)
                        song.subtitle_list.Add(locale, (string?)result[locale] ?? "");
                }

                AddSubtitle("ja");
                AddSubtitle("en-US");
                AddSubtitle("ko");
                AddSubtitle("zh-TW");
                AddSubtitle("zh-CN");
                
                songs[id] = song;
            }
            #endregion

            #region Alias
            query = @$"
            SELECT * FROM alias
            WHERE alias.id IN ({id_list})";

            results = database.Query(query);

            foreach (var result in results.Values)
            {
                long id = FetchId(result);
                Song song = songs[id];

                song.alias_list[(string?)result["lang"] ?? ""] = (string?)result["title"] ?? "";

                songs[id] = song;
            }
            #endregion

            #region Genres
            query = @$"
            SELECT * FROM genre
            WHERE genre.id IN ({id_list})";

            results = database.Query(query);

            foreach (var result in results.Values)
            {
                long id = FetchId(result);
                Song song = songs[id];

                song.genre = (long?)result["genre"] ?? 0;
                song.genre_list.Add(song.genre);
                if (result["subgenre"] != null) song.genre_list.Add((long?)result["subgenre"] ?? 0);
                if (result["subgenre2"] != null) song.genre_list.Add((long?)result["subgenre2"] ?? 0);

                songs[id] = song;
            }
            #endregion

            #region Sources
            query = @$"
            SELECT * FROM source
            WHERE source.id IN ({id_list})";

            results = database.Query(query);

            foreach (var result in results.Values)
            {
                long id = FetchId(result);
                long diff = (long?)result["diff"] ?? 4;
                Song song = songs[id];

                if (!song.chart_list.ContainsKey(diff)) song.chart_list[diff] = new();
                song.chart_list[diff].source_list[(string?)result["source"] ?? ""] = (string?)result["url"] ?? "";

                songs[id] = song;
            }
            #endregion

            #region Charts
            query = @$"
            SELECT * FROM chart
            WHERE chart.id IN ({id_list})";

            results = database.Query(query);

            foreach (var result in results.Values)
            {
                long id = FetchId(result);
                long diff = (long?)result["diff"] ?? 4;
                Song song = songs[id];

                if (!song.chart_list.ContainsKey(diff)) song.chart_list[diff] = new();
                song.chart_list[diff].style_list[(long?)result["style"] ?? 0] = new() { normal = (long?)result["normal"] ?? 0, expert = (long?)result["expert"], master = (long?)result["master"] };

                songs[id] = song;
            }
            #endregion

            response.Body = JsonConvert.SerializeObject(songs);
            response.StatusCode = (int)HttpStatusCode.OK;
        }

        return response;
    }
}
