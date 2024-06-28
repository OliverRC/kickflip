using System.CommandLine;
using kickflip.Enums;

namespace kickflip
{
    static class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand =
                new RootCommand(
                    "Plan and execute deployment to the remote server based on changes made in git since the last release (tag)");

            rootCommand.AddCommand(DeployCommand());
            rootCommand.AddCommand(GithubCommand());

            return await rootCommand.InvokeAsync(args);
        }

        static Command DeployCommand()
        {
            var pathArgument = new Argument<string?>
            (name: "path",
                description:
                "Path to the folder containing the git repository for Git based deployment modes or the folder to use in folder deployment. The path is an absolute path.",
                getDefaultValue: Directory.GetCurrentDirectory);

            var findModeOption = new Option<FindMode>(
                name: "--mode",
                description:
                "The find mode to use when trying to determine the starting point to compare changes too HEAD with.",
                getDefaultValue: () => FindMode.Tags);

            var deploymentPathOption = new Option<string?>
            (name: "--deployment-path",
                description:
                "Deployment path on the remote server to deploy to. The deployment path is relative to the root of the user account on the remote server.",
                getDefaultValue: () => "/");

            var hostnameOption = new Option<string>(
                name: "--hostname",
                description: "Hostname of the remote server"
            ) {IsRequired = true};

            var portOption = new Option<int>(
                name: "--port",
                description: "Port of the remote server",
                getDefaultValue: () => 22
            );

            var usernameOption = new Option<string>(
                name: "--username",
                description: "Authentication username for the remote server"
            ) {IsRequired = true};

            var passwordOption = new Option<string>(
                name: "--password",
                description: "Authentication password for the remote server"
            ) {IsRequired = true};

            var dryRunOption = new Option<bool>(
                name: "--dry",
                description:
                "Does a dry run of the deployment to see what would be done. No changes will be made to the remote server.",
                getDefaultValue: () => false);

            var deployCommand =
                new Command("deploy", "Kick your git changes and flip them over to that remote server!");

            deployCommand.AddArgument(pathArgument);
            deployCommand.AddOption(findModeOption);
            deployCommand.AddOption(deploymentPathOption);
            deployCommand.AddOption(hostnameOption);
            deployCommand.AddOption(portOption);
            deployCommand.AddOption(usernameOption);
            deployCommand.AddOption(passwordOption);
            deployCommand.AddOption(dryRunOption);

            deployCommand.SetHandler(HandleDeployment!, pathArgument, findModeOption, deploymentPathOption, hostnameOption,
                portOption, usernameOption, passwordOption, dryRunOption);

            return deployCommand;
        }

        static Command GithubCommand()
        {
            var localPathArgument = new Argument<string?>
            (name: "local-path",
                description:
                "Local path to the folder containing the git repository. The local path is an absolute path.",
                getDefaultValue: Directory.GetCurrentDirectory);

            var findModeOption = new Option<FindMode>(
                name: "--mode",
                description:
                "The find mode to use when trying to determine the starting point to compare changes too HEAD with.",
                getDefaultValue: () => FindMode.Tags);

            var deploymentPathOption = new Option<string?>
            (name: "--deployment-path",
                description:
                "Deployment path on the remote server to deploy to. The deployment path is relative to the root of the user account on the remote server.",
                getDefaultValue: () => "/");

            var repositoryOption = new Option<string?>(
                name: "--repo",
                description: "The repository where the PR resides. Should be in the format of <owner>/<repository>.",
                parseArgument: result =>
                {
                    var repository = result.Tokens.Single().ToString();
                    if (repository.Contains("/"))
                    {
                        return repository;
                    }

                    result.ErrorMessage =
                        "Repository should be in the format of <owner>/<repository>. See the GITHUB_REPOSITORY variable in the GitHub Actions documentation. https://docs.github.com/en/actions/learn-github-actions/environment-variables";
                    return null;
                }) {IsRequired = true};

            var refOption = new Option<string?>(
                name: "--ref",
                description:
                "The fully-formed ref of the branch or tag that triggered the workflow run. The ref given is fully-formed for pull requests it is refs/pull/<pr_number>/merge. See the GITHUB_REF variable in the GitHub Actions documentation. https://docs.github.com/en/actions/learn-github-actions/environment-variables",
                parseArgument: result =>
                {
                    var repository = result.Tokens.Single().ToString();
                    if (repository.Contains("refs/pull/") && repository.Contains("/merge"))
                    {
                        return repository;
                    }

                    result.ErrorMessage = "Ref must be fully formed and in the format refs/pull/<pr_number>/merge";
                    return null;
                }) {IsRequired = true};

            var tokenOption = new Option<string>(
                name: "--token",
                description:
                "The Github token or Personal access token to use for authentication. See the GITHUB_TOKEN variable in the GitHub Actions documentation. https://docs.github.com/en/actions/security-guides/automatic-token-authentication"
            ) {IsRequired = true};


            var pullRequestCommand = new Command("pull-request",
                "Adds a comment to the pull request with the changes that will be deployed to the remote server.");
            pullRequestCommand.AddAlias("pr");
            pullRequestCommand.AddArgument(localPathArgument);
            pullRequestCommand.AddOption(findModeOption);
            pullRequestCommand.AddOption(deploymentPathOption);
            pullRequestCommand.AddOption(repositoryOption);
            pullRequestCommand.AddOption(refOption);
            pullRequestCommand.AddOption(tokenOption);
            pullRequestCommand.SetHandler(HandleGithubPullRequest!, localPathArgument, findModeOption, deploymentPathOption, repositoryOption, refOption, tokenOption);

            var githubCommand = new Command("github",
                "Integration with github to allow kickflip to work in your existing Github workflow.");
            githubCommand.AddCommand(pullRequestCommand);

            return githubCommand;
        }

        static Task<int> HandleDeployment(
            string localPath,
            FindMode findMode,
            string deploymentPath,
            string hostname,
            int port,
            string username,
            string password,
            bool isDryRun)
        {
            var ignoreService = new IgnoreService(localPath);
            var gitService = new GitService(ignoreService);
            var fileSystemService = new FileSystemService(ignoreService);
            var outputService = new OutputService();
            var deploymentService = new SftpDeploymentService(hostname, port, username, password, deploymentPath);

            var changes = findMode switch
            {
                FindMode.Tags or FindMode.GitHubMergePR => gitService.GetChanges(localPath, deploymentPath, findMode),
                FindMode.Folder => fileSystemService.GetChanges(localPath, deploymentPath),
                _ => throw new ArgumentOutOfRangeException(nameof(findMode), findMode, null)
            };

            Console.WriteLine(outputService.GetChangesConsole(changes));

            var result = deploymentService.DeployChanges(localPath, changes, isDryRun);
            if (!result)
            {
                Console.WriteLine("Deployment failure. Please check the logs for more information.");
                return Task.FromResult((int) ExitCodes.FailedWithErrors);
            }

            Console.WriteLine("Deployment successful!");
            return Task.FromResult((int) ExitCodes.Success);
        }

        private static async Task<int> HandleGithubPullRequest(string localPath, FindMode findMode, string deploymentPath, string repository, string pullRequestReference,
            string token)
        {
            var ignoreService = new IgnoreService(localPath);
            var gitService = new GitService(ignoreService);
            var fileSystemService = new FileSystemService(ignoreService);
            var gitHubService = new GithubService(token);
            var outputService = new OutputService();

            var changes = findMode switch
            {
                FindMode.Tags or FindMode.GitHubMergePR => gitService.GetChanges(localPath, deploymentPath, findMode),
                FindMode.Folder => fileSystemService.GetChanges(localPath, deploymentPath),
                _ => throw new ArgumentOutOfRangeException(nameof(findMode), findMode, null)
            };
            
            Console.WriteLine(outputService.GetChangesConsole(changes));
            
            var comments = outputService.GetChangesMarkdown(changes);
            var result = await gitHubService.PullRequestCommentChanges(repository, pullRequestReference, comments);

            if (!result)
            {
                Console.WriteLine("Github Pull Request Comment Failure. Please check the logs for more information.");
                return (int) ExitCodes.FailedWithErrors;
            }

            Console.WriteLine("Github Pull Request Comment Successful!");
            return (int) ExitCodes.Success;
        }
    }
}