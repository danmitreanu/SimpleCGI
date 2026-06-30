using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using SimpleCGI.Types;

namespace SimpleCGI.WebServer;

public class Executor
{
    public static async Task Execute(PageResult? route, RequestType request, HttpListenerResponse httpRes, CancellationToken ct)
    {
        if (route is FileResult fileResult)
        {
            await ExecuteFile(fileResult, request, httpRes, ct);
        }
        else if (route is ExeResult exeResult)
        {
            await ExecuteExe(exeResult, request, httpRes, ct);
        }
        else
        {
            await ExecuteError(404, "Not Found", httpRes, ct);
        }
    }

    private static async Task ExecuteExe(ExeResult exeResult, RequestType request, HttpListenerResponse httpRes, CancellationToken ct)
    {
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = exeResult.WorkDir,
                FileName = exeResult.Exe,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };

        foreach (string arg in exeResult.Arguments)
            process.StartInfo.ArgumentList.Add(arg);

        try
        {
            process.Start();

            var stdin = process.StandardInput.BaseStream;
            var stdout = process.StandardOutput.BaseStream;

            using CancellationTokenSource processCts = new(TimeSpan.FromSeconds(5));
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(processCts.Token, ct);

            await WriteRequest(stdin, request, cts.Token);
            var response = await ReadResponse(stdout, request, cts.Token);

            httpRes.StatusCode = response.StatusCode;
            httpRes.ContentType = response.ContentType;
            foreach (var (name, values) in response.Headers)
            {
                foreach (string value in values)
                    httpRes.Headers.Add(name, value);
            }

            foreach (var cookie in response.Cookies)
            {
                httpRes.AppendCookie(cookie);
            }

            await stdout.CopyToAsync(httpRes.OutputStream, cts.Token);
            await process.WaitForExitAsync(cts.Token);
        }
        catch (Exception ex)
        {
            await ExecuteError(500, "Internal Server Error", $"{ex.GetType().Name} {ex.Message}\n{ex.StackTrace}", httpRes, ct);
        }
    }

    private static async Task ExecuteFile(FileResult fileResult, RequestType request, HttpListenerResponse httpRes, CancellationToken ct)
    {
        if (request.Method != "get")
        {
            await ExecuteError(405, "Method Not Allowed", httpRes, ct);
            return;
        }

        FileInfo fi = new(fileResult.Path);
        if (!fi.Exists)
        {
            await ExecuteError(404, "Not Found", httpRes, ct);
            return;
        }

        httpRes.StatusCode = 200;
        httpRes.ContentType = fileResult.ContentType ?? DetectMediaType(fi.Extension);
        using var fstream = File.Open(fileResult.Path, FileMode.Open, FileAccess.Read);
        await fstream.CopyToAsync(httpRes.OutputStream, ct);
    }

    private static Task ExecuteError(int statusCode, string reason, HttpListenerResponse httpRes, CancellationToken ct)
        => ExecuteError(statusCode, reason, null, httpRes, ct);

    private static async Task ExecuteError(int statusCode, string reason, string? details, HttpListenerResponse httpRes, CancellationToken ct)
    {
        Console.Error.WriteLine("{0} {1}", statusCode, reason);
        if (!string.IsNullOrEmpty(details))
            Console.Error.WriteLine(details);

        httpRes.StatusCode = statusCode;
        httpRes.ContentType = "text/html";
        using StreamWriter writer = new(httpRes.OutputStream, leaveOpen: true);
        StringBuilder sb = new();
        sb.Append("<h1>").Append(statusCode).Append("</h1><h2>").Append(reason).Append("</h2>");
        await writer.WriteAsync(sb, ct);
    }

    private static async Task WriteRequest(Stream dest, RequestType req, CancellationToken ct)
    {
        StringBuilder sb = new();
        sb
            .Append("REQ_ID ")
            .AppendLine(req.RequestId)
            .Append("MTD ")
            .AppendLine(req.Method)
            .Append("ABS_PATH ")
            .AppendLine(req.AbsolutePath)
            .Append("PATH ")
            .AppendLine(req.Path)
            .Append("QUERY_S ")
            .AppendLine(req.QueryString);

        foreach (var (name, val) in req.Query)
        {
            sb.Append("QUERY ").Append(name).Append(' ').AppendLine(val);
        }

        foreach (var (name, vals) in req.Headers)
        {
            foreach (string val in vals)
                sb.Append("HEADER ").Append(name).Append(' ').AppendLine(val);
        }

        sb.Append("LEN ").AppendLine(req.ContentLength.ToString());

        using (StreamWriter writer = new(dest, leaveOpen: true))
        {
            await writer.WriteLineAsync(sb, ct);
        }

        if (req.Body is not null)
        {
            await req.Body.CopyToAsync(dest, ct);
        }
    }

    private static async Task<ResponseType> ReadResponse(Stream src, RequestType request, CancellationToken ct)
    {
        ResponseType res = new()
        {
            RequestId = request.RequestId
        };

        using var headerStream = ReadHeaderStream(src);
        using (StreamReader streamReader = new(headerStream, Encoding.ASCII, leaveOpen: true))
        {
            string? line;
            while (!string.IsNullOrEmpty(line = await streamReader.ReadLineAsync(ct)))
            {
                string[] words = [.. line.Split(' ').Where(s => !string.IsNullOrEmpty(s))];
                if (words.Length == 0)
                    break;

                string tok = words[0];
                switch (tok)
                {
                    case "STATUS":
                        res.StatusCode = int.Parse(words[1]);
                        break;
                    case "TYPE":
                        res.ContentType = words[1];
                        break;
                    case "HEADER":
                        string name = words[1];
                        string value = string.Join(' ', words[2..]);
                        if (res.Headers.TryGetValue(name, out var values))
                        {
                            values.Add(value);
                        }
                        else
                        {
                            res.Headers.Add(name, [value]);
                        }
                        break;
                    case "COOKIE":
                    {
                        string cookieName = words[1];
                        Cookie cookie = new(cookieName, string.Join(' ', words[2..]));
                        res.Cookies.Add(cookie);
                        break;
                    }
                    case "COOKIE_PATH":
                    {
                        string cookieName = words[1];
                        string path = words[2];
                        Cookie cookie = new(cookieName, string.Join(' ', words[3..]), path);
                        res.Cookies.Add(cookie);
                        break;
                    }
                    case "COOKIE_PATH_DOMAIN":
                    {
                        string cookieName = words[1];
                        string path = words[2];
                        string domain = words[3];
                        Cookie cookie = new(cookieName, string.Join(' ', words[4..]), path, domain);
                        res.Cookies.Add(cookie);
                        break;
                    }
                }
            }
        }

        return res;
    }

    private static MemoryStream ReadHeaderStream(Stream stream)
    {
        MemoryStream memStream = new();
        char lastChar = '\n';
        while (stream.CanRead)
        {
            byte b = (byte)stream.ReadByte();
            char c = (char)b;

            if (c == '\r')
                continue;

            if (c == '\n' && lastChar == '\n')
                break;

            memStream.WriteByte(b);
            lastChar = c;
        }

        memStream.Position = 0;
        return memStream;
    }

    private static string DetectMediaType(string extension)
    {
        return extension switch
        {
            ".html" => "text/html",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpeg" => "image/jpeg",
            ".jpg" => "image/jpeg",
            ".xml" => "application/xml",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
