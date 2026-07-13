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

    public async Task<bool> PullRequestCommentChanges(string repository, string pullRequestReference, string sectionContent, string? actionName)
    {
        var repositoryParts = repository.Split("/");
        if (repositoryParts.Length != 2)
        {
            throw new ArgumentException("Repository must be in the format <owner>/<repository>");
        }

        var owner = repositoryParts[0];
        var name = repositoryParts[1];
        var pullRequestNumber = int.Parse(pullRequestReference.Replace("refs/pull/", "").Replace("/merge", ""));

        var existingComments = await _githubClient.Issue.Comment.GetAllForIssue(owner, name, pullRequestNumber);
        var existingComment = existingComments.FirstOrDefault(comment => PullRequestCommentComposer.IsKickflipComment(comment.Body));

        var body = PullRequestCommentComposer.Compose(existingComment?.Body, actionName, sectionContent);

        if (existingComment == null)
        {
            var issueComment = await _githubClient.Issue.Comment.Create(owner, name, pullRequestNumber, body);
            if (issueComment == null)
            {
                Console.WriteLine($"Unable to add comment to pull request {pullRequestReference} in repository {repository}");
                return false;
            }

            Console.WriteLine($"Comment added to pull request #{pullRequestNumber} in repository {repository}");
        }
        else
        {
            var issueComment = await _githubClient.Issue.Comment.Update(owner, name, existingComment.Id, body);
            if (issueComment == null)
            {
                Console.WriteLine($"Unable to update comment on pull request {pullRequestReference} in repository {repository}");
                return false;
            }

            Console.WriteLine($"Comment updated on pull request #{pullRequestNumber} in repository {repository}");
        }

        return true;
    }
}
