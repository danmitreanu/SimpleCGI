using System.Net;

namespace SimpleCGI.Types;

public record ResponseType
{
    public string RequestId { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 200;
    public string ContentType { get; set; } = "application/octet-stream";
    public Dictionary<string, List<string>> Headers { get; set; } = [];
    public List<Cookie> Cookies { get; set; } = [];
}
