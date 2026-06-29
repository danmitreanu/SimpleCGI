using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleCGI.WebServer;

public class Sitemap
{
    private static readonly JsonSerializerOptions s_jsonOpts;

    public record IndexFile
    {
        public string? Path { get; set; }
        public string? ContentType { get; set; }
    }

    public record Index
    {
        public string? Exe { get; set; }
        public string[] Arguments { get; set; } = [];
        public IndexFile? File { get; set; }
        public bool ForwardPaths { get; set; } = false;
    }

    public static Index DefaultRoot { get; } = new()
    {
        File = new IndexFile()
    };

    static Sitemap()
    {
        s_jsonOpts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        s_jsonOpts.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));
    }

    public string LocalDirectory { get; set; } = string.Empty;
    public Index Root { get; set; } = DefaultRoot;
    public Dictionary<string, Sitemap> Paths { get; set; } = [];

    public static Sitemap ReadDirectory(string path)
    {
        DirectoryInfo di = new(path);
        Sitemap sitemap = new()
        {
            LocalDirectory = di.FullName
        };

        string indexJsonPath = Path.Combine(di.FullName, "index.json");
        if (File.Exists(indexJsonPath))
        {
            string indexJson = File.ReadAllText(indexJsonPath);
            var index = JsonSerializer.Deserialize<Index>(indexJson, s_jsonOpts) ??
                throw new Exception($"{indexJsonPath} cannot be null");

            sitemap.Root = index;
        }

        foreach (var subdi in di.EnumerateDirectories())
        {
            if (subdi.Name.StartsWith("__"))
                continue;

            string pathName = subdi.Name;
            sitemap.Paths.Add(pathName, ReadDirectory(subdi.FullName));
        }

        return sitemap;
    }
}
