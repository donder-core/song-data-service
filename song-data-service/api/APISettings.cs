using System;

namespace SongDataService;

public static class APISettings
{
    public static readonly int SONG_LIMIT = int.TryParse(Environment.GetEnvironmentVariable(nameof(SONG_LIMIT)), out int limit) ? limit : 50;
    public static readonly string SERVER_URL = Environment.GetEnvironmentVariable(nameof(SERVER_URL)) ?? "http://localhost:8181/";
}
