namespace wootware_devops_ftp_deployment.Models;

public class DeploymentChange
{
    public DeploymentChange(DeploymentAction Action, string Path)
    {
        this.Action = Action;
        this.Path = Path;
    }

    public DeploymentAction Action { get; }
    public string Path { get; }
}