using kickflip.Tests.TestHelpers;

namespace kickflip.Tests;

/// <summary>
/// End-to-end tests that drive the compiled CLI as an external process,
/// verifying command wiring, argument validation, the different deployment
/// modes and that a dry run makes no remote changes.
/// </summary>
[Collection("CLI")]
public class CliIntegrationTests
{
    [Fact]
    public void Help_ListsTopLevelCommands()
    {
        var result = CliRunner.Run("--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("deploy", result.Output);
        Assert.Contains("github", result.Output);
    }

    [Fact]
    public void Deploy_Help_DescribesModeAndDryOptions()
    {
        var result = CliRunner.Run("deploy", "--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--mode", result.Output);
        Assert.Contains("--dry", result.Output);
        Assert.Contains("--hostname", result.Output);
    }

    [Fact]
    public void Deploy_MissingRequiredOptions_FailsWithError()
    {
        var result = CliRunner.Run("deploy");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("--hostname", result.Output);
    }

    [Fact]
    public void Deploy_TagsMode_DryRun_ReportsChangesAndDoesNotConnect()
    {
        using var repo = new GitRepositoryBuilder();
        repo.WriteFile("base.txt").Commit("initial");
        repo.Tag("v1.0");
        repo.WriteFile("added.txt").Commit("add file");

        var result = CliRunner.Run(
            "deploy", repo.Path,
            "--mode", "Tags",
            "--hostname", "nonexistent.invalid",
            "--username", "user",
            "--password", "password",
            "--dry");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Dry run", result.Output);
        Assert.Contains("added.txt", result.Output);
        Assert.Contains("Deployment successful!", result.Output);
        // A dry run must never connect to the remote server.
        Assert.DoesNotContain("Connecting to remote server", result.Output);
    }

    [Fact]
    public void Deploy_FolderMode_DryRun_UploadsFolderContents()
    {
        using var directory = new TempDirectory();
        directory.WriteFile("index.html");
        directory.WriteFile("assets/style.css");

        var result = CliRunner.Run(
            "deploy", directory.Path,
            "--mode", "Folder",
            "--deployment-path", "/public_html",
            "--hostname", "nonexistent.invalid",
            "--username", "user",
            "--password", "password",
            "--dry");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("index.html", result.Output);
        Assert.Contains("style.css", result.Output);
        Assert.Contains("Deployment successful!", result.Output);
        Assert.DoesNotContain("Connecting to remote server", result.Output);
    }

    [Fact]
    public void Deploy_GitHubMergePrMode_DryRun_ReportsPostMergeChanges()
    {
        using var repo = new GitRepositoryBuilder();
        repo.WriteFile("base.txt").Commit("initial");
        repo.WriteFile("merged.txt").Commit("Merge pull request #1 from feature/a");
        repo.WriteFile("after.txt").Commit("work after merge");

        var result = CliRunner.Run(
            "deploy", repo.Path,
            "--mode", "GitHubMergePR",
            "--hostname", "nonexistent.invalid",
            "--username", "user",
            "--password", "password",
            "--dry");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("after.txt", result.Output);
        Assert.Contains("Deployment successful!", result.Output);
    }

    [Fact]
    public void Github_Help_DescribesPullRequestCommand()
    {
        var result = CliRunner.Run("github", "--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("pull-request", result.Output);
    }

    [Fact]
    public void Github_PullRequest_InvalidRepoFormat_FailsWithGuidance()
    {
        var result = CliRunner.Run(
            "github", "pull-request",
            "--repo", "invalidformat",
            "--ref", "refs/pull/1/merge",
            "--token", "token");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("owner", result.Output.ToLowerInvariant());
    }

    [Fact]
    public void Github_PullRequest_InvalidRefFormat_FailsWithGuidance()
    {
        var result = CliRunner.Run(
            "github", "pull-request",
            "--repo", "owner/repo",
            "--ref", "refs/heads/main",
            "--token", "token");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("refs/pull/", result.Output);
    }
}
