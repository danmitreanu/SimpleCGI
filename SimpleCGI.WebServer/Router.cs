using SimpleCGI.Types;

namespace SimpleCGI.WebServer;

public record PageResult;
public record FileResult(string Path, string? ContentType) : PageResult;
public record ExeResult(string Exe, string[] Arguments, string WorkDir) : PageResult;

public class Router(Sitemap sitemap)
{
    public PageResult? Route(RequestType request)
    {
        string path = request.AbsolutePath;
        string[] parts = [.. path.Split('/').Where(s => !string.IsNullOrEmpty(s))];

        Sitemap lastMap = sitemap;

        for (int i = 0; i < parts.Length; i++)
        {
            string name = parts[i];

            if (lastMap.Paths.TryGetValue(name, out var submap))
            {
                lastMap = submap;
            }
            else
            {
                var root = lastMap.Root;
                string relativePath = "/" + string.Join('/', parts[i..]);
                request.Path = relativePath;

                if (root.File is not null)
                {
                    string filePath = string.IsNullOrEmpty(root.File.Path) ?
                        lastMap.LocalDirectory :
                        Path.Combine(lastMap.LocalDirectory, root.File.Path);

                    if (Directory.Exists(filePath))
                    {
                        string finalPath = Path.Combine(filePath, relativePath);
                        bool exists = File.Exists(finalPath);
                        return exists ?
                            new FileResult(finalPath, root.File.ContentType) :
                            null;
                    }
                    else if (File.Exists(filePath) && root.ForwardPaths)
                    {
                        return new FileResult(filePath, root.File.ContentType);
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (!string.IsNullOrEmpty(root.Exe) && root.ForwardPaths)
                {
                    return new ExeResult(root.Exe, root.Arguments, lastMap.LocalDirectory);
                }
                else
                {
                    return null;
                }
            }
        }

        request.Path = "/";
        if (lastMap.Root.File?.Path is not null)
        {
            return new FileResult(Path.Combine(lastMap.LocalDirectory, lastMap.Root.File.Path), lastMap.Root.File.ContentType);
        }
        else if (!string.IsNullOrEmpty(lastMap.Root.Exe))
        {
            FileInfo exeFile = new(Path.Combine(lastMap.LocalDirectory, lastMap.Root.Exe));
            string exePath = exeFile.Exists ? exeFile.FullName : lastMap.Root.Exe;
            return new ExeResult(exePath, lastMap.Root.Arguments, lastMap.LocalDirectory);
        }
        else
        {
            return null;
        }
    }
}
