using System.Net;
using Newtonsoft.Json;

namespace SongDataService
{
    public class GetDiff
    {
        private struct Chart
        {
            public long level;
            public long normal;
            public long? expert;
            public long? master;
        }

        public static async Task<ResponseData> GetDifficulty(long[] ids, long[] difficulties)
        {
            var response = new ResponseData();
            response.ContentType = "application/json";

            if (ids.Length == 0)
            {
                response.Body = "[]";
                return response;
            }
            foreach (long difficulty in difficulties)
            {
                if (difficulty < 1 || difficulty > 5)
                {
                    response.StatusCode = (int)HttpStatusCode.UnprocessableContent;
                    response.Body = JsonConvert.SerializeObject(new { error = response.StatusCode, message = $"'{difficulty}' is not a valid difficulty. Please only use an integer valued between 1~5."});
                    return response;
                }
            }

            using (DatabaseHandler database = new())
            {
                string query = @$"
                SELECT * FROM chart
                WHERE chart.id IN ({string.Join(',', ids)})
                AND chart.diff IN ({string.Join(',', difficulties)})";

                Dictionary<long, Dictionary<long, Dictionary<long, Chart>>> charts = [];

                var result = database.Query(query);
                foreach (var diffs in result.Values)
                {
                    long song_id = (long)(diffs["id"] ?? 0);
                    long diff = (long)(diffs["diff"] ?? 4);
                    long style = (long)(diffs["style"] ?? 0);
                    long level = (long)(diffs["level"] ?? 1);
                    long normal = (long)(diffs["normal"] ?? 0);
                    long? expert = (long?)diffs["expert"];
                    long? master = (long?)diffs["master"];

                    if (song_id <= 0) continue;

                    if (!charts.ContainsKey(song_id))
                        charts[song_id] = new();
                    if (!charts[song_id].ContainsKey(diff))
                        charts[song_id][diff] = [];
                    
                    charts[song_id][diff][style] = new Chart() { level = level, normal = normal, expert = expert, master = master };
                }

                response.Body = JsonConvert.SerializeObject(charts);
            }

            return response;
        }
    }
}

