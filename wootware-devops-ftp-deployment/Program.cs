// See https://aka.ms/new-console-template for more information

// Will need to take in
// SFTP: hostname, port, username and password
// --dry to allow for a dry run
// --output to allow for Markdown output for Github summary
// Future: --filter to allow tag filtering e.g "release-" tags only to be considered.
// Future: add comment to a PR in Github with summary of what's action will be taken

var isDryRun = false;
var path = "C:\\Dev\\Playpen\\git-changed-files-experiment";

var gitService = new GitService();
var deploymentService = new SftpDeploymentService("test", 22, "test", "test");
var markdownService = new MarkdownService(deploymentService);

var changes = gitService.GetChanges(path);

Console.WriteLine(markdownService.GetChangesOutput(changes));

deploymentService.DeployChanges(path, changes, isDryRun);

