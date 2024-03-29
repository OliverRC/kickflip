﻿using kickflip.Enums;
using LibGit2Sharp;

namespace kickflip.Services;

public class GitService
{
    private readonly IgnoreService _ignoreService;

    public GitService(IgnoreService ignoreService)
    {
        _ignoreService = ignoreService;
    }

    public List<DeploymentChange> GetChanges(string path, FindMode findMode)
    {
        // string path = Environment.CurrentDirectory;
        using var repo = new Repository(path);
        
        Console.WriteLine($"Repository path: {repo.Info.Path}");
        Console.WriteLine($"Current branch: {repo.Head.FriendlyName}");
        Console.WriteLine();

        Commit? fromCommit = null;
        switch (findMode)
        {
            case FindMode.Tags:
                fromCommit = GetLastCommitByTag(repo, true);
                break;
            case FindMode.GitHubMergePR:
                fromCommit = GetLastCommitByGitHubMergePr(repo, true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(findMode), findMode, "Invalid find mode");
        }
        

        var fromCommitInfo =
            fromCommit == null ? "<root commit>" : $"\"{fromCommit.Id} {fromCommit.MessageShort}\"";
        var toCommitInfo = $"\"{repo.Head.Tip.Id} {repo.Head.Tip.MessageShort}\"";
        Console.WriteLine($"Finding diff between \"{fromCommitInfo}\" and \"{toCommitInfo}\"");

        var changes = repo.Diff.Compare<TreeChanges>(fromCommit?.Tree, repo.Head.Tip.Tree);

        if(!changes.Any())
        {
            Console.WriteLine("No changes found");
            return new List<DeploymentChange>();
        }
        
        Console.WriteLine("The following git changes were found:");
        var deploymentChanges = new List<DeploymentChange>();
        foreach (var change in changes)
        {
            Console.WriteLine($"{change.Status}    {change.Path}");
            if (change.Status == ChangeKind.Renamed)
            {
                Console.WriteLine($" - {ChangeKind.Deleted}    {change.OldPath}");
                Console.WriteLine($" - {ChangeKind.Added}    {change.Path}");
            }

            deploymentChanges.AddRange(ToDeploymentChanges(change));
        }
            
        return deploymentChanges;
    }

    private IEnumerable<DeploymentChange> ToDeploymentChanges(TreeEntryChanges change)
    {
        var changes = new List<DeploymentChange>();

        if (_ignoreService.IsIgnored(change.Path))
        {
            changes.Add(new DeploymentChange(DeploymentAction.Ignore, change.Path));
            return changes;
        }
        
        switch (change.Status)
        {
            case ChangeKind.Added:
                changes.Add(new DeploymentChange(DeploymentAction.Add, change.Path));
                break;
            case ChangeKind.Deleted:
                changes.Add(new DeploymentChange(DeploymentAction.Delete, change.Path));
                break;
            case ChangeKind.Modified:
                changes.Add(new DeploymentChange(DeploymentAction.Modify, change.Path));
                break;
            case ChangeKind.Renamed:
                changes.Add(new DeploymentChange(DeploymentAction.Add, change.Path));
                changes.Add(new DeploymentChange(DeploymentAction.Delete, change.OldPath));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(change.Status), change.Status, "Currently only Added, Deleted, Modified and Renamed are supported");
        }

        return changes;
    }

    private Commit? GetLastCommitByTag(Repository repo, bool ignoreTipTag)
    {
        var lastTag = GetLastTag(repo, true);
        return lastTag?.Target.Peel<Commit>();
    }

    private Tag? GetLastTag(Repository repo, bool ignoreTipTag)
    {
        var commitsToHead = repo.Head.Commits;
        var tagsOnCommits = repo.Tags.Where(t => t.Target.GetType() == typeof(Commit)).ToList();

        foreach (var commit in commitsToHead)
        {
            var foundTags = tagsOnCommits.Where(x => x.Target.Peel<Commit>().Id == commit.Id).ToArray();
            if (foundTags.Length <= 0)
            {
                continue;
            }

            if (foundTags.Length > 1)
            {
                Console.WriteLine($"Found more than one tag for the commit \"{commit} {commit.MessageShort}\"");
            }

            var tag = foundTags[0];
            if (ignoreTipTag && tag.Target.Peel<Commit>() == repo.Head.Tip)
            {
                Console.WriteLine($"Ignoring tag {tag} because it is the tip of the current branch and ignoreTipTag is true");
                continue;
            }

            return foundTags[0];
        }

        return null;
    }
    
    private Commit? GetLastCommitByGitHubMergePr(Repository repo, bool ignoreTip)
    {
        var commitsToHead = repo.Head.Commits;
        foreach (var commit in commitsToHead)
        {
            if (!commit.Message.StartsWith("Merge pull request #", StringComparison.InvariantCultureIgnoreCase) && 
                !commit.Message.StartsWith("Revert \"Merge pull request #", StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }
            
            if (ignoreTip && commit == repo.Head.Tip)
            {
                Console.WriteLine($"Ignoring commit {commit} because it is the tip of the current branch and ignoreTip is true");
                continue;
            }

            return commit;
        }

        return null;
    }
}