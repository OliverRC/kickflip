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
            DeploymentAction.Delete => "❌ Delete",
            DeploymentAction.Ignore => "🚫 None",
            _ => throw new ArgumentOutOfRangeException(nameof(change.Action), change.Action, "Unknown or unsupported deployment action")
        };
    }
    
    public string GetChangesMarkdown(List<DeploymentChange> changes)
    {
        var builder = new StringBuilder();

        builder.AppendLine("### 🛹 Kickflip");
        builder.AppendLine();
        
        builder.AppendLine("The following deployment changes are going to be applied");
        builder.AppendLine();
        
        var table = new ConsoleTable("Change", "Action", "File");
        foreach (var change in changes)
        {
            table.AddRow(change.Action, GetAction(change), change.Path);
        }
        
        builder.AppendLine(table.ToMarkDownString());

        return builder.ToString();
    }
    
    public string GetChangesConsole(List<DeploymentChange> changes)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Deployment Changes");
        builder.AppendLine();
        
        builder.AppendLine("The following deployment changes are going to be applied");
        builder.AppendLine();
        
        var table = new ConsoleTable("Change", "Action", "File");
        foreach (var change in changes)
        {
            table.AddRow(change.Action, GetAction(change), change.Path);
        }
        
        builder.AppendLine(table.ToString());
        
        return builder.ToString();
    }
}