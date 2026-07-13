using kickflip.Services;
using kickflip.Tests.TestHelpers;

namespace kickflip.Tests;

public class IgnoreServiceTests
{
    [Fact]
    public void IsIgnored_WithNoIgnoreFile_OnlyIgnoresKickflipIgnoreFiles()
    {
        using var directory = new TempDirectory();
        var service = new IgnoreService(directory.Path);

        Assert.False(service.IsIgnored("index.html"));
        Assert.True(service.IsIgnored(".kickflipignore"));
    }

    [Fact]
    public void IsIgnored_HonoursPatternsInIgnoreFile()
    {
        using var directory = new TempDirectory();
        directory.WriteFile(".kickflipignore", "*.log\nsecrets/**");
        var service = new IgnoreService(directory.Path);

        Assert.True(service.IsIgnored("app.log"));
        Assert.True(service.IsIgnored("secrets/password.txt"));
        Assert.False(service.IsIgnored("index.html"));
    }

    [Fact]
    public void IsIgnored_AlwaysIgnoresTheIgnoreFileItself()
    {
        using var directory = new TempDirectory();
        directory.WriteFile(".kickflipignore", "*.log");
        var service = new IgnoreService(directory.Path);

        Assert.True(service.IsIgnored(".kickflipignore"));
    }
}
