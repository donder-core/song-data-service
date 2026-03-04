using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SongDataService
{
    public class ServerHandler
    {
        private HttpListener _listener = new();
        private readonly string _url = "http://localhost:8181/";

        public async Task StartAsync()
        {
            _listener.Prefixes.Add(_url);
            _listener.Start();

            Console.WriteLine($"Server opened at {_url}");

            await ListenForRequestsAsync();
        }

        private async Task ListenForRequestsAsync()
        {
            while (_listener.IsListening)
            {
                var context = await _listener.GetContextAsync();
                _ = HandleRequestAsync(context);
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                Console.WriteLine($"[Request] {DateTime.Now:yyyy/MM/dd HH:mm:ss} - {request.HttpMethod} {request.Url}");

                response.Headers.Add("Access-Control-Allow-Methods", "GET, HEAD");
                response.Headers.Add("Access-Control-Allow-Origin", "*");

                string data = Uri.UnescapeDataString(request.Url?.AbsolutePath ?? "");
                Dictionary<string, string> queries = [];
                foreach (string item in (request.Url?.Query ?? "").TrimStart('?').Split('&'))
                {
                    string[] split = item.Split('=', 2);
                    if (split.Length == 2) queries[Uri.UnescapeDataString(split[0])] = Uri.UnescapeDataString(split[1]);
                }

                if (queries.Values.Any(value => Regex.IsMatch(value, @"DROP\s+TABLE") || Regex.IsMatch(value, @"TRUNCATE\s+TABLE")))
                {
                    await SendResponseAsync(response, new() {
                        StatusCode = 418,
                        StatusDescription = "Bruh Moment",
                        Body = JsonConvert.SerializeObject(new { error = 418, message = "🖕" })
                        });
                }
                else switch (request.HttpMethod)
                {
                    case "HEAD":
                        response.StatusCode = (int)HttpStatusCode.NoContent;
                        response.Close();
                        break;
                    case "GET":
                        switch (data)
                        {
                            case "/search":
                                await SendResponseAsync(response, await SearchIDs.Search(
                                    title: queries.TryGetValue("title", out var title) ? title : null,
                                    subtitle: queries.TryGetValue("subtitle", out var subtitle) ? subtitle : null,
                                    genre: queries.TryGetValue("genre", out var genre) ? (int.TryParse(genre, out var genre_result) ? genre_result : null ) : null,
                                    diff: queries.TryGetValue("diff", out var diff) ? (int.TryParse(diff, out var diff_result) ? diff_result : null ) : null,
                                    level: queries.TryGetValue("level", out var level) ? (int.TryParse(level, out var level_result) ? level_result : null ) : null,
                                    useAlias: queries.TryGetValue("use_alias", out var alias) ? alias switch { "true" => true, "false" => false, _ => null } : null
                                ));
                                break;
                            case "/diff":
                                List<long> ids = [];
                                List<long> diffs = [1,2,3,4,5];
                                if (queries.ContainsKey("id"))
                                    ids = queries["id"].Split(',').Select(item => long.TryParse(item, out long result) ? result : 0).ToList();
                                if (queries.ContainsKey("diff"))
                                    diffs = queries["diff"].Split(',').Select(item => long.TryParse(item, out long result) ? result : 0).ToList();
                                await SendResponseAsync(response, await GetDiff.GetDifficulty(ids.ToArray(), diffs.ToArray()));
                                break;
                            default:
                                response.StatusCode = (int)HttpStatusCode.NotFound;
                                response.Close();
                                break;
                        }
                        break;
                    default:
                        response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        response.Close();
                        break;
                }
                Console.WriteLine($"[Response] {DateTime.Now:yyyy/MM/dd HH:mm:ss} - {response.StatusCode} {request.Url}");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(response, ex);
            }
        }

        private async Task SendResponseAsync(HttpListenerResponse response, ResponseData responseData)
        {
            response.StatusCode = responseData.StatusCode;
            if (responseData.StatusDescription is not null) response.StatusDescription = responseData.StatusDescription;
            response.ContentType = responseData.ContentType;
            foreach (var header in responseData.Headers)
                response.Headers.Add(header.Key, header.Value);
            if (!string.IsNullOrEmpty(responseData.Body))
            {
                byte[] buffer = Encoding.UTF8.GetBytes(responseData.Body);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer);
            }

            response.Close();
        }

        private async Task HandleErrorAsync(HttpListenerResponse response, Exception ex)
        {
            response.StatusCode = 500;
            response.ContentType = "application/json";

            string error = JsonConvert.SerializeObject(new { error = "Internal Server Error", message = ex.Message });

            byte[] buffer = Encoding.UTF8.GetBytes(error);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);

            response.Close();

            Console.WriteLine($"[Response] {DateTime.Now:yyyy/MM/dd HH:mm:ss} - {response.StatusCode} - {error}");
        }
    }
}