namespace kickflip.Models;

public class DeploymentChange
{
    public DeploymentChange(DeploymentAction action, string path)
    {
        Action = action;
        Path = path;
    }

    public DeploymentAction Action { get; }
    public string Path { get; }
}