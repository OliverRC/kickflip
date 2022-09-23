using ConsoleTables;

namespace kickflip.Services;

public class OutputService
{
    private readonly SftpDeploymentService _deploymentService;

    public OutputService(SftpDeploymentService deploymentService)
    {
        _deploymentService = deploymentService;
    }
    
    public string GetChangesMarkdown(List<DeploymentChange> changes)
    {
        var builder = new StringBuilder();

        builder.AppendLine("## Deployment Changes");
        builder.AppendLine();
        
        builder.AppendLine("The following deployment changes are going to be applied");
        builder.AppendLine();
        
        builder.AppendLine("| Change | Action | File |");
        builder.AppendLine("|--------|--------|------|");

        foreach (var change in changes)
        {
            builder.AppendLine($"| {change.Action} | {_deploymentService.GetAction(change)} | {change.Path} |");
        }
        
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
            table.AddRow(change.Action, _deploymentService.GetAction(change), change.Path);
        }
        
        builder.AppendLine(table.ToString());
        
        return builder.ToString();
    }
}