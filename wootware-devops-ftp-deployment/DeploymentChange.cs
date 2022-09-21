namespace wootware_devops_ftp_deployment;

public class DeploymentChange
{
    public DeploymentAction Action { get; set; }
    public string Path { get; set; }
}