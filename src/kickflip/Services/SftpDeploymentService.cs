using Renci.SshNet;
using Renci.SshNet.Common;

namespace kickflip.Services;

public class SftpDeploymentService
{
    private readonly string _deploymentPath;
    private readonly SftpClient _client;

    public SftpDeploymentService(string host, int port, string username, string password, string deploymentPath)
    {
        _deploymentPath = deploymentPath;
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
            case DeploymentAction.Ignore:
                Console.WriteLine($"Change ignored: {change.Path}");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(change.Action), change.Action, "Unknown or unsupported deployment action");
        }
    }
    
    private void Upload(string path, DeploymentChange change, bool isDryRun)
    {
        var localPath = Path.Combine(path, change.Path);
        var remotePath = Utilities.Utilities.UrlCombine(_deploymentPath, change.Path);

        var uploadOutput = $"\"{localPath}\" to \"{remotePath}\" @ \"{_client.ConnectionInfo.Host}\"";
        
        if (isDryRun)
        {
            Console.WriteLine($"DRY RUN: Would upload {uploadOutput}");
            return;
        }

        try
        {
            var fileInfo = new FileInfo(localPath);
            using var fileStream = fileInfo.OpenRead();
            
            var directoryPath = remotePath.Replace(fileInfo.Name, "");
         
            CreateDirectoriesRecursively(directoryPath);
            _client.UploadFile(fileStream, remotePath);
            
            Console.WriteLine($"Uploaded {uploadOutput}");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error uploading {uploadOutput}: {exception.Message}");
        }
    }

    // TODO:
    // Can probably optimize this by rather going back from the deepest directory back until we find the first existing directory.
    // Then recurse down the from there creating new directories.
    private void CreateDirectoriesRecursively(string path)
    {
        var current = "";

        if (path[0] == '/')
        {
            path = path.Substring(1);
        }

        while (!string.IsNullOrEmpty(path))
        {
            int p = path.IndexOf('/');
            current += '/';
            if (p >= 0)
            {
                current += path.Substring(0, p);
                path = path.Substring(p + 1);
            }
            else
            {
                current += path;
                path = "";
            }

            try
            {
                var attrs = _client.GetAttributes(current);
                if (!attrs.IsDirectory)
                {
                    throw new Exception("Not a directory");
                }
            }
            catch (SftpPathNotFoundException)
            {
                Console.WriteLine($"Directory {current} did not exist so creating it");
                _client.CreateDirectory(current);
                Console.WriteLine($"Directory {current} created");
            }
        }
    }
    
    private void Delete(DeploymentChange change, bool isDryRun)
    {
        var remotePath = Utilities.Utilities.UrlCombine(_deploymentPath, change.Path);
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