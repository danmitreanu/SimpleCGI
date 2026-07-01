
using System.Text.Json;
using SimpleCGI.Client;

var request = SimpleCGIRequest.Read();

string echoOutput = JsonSerializer.Serialize(new
{
    req_id = request.RequestId,
    method = request.Method,
    abs_path = request.AbsolutePath,
    path = request.Path
});

SimpleCGIResponse response = new(request)
{
    ContentType = "application/json",
    BodyString = echoOutput
};

response.Send();
