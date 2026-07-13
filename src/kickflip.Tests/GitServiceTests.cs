using kickflip.Enums;
using kickflip.Models;
using kickflip.Services;
using kickflip.Tests.TestHelpers;

namespace kickflip.Tests;

public class GitServiceTests
{
    private static GitService CreateService(string path) => new(new IgnoreService(path));

    [Fact]
    public void GetChanges_Tags_ComparesFromLastTagToHead()
    {
        using var repo = new GitRepositoryBuilder();
        repo.WriteFile("kept.txt").Commit("initial");
        repo.Tag("v1.0");

        repo.WriteFile("added.txt").Commit("add new file");

        var changes = CreateService(repo.Path).GetChanges(repo.Path, "/", FindMode.Tags);

        var added = changes.Single(c => c.Path == "added.txt");
        Assert.Equal(DeploymentAction.Add, added.Action);
        Assert.Equal(Source.Git, added.Source);
        Assert.DoesNotContain(changes, c => c.Path == "kept.txt");
    }

    [Fact]
    public void GetChanges_Tags_DetectsModifiedAndDeletedFiles()
    {
        using var repo = new GitRepositoryBuilder();
        repo.WriteFile("modify.txt", "v1").WriteFile("delete.txt", "bye").Commit("initial");
        repo.Tag("v1.0");

        repo.WriteFile("modify.txt", "v2").DeleteFile("delete.txt").Commit("changes");

        var changes = CreateService(repo.Path).GetChanges(repo.Path, "/", FindMode.Tags);

        Assert.Equal(DeploymentAction.Modify, changes.Single(c => c.Path == "modify.txt").Action);
        Assert.Equal(DeploymentAction.Delete, changes.Single(c => c.Path == "delete.txt").Action);
    }

    [Fact]
    public void GetChanges_Tags_WithNoTag_ComparesFromRoot()
    {
        using var repo = new GitRepositoryBuilder();
        repo.WriteFile("one.txt").Commit("initial");
        repo.WriteFile("two.txt").Commit("second");

        var changes = CreateService(repo.Path).GetChanges(repo.Path, "/", FindMode.Tags);

        Assert.Contains(changes, c => c.Path == "one.txt");
        Assert.Contains(changes, c => c.Path == "two.txt");
    }

    [Fact]
    public void GetChanges_GitHubMergePr_ComparesFromLastMergeCommit()
    {
        using var repo = new GitRepositoryBuilder();
        repo.WriteFile("base.txt").Commit("initial");
        repo.WriteFile("merged.txt").Commit("Merge pull request #1 from feature/a");
        repo.WriteFile("after-merge.txt").Commit("work after merge");

        var changes = CreateService(repo.Path).GetChanges(repo.Path, "/", FindMode.GitHubMergePR);

        Assert.Contains(changes, c => c.Path == "after-merge.txt");
        Assert.DoesNotContain(changes, c => c.Path == "base.txt");
    }

    [Fact]
    public void GetChanges_IgnoredFilesAreMarkedIgnored()
    {
        using var repo = new GitRepositoryBuilder();
        repo.WriteFile(".kickflipignore", "*.log").Commit("initial");
        repo.Tag("v1.0");
        repo.WriteFile("app.log").Commit("add log");

        var changes = CreateService(repo.Path).GetChanges(repo.Path, "/", FindMode.Tags);

        Assert.Equal(DeploymentAction.Ignore, changes.Single(c => c.Path == "app.log").Action);
    }

    [Fact]
    public void GetChanges_AppliesDeploymentPathPrefix()
    {
        using var repo = new GitRepositoryBuilder();
        repo.WriteFile("base.txt").Commit("initial");
        repo.Tag("v1.0");
        repo.WriteFile("sub/file.txt").Commit("add nested file");

        var changes = CreateService(repo.Path).GetChanges(repo.Path, "/public_html", FindMode.Tags);

        var change = changes.Single(c => c.Path == "sub/file.txt");
        Assert.Contains("public_html", change.DeploymentPath);
    }
}
