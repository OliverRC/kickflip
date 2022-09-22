namespace wootware_devops_ftp_deployment.Models;

public class DeploymentChange
{
    public DeploymentAction Action { get; set; }
    public string Path { get; set; }
}