namespace SongDataService
{
    public static class SqliteExtensions
    {
        public static string HtmlDecode(this string input) => System.Net.WebUtility.HtmlDecode(input);
        public static string Sqlite_UnescapeText(this string input) => input.Replace("''", "'");
        public static string Sqlite_EscapeForText(this string input) => input.Replace("'", "''");
        public static string Sqlite_EscapeForLike(this string input) => input
        .Replace($"{Sqlite_EscapeChar}", $"{Sqlite_EscapeChar}{Sqlite_EscapeChar}")
        .Replace("%", Sqlite_EscapeChar+"%")
        .Replace("_", Sqlite_EscapeChar+"_")
        .Sqlite_EscapeForText();
        public static readonly char Sqlite_EscapeChar = '\\';
    }
}