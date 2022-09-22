// See https://aka.ms/new-console-template for more information

// Will need to take in
// SFTP: hostname, port, username and password
// --dry to allow for a dry run
// --output to allow for Markdown output for Github summary
// Future: --filter to allow tag filtering e.g "release-" tags only to be considered.
// Future: add comment to a PR in Github with summary of what's action will be taken

using System.CommandLine;

namespace wootware_devops_ftp_deployment
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
       
            var rootCommand = new RootCommand("Plan and execute deployment to the remote server based on changes made in git since the last release (tag)");
            
            rootCommand.AddCommand(DeployCommand());

            return await rootCommand.InvokeAsync(args);
        }

        static Command DeployCommand()
        {
            var localPathArgument = new Argument<string?>
            (name: "path",
                description: "Path to the folder containing the git repository", parse: result =>
                {
                    var path = result.Tokens.Single().Value;
                    if (Directory.Exists(path)) { return path; }
                    
                    result.ErrorMessage = "Path provided does not exist";
                    return null;
                });

            var hostnameOption = new Option<string>(
                name: "--hostname",
                description: "Hostname of the remote server"
            ) { IsRequired = true };
            
            var portOption = new Option<int>(
                name: "--port",
                description: "Port of the remote server",
                getDefaultValue: () => 22
            );
            
            var usernameOption = new Option<string>(
                name: "--username",
                description: "Authentication username for the remote server"
            ) { IsRequired = true };
            
            var passwordOption = new Option<string>(
                name: "--password",
                description: "Authentication password for the remote server"
            ) { IsRequired = true };
            
            var dryRunOption = new Option<bool>(
                name: "--dry",
                description: "Does a dry run of the deployment to see what would be done. No changes will be made to the remote server.",
                getDefaultValue: () => false);
            
            var deployCommand = new Command("deploy", "Deploy the latest changes (git) to the remote server");
            deployCommand.AddArgument(localPathArgument);
            deployCommand.AddOption(hostnameOption);
            deployCommand.AddOption(portOption);
            deployCommand.AddOption(usernameOption);
            deployCommand.AddOption(passwordOption);
            deployCommand.AddOption(dryRunOption);

            deployCommand.SetHandler(HandleDeployment!, localPathArgument, hostnameOption, portOption, usernameOption, passwordOption, dryRunOption);

            return deployCommand;
        }
        
        
        static void HandleDeployment(string localPath, string hostname, int port, string username, string password, bool isDryRun)
        {
            var gitService = new GitService();
            var deploymentService = new SftpDeploymentService(hostname, port, username, password);
            var markdownService = new MarkdownService(deploymentService);
            
            var changes = gitService.GetChanges(localPath);
            
            Console.WriteLine(markdownService.GetChangesOutput(changes));
            
            deploymentService.DeployChanges(localPath, changes, isDryRun);
        }
    }
}



