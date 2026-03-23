using System;
using Newtonsoft.Json;

namespace SongDataService;

public class GetStats
{
    private struct Stats
    {
        public struct Region
        {
            [JsonProperty("japan")]
            public long Japan;
            [JsonProperty("asia")]
            public long Asia;
            [JsonProperty("oceania")]
            public long Oceania;
            [JsonProperty("united-states")]
            public long UnitedStates;
            [JsonProperty("china")]
            public long China;

            public Region() { Japan = 0; Asia = 0; Oceania = 0; UnitedStates = 0; China = 0; }
        }

        public struct Language
        {
            [JsonProperty("ja")]
            public long Japanese;
            [JsonProperty("en-US")]
            public long English;
            [JsonProperty("ko")]
            public long Korean;
            [JsonProperty("zh-TW")]
            public long TraditionalChinese;
            [JsonProperty("zh-CN")]
            public long SimplifiedChinese;

            public Language() { Japanese = 0; English = 0; Korean = 0; TraditionalChinese = 0; SimplifiedChinese = 0; }
        }

        [JsonProperty("total_count")]
        public long TotalSongs;
        [JsonProperty("available_all_count")]
        public long AvailableEverywhere;
        [JsonProperty("unavailable_all_count")]
        public long UnavailableEverywhere;

        [JsonProperty("available_count")]
        public Region Available;
        [JsonProperty("exclusive_count")]
        public Region Exclusive;
        [JsonProperty("excluded_count")]
        public Region Excluded;
        [JsonProperty("unknown_count")]
        public Region Unknown;

        [JsonProperty("complete_title_count")]
        public long CompleteTitleCount;
        [JsonProperty("title_count")]
        public Language TitleCount;

        public Stats() { 
            TotalSongs = 0;

            Available = new();
            Exclusive = new();
            Excluded = new();
            Unknown = new();

            AvailableEverywhere = 0;
            UnavailableEverywhere = 0;

            TitleCount = new();
            CompleteTitleCount = 0;
            }
    }

    public static async Task<ResponseData> AllStats(RequestData request)
    {
        if (!APIExtensions.ContentTypeIsAllowed(request)) return APIExtensions.UnsupportedContentType;

        ResponseData response = new();
        DatabaseHandler database = new();
        Stats stats = new();

        long Query(string query) => (long?)database.Query(query).First().Value.First().Value ?? 0;

        stats.TotalSongs = Query(
            @$"
            SELECT COUNT(*) FROM region;"
        );

        stats.Available = new()
        {
            Japan = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.japan > 0"
            ),
            Asia = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.asia > 0"
            ),
            Oceania = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.oceania > 0"
            ),
            UnitedStates = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.'united-states' > 0"
            ),
            China = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.china > 0"
            )
        };

        stats.Exclusive = new()
        {
            Japan = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.japan > 0 AND region.asia = 0 AND region.oceania = 0 AND region.'united-states' = 0 AND region.china = 0"
            ),
            Asia = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.asia > 0 AND region.japan = 0 AND region.oceania = 0 AND region.'united-states' = 0 AND region.china = 0"
            ),
            Oceania = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.oceania > 0 AND region.asia = 0 AND region.japan = 0 AND region.'united-states' = 0 AND region.china = 0"
            ),
            UnitedStates = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.'united-states' > 0 AND region.asia = 0 AND region.oceania = 0 AND region.japan = 0 AND region.china = 0"
            ),
            China = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.china > 0 AND region.asia = 0 AND region.oceania = 0 AND region.'united-states' = 0 AND region.japan = 0"
            )
        };

        stats.Excluded = new()
        {
            Japan = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.japan = 0 AND (region.asia > 0 OR region.oceania > 0 OR region.'united-states' > 0 OR region.china > 0)"
            ),
            Asia = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.asia = 0 AND (region.japan > 0 OR region.oceania > 0 OR region.'united-states' > 0 OR region.china > 0)"
            ),
            Oceania = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.oceania = 0 AND (region.asia > 0 OR region.japan > 0 OR region.'united-states' > 0 OR region.china > 0)"
            ),
            UnitedStates = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.'united-states' = 0 AND (region.asia > 0 OR region.oceania > 0 OR region.japan > 0 OR region.china > 0)"
            ),
            China = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.china = 0 AND (region.asia > 0 OR region.oceania > 0 OR region.'united-states' > 0 OR region.japan > 0)"
            )
        };

        stats.Unknown = new()
        {
            Japan = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.japan = -1"
            ),
            Asia = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.asia = -1"
            ),
            Oceania = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.oceania = -1"
            ),
            UnitedStates = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.'united-states' = -1"
            ),
            China = Query(
                @$"SELECT COUNT(*) FROM region
                WHERE region.china = -1"
            )
        };

        stats.AvailableEverywhere = Query(
            @$"SELECT COUNT(*) FROM region
            WHERE region.japan > 0 AND region.asia > 0 AND region.oceania > 0 AND region.'united-states' > 0 AND region.china > 0"
        );

        stats.UnavailableEverywhere = Query(
            @$"SELECT COUNT(*) FROM region
            WHERE region.japan = 0 AND region.asia = 0 AND region.oceania = 0 AND region.'united-states' = 0 AND region.china = 0"
        );

        stats.TitleCount = new()
        {
            Japanese = Query(
                @$"SELECT COUNT(*) FROM title
                WHERE title.ja IS NOT NULL"
            ),
            English = Query(
                @$"SELECT COUNT(*) FROM title
                WHERE title.'en-US' IS NOT NULL"
            ),
            Korean = Query(
                @$"SELECT COUNT(*) FROM title
                WHERE title.ko IS NOT NULL"
            ),
            TraditionalChinese = Query(
                @$"SELECT COUNT(*) FROM title
                WHERE title.'zh-TW' IS NOT NULL"
            ),
            SimplifiedChinese = Query(
                @$"SELECT COUNT(*) FROM title
                WHERE title.'zh-CN' IS NOT NULL"
            )
        };

        stats.CompleteTitleCount = Query(
            @$"SELECT COUNT(*) FROM title
            WHERE title.ja IS NOT NULL AND title.'en-US' IS NOT NULL AND title.ko IS NOT NULL AND title.'zh-TW' IS NOT NULL AND title.'zh-CN' IS NOT NULL"
        );

        response.Body = JsonConvert.SerializeObject(stats);

        return response;
    }
}
