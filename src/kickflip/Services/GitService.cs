using kickflip.Enums;
using LibGit2Sharp;

namespace kickflip.Services;

public class GitService(IgnoreService ignoreService)
{
    public List<DeploymentChange> GetChanges(string path, string deploymentPath, FindMode findMode)
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
        
        var deploymentChanges = new List<DeploymentChange>();
        foreach (var change in changes)
        {
            deploymentChanges.AddRange(ToDeploymentChanges(change, deploymentPath));
        }
            
        return deploymentChanges;
    }

    private IEnumerable<DeploymentChange> ToDeploymentChanges(TreeEntryChanges change, string deploymentPath)
    {
        var changes = new List<DeploymentChange>();

        if (ignoreService.IsIgnored(change.Path))
        {
            changes.Add(new DeploymentChange(DeploymentAction.Ignore, Source.Git, change.Path, ""));
            return changes;
        }
        
        var deploymentPathWithFile = Path.Combine(deploymentPath, change.Path);
        switch (change.Status)
        {
            case ChangeKind.Added:
                changes.Add(new DeploymentChange(DeploymentAction.Add, Source.Git, change.Path, deploymentPathWithFile));
                break;
            case ChangeKind.Deleted:
                changes.Add(new DeploymentChange(DeploymentAction.Delete, Source.Git, change.Path, deploymentPathWithFile));
                break;
            case ChangeKind.Modified:
                changes.Add(new DeploymentChange(DeploymentAction.Modify, Source.Git, change.Path, deploymentPathWithFile));
                break;
            case ChangeKind.Renamed:
                changes.Add(new DeploymentChange(DeploymentAction.Add, Source.Git, change.Path, deploymentPathWithFile));
                changes.Add(new DeploymentChange(DeploymentAction.Delete, Source.Git, change.OldPath, deploymentPathWithFile));
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