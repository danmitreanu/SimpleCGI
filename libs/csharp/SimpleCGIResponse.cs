using System.Text;

namespace SimpleCGI.Client;

public class SimpleCGIResponse(SimpleCGIRequest request)
{
    public class Cookie
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
    }

    public string RequestId { get; set; } = request.RequestId;
    public int StatusCode { get; set; } = 200;
    public string ContentType { get; set; } = "application/octet-stream";
    public Dictionary<string, List<string>> Headers { get; } = [];
    public List<Cookie> Cookies { get; } = [];

    public Func<Stream, CancellationToken, Task>? BodyWriterAsync { private get; set; }
    public Action<Stream> BodyWriter
    {
        set
        {
            BodyWriterAsync = (stream, _) =>
            {
                value(stream);
                return Task.CompletedTask;
            };
        }
    }

    public Encoding BodyStringEncoding { get; set; } = Encoding.UTF8;
    public string BodyString
    {
        set
        {
            byte[] bytes = BodyStringEncoding.GetBytes(value);
            BodyWriterAsync = async (stream, ct) =>
            {
                await stream.WriteAsync(bytes, ct);
            };
        }
    }

    public void Send() => Send(Console.OpenStandardOutput());

    public void Send(Stream stream) => SendAsync(stream).Wait();

    public Task SendAsync(CancellationToken ct = default) => SendAsync(Console.OpenStandardOutput(), ct);

    public async Task SendAsync(Stream stream, CancellationToken ct = default)
    {
        StringBuilder sb = new();
        sb
            .Append("REQ_ID ").AppendLine(RequestId)
            .Append("STATUS ").Append(StatusCode).AppendLine()
            .Append("TYPE ").AppendLine(ContentType);

        foreach (var (name, values) in Headers)
        {
            foreach (string value in values)
                sb.Append("HEADER ").Append(name).Append(' ').AppendLine(value);
        }

        foreach (var cookie in Cookies)
        {
            if (!string.IsNullOrEmpty(cookie.Domain))
            {
                string path = string.IsNullOrEmpty(cookie.Path) ? "/" : cookie.Path;
                sb
                    .Append("COOKIE_PATH_DOMAIN ")
                    .Append(cookie.Name).Append(' ')
                    .Append(path).Append(' ')
                    .Append(cookie.Domain).Append(' ')
                    .AppendLine(cookie.Value);
            }
            else if (!string.IsNullOrEmpty(cookie.Domain))
            {
                sb
                    .Append("COOKIE_PATH ")
                    .Append(cookie.Name).Append(' ')
                    .Append(cookie.Path).Append(' ')
                    .AppendLine(cookie.Value);
            }
        }

        using (StreamWriter writer = new(stream, leaveOpen: true))
        {
            await writer.WriteLineAsync(sb, ct);
        }

        if (BodyWriterAsync is not null)
        {
            await BodyWriterAsync(stream, ct);
        }
    }
}
