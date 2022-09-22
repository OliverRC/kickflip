using Renci.SshNet;

namespace wootware_devops_ftp_deployment.Services;

public class SftpDeploymentService
{
    private readonly string _remotePath;
    private readonly SftpClient _client;

    public SftpDeploymentService(string host, int port, string username, string password, string remotePath = "/")
    {
        _remotePath = remotePath;
        _client = new SftpClient(host, port, username, password);
    }

    private void Connect()
    {
        if (_client.IsConnected)
        {
            return;
        }
        
        Console.WriteLine($"Connecting to remote server {_client.ConnectionInfo.Host}:{_client.ConnectionInfo.Port} with user {_client.ConnectionInfo.Username}");
        _client.Connect();
        Console.WriteLine("Connected!");
    }

    public string GetAction(DeploymentChange change)
    {
        return change.Action switch
        {
            DeploymentAction.Add => "⬆ Upload",
            DeploymentAction.Modify => "⬆ Upload",
            DeploymentAction.Delete => "❌ Delete",
            _ => throw new ArgumentOutOfRangeException(nameof(change.Action), change.Action, "Unknown or unsupported deployment action")
        };
    }
    
    public void DeployChanges(string path, List<DeploymentChange> changes, bool isDryRun)
    {
        if(isDryRun)
        {
            Console.WriteLine("Dry run, no changes will be made");
            Console.WriteLine();
        }
        else
        {
            Connect();
        }

        foreach (var change in changes)
        {
            Deploy(path, change, isDryRun);
        }
        
    }

    private void Deploy(string path, DeploymentChange change, bool isDryRun)
    {
        switch (change.Action)
        {
            case DeploymentAction.Add:
            case DeploymentAction.Modify:
                Upload(path, change, isDryRun);
                break;
            case DeploymentAction.Delete:
                Delete(change, isDryRun);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(change.Action), change.Action, "Unknown or unsupported deployment action");
        }
    }
    
    private void Upload(string path, DeploymentChange change, bool isDryRun)
    {
        var localPath = Path.Combine(path, change.Path);
        var remotePath = Utilities.Utilities.UrlCombine(_remotePath, change.Path);

        var uploadOutput = $"\"{localPath}\" to \"{remotePath}\" @ \"{_client.ConnectionInfo.Host}\"";
        
        if (isDryRun)
        {
            Console.WriteLine($"DRY RUN: Would upload {uploadOutput}");
            return;
        }

        try
        {
            using var fileStream = File.OpenRead(localPath);
            _client.UploadFile(fileStream, remotePath);
            Console.WriteLine($"Uploaded {uploadOutput}");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error uploading {uploadOutput}: {exception.Message}");
        }
    }
    
    private void Delete(DeploymentChange change, bool isDryRun)
    {
        
        var remotePath = Utilities.Utilities.UrlCombine(_remotePath, change.Path);
        var deleteOutput = $"\"{remotePath}\" @ \"{_client.ConnectionInfo.Host}\"";
        
        if (isDryRun)
        {
            Console.WriteLine($"DRY RUN: Would delete {deleteOutput}"); 
            return;
        }

        try
        {
            _client.Delete(remotePath);
            Console.WriteLine($"Deleted {deleteOutput}");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error deleting {deleteOutput}: {exception.Message}");
        }
    }
}