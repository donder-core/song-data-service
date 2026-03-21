using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SongDataService
{
    public class ServerHandler
    {
        private HttpListener _listener = new();

        public async Task StartAsync()
        {
            _listener.Prefixes.Add(APISettings.SERVER_URL);
            _listener.Start();

            Console.WriteLine($"Server opened at {APISettings.SERVER_URL}");

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

                RequestData requestData = await RequestParser.ParseAsync(request);

                response.Headers.Add("Access-Control-Allow-Methods", "GET, HEAD");
                response.Headers.Add("Access-Control-Allow-Origin", "*");

                string data = Uri.UnescapeDataString(request.Url?.AbsolutePath ?? "");
                Dictionary<string, string> queries = [];
                string query = request.Url?.Query ?? "";
                foreach (string item in query.TrimStart('?').Split('&'))
                {
                    string[] split = item.Split('=', 2);
                    if (split.Length == 2) queries[Uri.UnescapeDataString(split[0])] = Uri.UnescapeDataString(split[1]);
                }

                if (Regex.IsMatch(Uri.UnescapeDataString(query), @"DROP\s+TABLE", RegexOptions.IgnoreCase) || Regex.IsMatch(Uri.UnescapeDataString(query), @"TRUNCATE\s+TABLE", RegexOptions.IgnoreCase) || Regex.IsMatch(Uri.UnescapeDataString(query), @"DELETE\s+FROM", RegexOptions.IgnoreCase))
                {
                    await SendResponseAsync(response, new() {
                        StatusCode = 418,
                        StatusDescription = "Bruh Moment",
                        Body = JsonConvert.SerializeObject(new ErrorData(418, "Cringe requests are forbidden."))
                        });
                }
                else
                {
                    switch (data)
                    {
                        case "/search":
                            response.AddHeader("Vary", "Accept");
                            break;
                    }
                    switch (request.HttpMethod)
                    {
                        // case "OPTIONS":
                        //     break;
                        case "HEAD":
                            response.StatusCode = (int)HttpStatusCode.NoContent;
                            response.Close();
                            break;
                        case "GET":
                            switch (data)
                            {
                                case "/search":
                                    await SendResponseAsync(response, await SearchIDs.Search(
                                        request: requestData,
                                        title: queries.TryGetValue("title", out var title) ? title : null,
                                        subtitle: queries.TryGetValue("subtitle", out var subtitle) ? subtitle : null,
                                        genre: queries.TryGetValue("genre", out var genre) ? (int.TryParse(genre, out var genre_result) ? genre_result : null ) : null,
                                        diff: queries.TryGetValue("diff", out var diff) ? (int.TryParse(diff, out var diff_result) ? diff_result : null ) : null,
                                        level: queries.TryGetValue("level", out var level) ? (int.TryParse(level, out var level_result) ? level_result : null ) : null,
                                        includeSayonara: queries.TryGetValue("include_sayonara", out var sayonara) ? sayonara switch { "true" => true, "false" => false, _ => null } : null,
                                        useAlias: queries.TryGetValue("use_alias", out var alias) ? alias switch { "true" => true, "false" => false, _ => null } : null,
                                        titleComparison: queries.TryGetValue("title_comparison", out var title_comparison) ? title_comparison switch { "and" => true, "or" => false, _ => null } : null,
                                        limit: queries.TryGetValue("limit", out var limit) ? (int.TryParse(limit, out var limit_result) ? limit_result : null) : null
                                    ));
                                    break;
                                case "/song":
                                    long[] song_ids = [];
                                    if (queries.ContainsKey("id"))
                                        song_ids = queries["id"].Split(',').Select(item => long.TryParse(item, out long result) ? result : 0).ToArray();
                                    await SendResponseAsync(response, await GetSong.Songs(requestData, song_ids));
                                    break;
                                case "/random":
                                    await SendResponseAsync(response, await GetSong.RandomSongs(
                                        request: requestData,
                                        includeSayonara: queries.TryGetValue("include_sayonara", out var ran_sayonara) ? ran_sayonara switch { "true" => true, "false" => false, _ => null } : null,
                                        limit: queries.TryGetValue("limit", out var ran_limit) ? (int.TryParse(ran_limit, out int ran_limit_result) ? ran_limit_result : null) : null
                                    ));
                                    break;
                                case "/docs":
                                    await SendResponseAsync(response, new()
                                    {
                                        ContentType = "text/html; charset=utf-8",
                                        Body = File.ReadAllText("doc/doc.html")
                                    });
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
            response.ContentType = "application/json; charset=utf-8";

            string error = JsonConvert.SerializeObject(new ErrorData(response.StatusCode, ex.Message));

            byte[] buffer = Encoding.UTF8.GetBytes(error);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);

            response.Close();

            Console.WriteLine($"[Response] {DateTime.Now:yyyy/MM/dd HH:mm:ss} - {response.StatusCode} - {ex}");
        }
    }
}