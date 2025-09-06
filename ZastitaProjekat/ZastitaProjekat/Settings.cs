using System;
using System.IO;
using System.Text.Json;

public class AppSettings
{
    public string TargetFolder { get; set; }
    public string EncryptedFolder { get; set; }
    public string ReceivedFolder { get; set; }
}

public static class Settings
{
    public static string PathForConfigFile => Path.Combine(GetProjectRoot(), "appsettings.json");

    public static AppSettings Load()
    {
        var configPath = PathForConfigFile;

        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s != null &&
                    !string.IsNullOrWhiteSpace(s.TargetFolder) &&
                    !string.IsNullOrWhiteSpace(s.EncryptedFolder) &&
                    !string.IsNullOrWhiteSpace(s.ReceivedFolder))
                {

                    var root = GetProjectRoot();
                    if (!IsUnderRoot(s.TargetFolder, root) ||
                        !IsUnderRoot(s.EncryptedFolder, root) ||
                        !IsUnderRoot(s.ReceivedFolder, root))
                    {
                        s.TargetFolder = Path.Combine(root, "Target");
                        s.EncryptedFolder = Path.Combine(root, "X");
                        s.ReceivedFolder = Path.Combine(root, "PrimljeniFajlovi");
                        Save(s);
                    }
                    return s;
                }
            }
        }
        catch
        {

        }


        var projRoot = GetProjectRoot();
        var defaults = new AppSettings
        {
            TargetFolder = Path.Combine(projRoot, "Target"),
            EncryptedFolder = Path.Combine(projRoot, "X"),
            ReceivedFolder = Path.Combine(projRoot, "PrimljeniFajlovi")
        };
        Save(defaults);
        return defaults;
    }

    public static void Save(AppSettings s)
    {
        var configPath = PathForConfigFile;
        var json = JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        File.WriteAllText(configPath, json);
    }


    private static string GetProjectRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);


        for (int i = 0; i < 6 && dir != null; i++)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }


        dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        for (int i = 0; i < 3 && dir?.Parent != null; i++)
            dir = dir.Parent!;
        return dir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    private static bool IsUnderRoot(string path, string root)
    {
        try
        {
            var full = Path.GetFullPath(path);
            var fullRoot = Path.GetFullPath(root);
            return full.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }
}
