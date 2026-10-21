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
        
        foreach (var pattern in File.ReadAllLines(ignoreFile))
        {
            // "!pattern" re-includes files an earlier pattern ignored; unlike .gitignore, order doesn't matter
            if (pattern.StartsWith('!'))
            {
                _matcher.AddExclude(pattern[1..]);
            }
            else
            {
                _matcher.AddInclude(pattern);
            }
        }
    }
    
    public bool IsIgnored(string path)
    {
        return _matcher.Match(path).HasMatches;
    }
}