using System.Net;
using System.Web;
using SimpleCGI.Types;

namespace SimpleCGI.WebServer;

public static class TypesExtensions
{
    extension(RequestType)
    {
        public static RequestType FromHttpRequest(HttpListenerRequest request, string? id = null)
        {
            RequestType req = new()
            {
                RequestId = id ?? Guid.NewGuid().ToString(),
                Method = request.HttpMethod.ToUpperInvariant(),
                AbsolutePath = request.Url?.AbsolutePath ?? "/",
                Path = request.Url?.AbsolutePath ?? "/",
                QueryString = request.Url?.Query ?? "?",
                Query = ParseQueryString(request.Url?.Query.ToString())
            };

            foreach (string? headerName in request.Headers.AllKeys)
            {
                if (headerName is null)
                    continue;

                var values = (request.Headers.GetValues(headerName) ?? []).ToList();
                req.Headers.Add(headerName, values);
            }

            req.ContentLength = request.ContentLength64;

            if (request.HasEntityBody)
                req.Body = request.InputStream;

            return req;
        }
    }

    public static Dictionary<string, string> ParseQueryString(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString))
            return [];

        if (queryString.StartsWith('?'))
            queryString = queryString[1..];

        string[] parts = queryString.Split('&');

        Dictionary<string, string> values = [];
        foreach (string part in parts)
        {
            int eqidx = part.IndexOf('=');
            string name = HttpUtility.UrlDecode(part[..eqidx]);
            string value = HttpUtility.UrlDecode(part[(eqidx + 1)..]);

            values[name] = value;
        }

        return values;
    }
}
