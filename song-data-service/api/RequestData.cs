using System.Collections.Specialized;
using System.Net;

namespace SongDataService
{
    public class RequestData
    {
        public string Method { get; set; } = "";
        public Uri? Url { get; set; }
        public Dictionary<string, string> Headers { get; set; } = [];
        public Dictionary<string, string> Parameters { get; set; } = [];
        public string Body { get; set; } = "";
    }

    public class RequestParser
    {
        public static async Task<RequestData> ParseAsync(HttpListenerRequest request)
        {
            return new RequestData
            {
            Method = request.HttpMethod,
            Url = request.Url,
            Headers = ParseHeader(request.Headers),
            Parameters = ParseQuery(request.Url?.Query ?? ""),
            Body = await ReadBodyAsync(request)
            };
        }

        private static Dictionary<string, string> ParseHeader(NameValueCollection headers)
        {
            Dictionary<string, string> result = [];
            foreach (string? key in headers.AllKeys)
            {
                if (key is null) continue;
                result[key] = headers[key] ?? "";
            }
            return result;
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            if (string.IsNullOrEmpty(query) || !query.Contains('?')) return [];

            Dictionary<string, string> parameters = [];
            string[] pairs = query.TrimStart('?').Split('&');

            foreach (string pair in pairs)
            {
                var key_value = pair.Split('=');
                if (key_value.Length == 2)
                    parameters[Uri.UnescapeDataString(key_value[0])] = Uri.UnescapeDataString(key_value[1]);
            }

            return parameters;
        }

        private static async Task<string> ReadBodyAsync(HttpListenerRequest request)
        {
            if (request.ContentLength64 == 0) return "";

            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            return await reader.ReadToEndAsync();
        }
    }
}