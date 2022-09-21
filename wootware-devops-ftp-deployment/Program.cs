// See https://aka.ms/new-console-template for more information

// Will need to take in
// SFTP: hostname, port, username and password
// --dry to allow for a dry run
// --output to allow for Markdown output for Github summary
// Future: --filter to allow tag filtering e.g "release-" tags only to be considered.
// Future: add comment to a PR in Github with summary of what's action will be taken

var gitService = new GitService();

var changes = gitService.GetChanges();

var t = 1;
