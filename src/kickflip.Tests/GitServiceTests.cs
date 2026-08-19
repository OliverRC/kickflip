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

    /// <summary>
    /// Regression test for the rename bug that wiped a deployment: the Delete side
    /// of a rename must target the OLD deployment path, never the new one that the
    /// Add side is uploading to.
    /// </summary>
    [Fact]
    public void GetChanges_Tags_RenamedFile_DeletesOldPathAndUploadsNewPath()
    {
        using var repo = new GitRepositoryBuilder();
        var content = "<?php class Account {} // enough content for rename detection";
        repo.WriteFile("Model/Customer/Account.php", content).Commit("initial");
        repo.Tag("v1.0");

        repo.DeleteFile("Model/Customer/Account.php").WriteFile("Model/Account.php", content).Commit("move account model");

        var changes = CreateService(repo.Path).GetChanges(repo.Path, "/", FindMode.Tags);

        var add = changes.Single(c => c.Action == DeploymentAction.Add);
        var delete = changes.Single(c => c.Action == DeploymentAction.Delete);

        Assert.Equal("Model/Account.php", add.Path);
        Assert.Equal(Path.Combine("/", "Model/Account.php"), add.DeploymentPath);

        Assert.Equal("Model/Customer/Account.php", delete.Path);
        Assert.Equal(Path.Combine("/", "Model/Customer/Account.php"), delete.DeploymentPath);

        // The incident: the delete pointed at the freshly uploaded file
        Assert.NotEqual(add.DeploymentPath, delete.DeploymentPath);
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

    /// <summary>
    /// MergeBase mode reports exactly the PR's own changes: base-branch commits
    /// that were merged INTO the PR branch (and anything that landed on the base
    /// branch in parallel) must not show up, which is where GitHubMergePR goes
    /// wrong on long-lived branches.
    /// </summary>
    [Fact]
    public void GetChanges_MergeBase_OnlyReportsTheBranchesOwnChanges()
    {
        using var repo = new GitRepositoryBuilder();
        repo.WriteFile("base.txt").Commit("initial");
        var main = CurrentBranch(repo.Path);

        repo.Branch("feature").WriteFile("feature.txt").Commit("feature work");

        repo.Checkout(main).WriteFile("landed-on-main.txt").Commit("Merge pull request #1 from someone/other");

        repo.Checkout("feature").Merge(main).WriteFile("feature-2.txt").Commit("more feature work");

        var changes = CreateService(repo.Path).GetChanges(repo.Path, "/", FindMode.MergeBase, main);

        Assert.Equal(DeploymentAction.Add, changes.Single(c => c.Path == "feature.txt").Action);
        Assert.Equal(DeploymentAction.Add, changes.Single(c => c.Path == "feature-2.txt").Action);
        Assert.DoesNotContain(changes, c => c.Path == "landed-on-main.txt");
        Assert.DoesNotContain(changes, c => c.Path == "base.txt");
    }

    [Fact]
    public void GetChanges_MergeBase_ResolvesOriginPrefixedBaseRef()
    {
        using var repo = new GitRepositoryBuilder();
        repo.WriteFile("base.txt").Commit("initial");
        var main = CurrentBranch(repo.Path);
        repo.Branch("feature").WriteFile("feature.txt").Commit("feature work");
        // actions/checkout leaves the base branch as a remote-tracking ref only.
        using (var git = new LibGit2Sharp.Repository(repo.Path))
        {
            git.Refs.Add($"refs/remotes/origin/{main}", git.Branches[main].Tip.Id);
            git.Refs.Remove(git.Branches[main].CanonicalName);
        }

        var changes = CreateService(repo.Path).GetChanges(repo.Path, "/", FindMode.MergeBase, main);

        Assert.Single(changes, c => c.Path == "feature.txt");
    }

    [Fact]
    public void GetChanges_MergeBase_WithoutBaseRefOrUnknownRef_Throws()
    {
        using var repo = new GitRepositoryBuilder();
        repo.WriteFile("base.txt").Commit("initial");

        var service = CreateService(repo.Path);
        Assert.Throws<InvalidOperationException>(() => service.GetChanges(repo.Path, "/", FindMode.MergeBase, null));
        Assert.Throws<InvalidOperationException>(() => service.GetChanges(repo.Path, "/", FindMode.MergeBase, "nope"));
    }

    private static string CurrentBranch(string path)
    {
        using var git = new LibGit2Sharp.Repository(path);
        return git.Head.FriendlyName;
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
