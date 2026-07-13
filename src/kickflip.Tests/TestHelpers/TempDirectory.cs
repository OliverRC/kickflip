namespace kickflip.Tests.TestHelpers;

/// <summary>
/// Creates a unique temporary directory that is deleted when disposed.
/// Used to give tests an isolated file system sandbox.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kickflip-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string WriteFile(string relativePath, string contents = "")
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath);
        var directory = System.IO.Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, contents);
        return fullPath;
    }

    public void DeleteFile(string relativePath)
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                // Git repositories mark objects read-only, clear before delete.
                foreach (var file in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup; ignore failures during teardown.
        }
    }
}
