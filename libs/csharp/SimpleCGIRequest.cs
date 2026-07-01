namespace SimpleCGI.Client;

public class SimpleCGIRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string AbsolutePath { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public Dictionary<string, string> Query { get; } = [];
    public Dictionary<string, List<string>> Headers { get; } = [];

    public Stream BodyStream { get; set; } = null!;

    public string ReadBodyAsString()
    {
        using StreamReader reader = new(BodyStream);
        return reader.ReadToEnd();
    }

    public Task<string> ReadBodyAsStringAsync(CancellationToken ct = default)
    {
        using StreamReader reader = new(BodyStream);
        return reader.ReadToEndAsync(ct);
    }

    public static SimpleCGIRequest Read() => Read(Console.OpenStandardInput());

    public static SimpleCGIRequest Read(Stream stream) => ReadAsync(stream).Result;

    public static Task<SimpleCGIRequest> ReadAsync(CancellationToken ct = default) => ReadAsync(Console.OpenStandardInput(), ct);

    public static async Task<SimpleCGIRequest> ReadAsync(Stream stream, CancellationToken ct = default)
    {
        SimpleCGIRequest request = new();

        using MemoryStream memStream = new();

        char lastChar = '\n';
        byte[] oneByte = new byte[1];

        while (stream.CanRead)
        {
            await stream.ReadExactlyAsync(oneByte, ct);
            byte b = oneByte[0];
            char c = (char)b;

            if (c == '\r')
                continue;

            if (c == '\n' && lastChar == '\n')
                break;

            memStream.WriteByte(b);
            lastChar = c;
        }

        memStream.Position = 0;

        using (StreamReader reader = new(memStream))
        {
            string? line;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(ct)))
            {
                string[] toks = line.Split(' ');
                switch (toks[0])
                {
                    case "REQ_ID":
                        request.RequestId = toks[1];
                        break;
                    case "MTD":
                        request.Method = toks[1];
                        break;
                    case "ABS_PATH":
                        request.AbsolutePath = string.Join(' ', toks[1..]);
                        break;
                    case "PATH":
                        request.Path = string.Join(' ', toks[1..]);
                        break;
                    case "QUERY_S":
                        request.QueryString = string.Join(' ', toks[1..]);
                        break;
                    case "QUERY":
                        {
                            string name = toks[1];
                            string value = string.Join(' ', toks[2..]);
                            request.Query[name] = value;
                        }
                        break;
                    case "HEADER":
                        {
                            string name = toks[1];
                            string value = string.Join(' ', toks[2..]);
                            if (request.Headers.TryGetValue(name, out var valueList))
                            {
                                valueList.Add(value);
                            }
                            else
                            {
                                request.Headers.Add(name, [value]);
                            }
                        }
                        break;
                }
            }
        }

        request.BodyStream = stream;
        return request;
    }
}
