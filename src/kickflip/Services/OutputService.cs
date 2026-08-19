using ConsoleTables;

namespace kickflip.Services;

public class OutputService
{
    private string GetAction(DeploymentChange change)
    {
        return change.Action switch
        {
            DeploymentAction.Add => "⬆ Upload",
            DeploymentAction.Modify => "⬆ Upload",
            DeploymentAction.AddOrModify => "⬆ Upload",
            DeploymentAction.Delete => "❌ Delete",
            DeploymentAction.Ignore => "🚫 None",
            _ => throw new ArgumentOutOfRangeException(nameof(change.Action), change.Action, "Unknown or unsupported deployment action")
        };
    }

    /// <summary>
    /// "N files changed · M to deploy · K ignored". Files changed = distinct paths
    /// in the diff (matches GitHub's PR count except for renames, which GitHub
    /// shows as one file and we count as two: the new path that is uploaded and
    /// the old path that is deleted); to-deploy = actions that will hit the
    /// server; ignored = .kickflipignore matches.
    /// </summary>
    public static string GetChangesSummary(List<DeploymentChange> changes)
    {
        var ignored = changes.Count(c => c.Action == DeploymentAction.Ignore);
        var toDeploy = changes.Count - ignored;
        var filesChanged = changes.Select(c => c.Path).Distinct().Count();
        return $"**{filesChanged}** file{(filesChanged == 1 ? "" : "s")} changed · **{toDeploy}** to deploy · **{ignored}** ignored";
    }

    public string GetChangesMarkdown(List<DeploymentChange> changes)
    {
        var builder = new StringBuilder();

        builder.AppendLine(GetChangesSummary(changes));
        builder.AppendLine();
        builder.AppendLine("The following deployment changes are going to be applied");
        builder.AppendLine();

        var table = new ConsoleTable("Change", "Action", "Source", "File", "Deployment Path");
        foreach (var change in changes)
        {
            table.AddRow(change.Action, GetAction(change), change.Source, change.Path, change.DeploymentPath);
        }

        builder.AppendLine(table.ToMarkDownString());

        return builder.ToString();
    }
    
    public string GetChangesConsole(List<DeploymentChange> changes)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Deployment Changes");
        builder.AppendLine();
        builder.AppendLine(GetChangesSummary(changes).Replace("**", ""));
        builder.AppendLine();
        builder.AppendLine("The following deployment changes are going to be applied");
        builder.AppendLine();
        
        var table = new ConsoleTable("Change", "Action", "Source", "File", "Deployment Path");
        foreach (var change in changes)
        {
            table.AddRow(change.Action, GetAction(change), change.Source, change.Path, change.DeploymentPath);
        }
        
        builder.AppendLine(table.ToString());
        
        return builder.ToString();
    }
}