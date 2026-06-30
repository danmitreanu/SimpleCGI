using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleCGI.WebServer;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(Sitemap.Index))]
[JsonSerializable(typeof(Sitemap.IndexFile))]
internal partial class SitemapJsonContext : JsonSerializerContext { }

public class Sitemap
{
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

        string indexJsonPath = Path.Combine(di.FullName, "_simple.json");
        if (File.Exists(indexJsonPath))
        {
            string indexJson = File.ReadAllText(indexJsonPath);
            var index = JsonSerializer.Deserialize(indexJson, SitemapJsonContext.Default.Index) ??
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
