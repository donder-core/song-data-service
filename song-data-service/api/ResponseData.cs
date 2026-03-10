using System.Net;

namespace SongDataService
{
    public class ResponseData
    {
        public int StatusCode { get; set; } = (int)HttpStatusCode.OK;
        public string? StatusDescription { get; set; } = null;
        public string ContentType { get; set; } = "application/json; charset=utf-8";
        public string Body { get; set; } = "";
        public Dictionary<string, string> Headers { get; set; } = [];
    }
}

