using kickflip.Enums;
using kickflip.Models;
using kickflip.Services;
using kickflip.Tests.TestHelpers;

namespace kickflip.Tests;

public class FileSystemServiceTests
{
    [Fact]
    public void GetChanges_ReturnsAddOrModifyForEveryFile()
    {
        using var directory = new TempDirectory();
        directory.WriteFile("index.html");
        directory.WriteFile("assets/style.css");

        var service = new FileSystemService(new IgnoreService(directory.Path));
        var changes = service.GetChanges(directory.Path, "/public_html");

        Assert.All(changes.Where(c => c.Action != DeploymentAction.Ignore),
            change => Assert.Equal(DeploymentAction.AddOrModify, change.Action));
        Assert.All(changes, change => Assert.Equal(Source.Folder, change.Source));

        var index = changes.Single(c => c.Path == "index.html");
        Assert.Equal("/public_html/index.html", index.DeploymentPath);
    }

    [Fact]
    public void GetChanges_MarksIgnoredFilesAsIgnored()
    {
        using var directory = new TempDirectory();
        directory.WriteFile(".kickflipignore", "*.log");
        directory.WriteFile("index.html");
        directory.WriteFile("app.log");

        var service = new FileSystemService(new IgnoreService(directory.Path));
        var changes = service.GetChanges(directory.Path, "/");

        var log = changes.Single(c => c.Path == "app.log");
        Assert.Equal(DeploymentAction.Ignore, log.Action);
        Assert.Equal(string.Empty, log.DeploymentPath);
    }

    [Fact]
    public void GetChanges_ThrowsWhenDirectoryMissing()
    {
        var service = new FileSystemService(new IgnoreService(Path.GetTempPath()));

        Assert.Throws<DirectoryNotFoundException>(() =>
            service.GetChanges(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), "/"));
    }
}
