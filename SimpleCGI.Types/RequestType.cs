namespace SimpleCGI.Types;

public record RequestType
{
    public string RequestId { get; set; } = string.Empty;
    public string Method { get; set; } = "get";
    public string AbsolutePath { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public Dictionary<string, string> Query { get; set; } = [];
    public Dictionary<string, List<string>> Headers { get; set; } = [];
    public long ContentLength { get; set; } = 0;
    public Stream? Body { get; set; }
}
