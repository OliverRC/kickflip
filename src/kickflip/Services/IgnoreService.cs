using Microsoft.Extensions.FileSystemGlobbing;

namespace kickflip.Services;

public class IgnoreService
{
    private readonly Matcher _matcher;

    public IgnoreService(string localPath)
    {
        _matcher = new Matcher();
        _matcher.AddInclude("**/*.kickflipignore");
        
        var ignoreFile = Path.Combine(localPath, ".kickflipignore");
        if (!File.Exists(ignoreFile))
        {
            return;
        }
        
        var ignorePatterns = File.ReadAllLines(ignoreFile);
        _matcher.AddIncludePatterns(ignorePatterns);
    }
    
    public bool IsIgnored(string path)
    {
        return _matcher.Match(path).HasMatches;
    }
}