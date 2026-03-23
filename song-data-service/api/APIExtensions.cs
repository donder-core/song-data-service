using System;
using System.Net;
using Newtonsoft.Json;

namespace SongDataService;

public class APIExtensions
{
    public static bool ContentTypeIsAllowed(RequestData request, IEnumerable<string>? types = null)
    {
        List<string> valid_types = types?.ToList() ?? ["application/json", "*/*"];
        return request.Headers.TryGetValue("accept", out string? type) && valid_types.Any(type.Contains);
    }

    public static ResponseData UnsupportedContentType
    {
        get
        {
            return new()
            {
                StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                Body = JsonConvert.SerializeObject(new ErrorData((int)HttpStatusCode.UnsupportedMediaType, "None of the content type(s) requested are supported."))
            };
        }
    }
}
