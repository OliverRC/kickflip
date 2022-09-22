using System.Text;

namespace wootware_devops_ftp_deployment;

public class MarkdownService
{
    private readonly SftpDeploymentService _deploymentService;

    public MarkdownService(SftpDeploymentService deploymentService)
    {
        _deploymentService = deploymentService;
    }
    
    public string GetChangesOutput(List<DeploymentChange> changes)
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
}