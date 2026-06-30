
using System.Net;
using SimpleCGI.Types;
using SimpleCGI.WebServer;

string url = args.Length >= 1 ? args[0] : "http://localhost:8080";
string wwwroot = args.Length >= 2 ? args[1] : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

if (!url.EndsWith('/'))
    url = $"{url}/";

Sitemap sitemap = Sitemap.ReadDirectory(wwwroot);
Router router = new(sitemap);

CancellationTokenSource cts = new();
var ct = cts.Token;

Console.CancelKeyPress += (sender, evt) =>
{
    cts.Cancel();
    evt.Cancel = true;
};

HttpListener listener = new();
listener.Prefixes.Add(url);

listener.Start();
Console.WriteLine("Started at {0}", url);

while (!ct.IsCancellationRequested)
{
    try
    {
        var ctx = await listener.GetContextAsync().WaitAsync(ct);
        try
        {
            var request = RequestType.FromHttpRequest(ctx.Request);
            var routeResult = router.Route(request);
            await Executor.Execute(routeResult, request, ctx.Response, ct);
        }
        catch (OperationCanceledException)
        {
            ctx.Response.StatusCode = 504;
        }
        finally
        {
            ctx.Response.Headers["server"] = "SimpleCGI";
            ctx.Response.Close();
        }
    }
    catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
    {
        Console.WriteLine("Stopped.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Uncaught exception while accepting HTTP context {0}: {1}\n{2}", ex.GetType().Name, ex.Message, ex.StackTrace);
    }
}
