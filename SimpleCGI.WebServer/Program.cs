
using System.Net;
using System.Threading.Channels;
using SimpleCGI.Types;
using SimpleCGI.WebServer;

if (args.Length != 0 && args[0] == "help")
{
    Console.WriteLine("SimpleCGI.WebServer [host_url] [wwwroot_path] [concurrency] [request_queue_size]");
    return;
}

string url = args.Length >= 1 ? args[0] : "http://localhost:8080";
string wwwroot = args.Length >= 2 ? args[1] : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
int concurrency = args.Length >= 3 ? int.Parse(args[2]) : Environment.ProcessorCount;
int queueSize = args.Length >= 4 ? int.Parse(args[3]) : 10_000;

if (!url.EndsWith('/'))
    url = $"{url}/";

var requestChannel = Channel.CreateBounded<HttpListenerContext>(new BoundedChannelOptions(queueSize)
{
    FullMode = BoundedChannelFullMode.Wait
});

Sitemap sitemap = Sitemap.ReadDirectory(wwwroot);
Router router = new(sitemap);

CancellationTokenSource cts = new();
var ct = cts.Token;

Console.CancelKeyPress += (sender, evt) =>
{
    cts.Cancel();
    requestChannel.Writer.TryComplete();
    evt.Cancel = true;
};

HttpListener listener = new();
listener.Prefixes.Add(url);

var workers = Enumerable.Range(0, concurrency).Select(_ => Task.Run(() => Process(router, requestChannel, ct))).ToArray();

listener.Start();
Console.WriteLine("Started at {0}", url);

while (!ct.IsCancellationRequested)
{
    try
    {
        var ctx = await listener.GetContextAsync().WaitAsync(ct);
        try
        {
            await requestChannel.Writer.WriteAsync(ctx, ct);
        }
        catch (OperationCanceledException)
        {
            ctx.Response.StatusCode = 504;
            ctx.Response.Close();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to add request to queue {0}: {1}\n{2}", ex.GetType().Name, ex.Message, ex.StackTrace);
            ctx.Response.StatusCode = 500;
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

await Task.WhenAll(workers);

static async Task Process(Router router, Channel<HttpListenerContext> chan, CancellationToken ct)
{
    try
    {
        await foreach (var ctx in chan.Reader.ReadAllAsync(ct))
        {
            try
            {
                var request = RequestType.FromHttpRequest(ctx.Request);
                var routeResult = router.Route(request);
                Console.WriteLine("Executing {0}", request.AbsolutePath);
                await Executor.Execute(routeResult, request, ctx.Response, ct);
            }
            catch (OperationCanceledException)
            {
                ctx.Response.StatusCode = 504;
            }
            catch (Exception ex)
            {
                ctx.Response.StatusCode = 500;
                Console.Error.WriteLine("Uncaught exception while executing HTTP request {0}: {1}\n{2}", ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            finally
            {
                ctx.Response.Headers["server"] = "SimpleCGI";
                ctx.Response.Close();
            }
        }
    }
    catch (OperationCanceledException) {}
    catch (Exception ex)
    {
        Console.Error.WriteLine("Executor ended unexpectedly with exception {0}: {1}\n{2}", ex.GetType().Name, ex.Message, ex.StackTrace);
    }
}
