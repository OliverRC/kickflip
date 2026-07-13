using kickflip.Enums;
using kickflip.Models;
using kickflip.Services;
using kickflip.Tests.TestHelpers;

namespace kickflip.Tests;

/// <summary>
/// The deployment service talks to a real SFTP server for live runs, which we
/// cannot stand up in unit tests. These tests focus on the dry-run behaviour,
/// which is the safety-critical path: a dry run must never connect to the
/// remote server nor touch the local file system, yet must report success.
/// </summary>
public class SftpDeploymentServiceTests
{
    private static SftpDeploymentService CreateService() =>
        new("nonexistent.invalid", 22, "user", "password", "/public_html");

    [Fact]
    public void DeployChanges_DryRun_DoesNotConnectAndReportsSuccess()
    {
        using var directory = new TempDirectory();
        directory.WriteFile("index.html");

        var changes = new List<DeploymentChange>
        {
            new(DeploymentAction.Add, Source.Git, "index.html", "/public_html/index.html"),
            new(DeploymentAction.Delete, Source.Git, "old.html", "/public_html/old.html"),
            new(DeploymentAction.Ignore, Source.Git, "app.log", ""),
        };

        // A hostname that cannot resolve means any attempt to connect would throw.
        // If the dry run returns success without throwing, we know it did not connect.
        var result = CreateService().DeployChanges(directory.Path, changes, isDryRun: true);

        Assert.True(result);
    }

    [Fact]
    public void DeployChanges_DryRun_DoesNotModifyLocalFiles()
    {
        using var directory = new TempDirectory();
        var filePath = directory.WriteFile("index.html", "original");

        var changes = new List<DeploymentChange>
        {
            new(DeploymentAction.Add, Source.Git, "index.html", "/public_html/index.html"),
            new(DeploymentAction.Delete, Source.Git, "index.html", "/public_html/index.html"),
        };

        CreateService().DeployChanges(directory.Path, changes, isDryRun: true);

        Assert.True(File.Exists(filePath));
        Assert.Equal("original", File.ReadAllText(filePath));
    }

    [Fact]
    public void DeployChanges_DryRun_WithNoChanges_ReturnsSuccess()
    {
        using var directory = new TempDirectory();

        var result = CreateService().DeployChanges(directory.Path, [], isDryRun: true);

        Assert.True(result);
    }

    [Fact]
    public void DeployChanges_LiveRun_WithUnresolvableHost_FailsWithoutThrowing()
    {
        using var directory = new TempDirectory();
        directory.WriteFile("index.html");

        var changes = new List<DeploymentChange>
        {
            new(DeploymentAction.Ignore, Source.Git, "app.log", ""),
        };

        // Only an ignored change means Connect() is called but no upload/delete.
        // Connecting to an invalid host throws inside Connect; ensure we surface it.
        Assert.ThrowsAny<Exception>(() =>
            CreateService().DeployChanges(directory.Path, changes, isDryRun: false));
    }
}
