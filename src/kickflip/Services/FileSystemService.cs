using kickflip.Enums;

namespace kickflip.Services;

public class FileSystemService(IgnoreService ignoreService)
{
    /// <summary>
    /// Gets all the files in the specified directory (minus any ignored files) and returns them as a list of deployment changes.
    /// </summary>
    public List<DeploymentChange> GetChanges(string localPath, string deploymentPath)
    {
        var changes = new List<DeploymentChange>();
        
        // Check path is valid and a directory
        if (!Directory.Exists(localPath))
        {
            throw new DirectoryNotFoundException($"The specified path does not exist: {localPath}");
        }
        
        // Get all files in the directory
        var files = Directory.GetFiles(localPath, "*", SearchOption.AllDirectories);
        
        // Filter out ignored files, add the rest to the list of changes
        foreach (string file in files)
        {
            var relativePath = file.Replace(localPath, string.Empty).TrimStart(Path.DirectorySeparatorChar);
            if (ignoreService.IsIgnored(relativePath))
            {
                changes.Add(new DeploymentChange(DeploymentAction.Ignore, Source.Folder, relativePath, ""));
                continue;
            }
            
            var deploymentFilePath = Utilities.UrlCombine(deploymentPath, relativePath);
            changes.Add(new DeploymentChange(DeploymentAction.AddOrModify, Source.Folder, relativePath, deploymentFilePath));
        }

        return changes;
    }
}