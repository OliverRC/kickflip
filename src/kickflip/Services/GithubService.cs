using Octokit;

namespace kickflip.Services;

public class GithubService
{
    private readonly GitHubClient _githubClient;

    public GithubService(string token)
    {
        _githubClient = new GitHubClient(new ProductHeaderValue("kickflip"))
        {
            Credentials = new Credentials(token)
        };
    }

    public async Task PullRequestCommentChanges(string repository, string pullRequestReference, string comment)
    {
        var repositoryParts = repository.Split("/");
        if (repositoryParts.Length != 2)
        {
            throw new ArgumentException("Repository must be in the format <owner>/<repository>");
        }

        var pullRequestNumber = int.Parse(pullRequestReference.Replace("refs/pull/", "").Replace("/merge", ""));

        var t = await _githubClient.Issue.Comment.Create(repositoryParts[0], repositoryParts[1], pullRequestNumber, comment);
    }
}