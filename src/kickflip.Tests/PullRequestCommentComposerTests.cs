using kickflip.Services;

namespace kickflip.Tests;

public class PullRequestCommentComposerTests
{
    [Fact]
    public void Compose_WithNoExistingComment_CreatesMarkedComment()
    {
        var body = PullRequestCommentComposer.Compose(null, "deploy", "content-a");

        Assert.Contains(PullRequestCommentComposer.CommentMarker, body);
        Assert.Contains("### 🛹 Kickflip", body);
        Assert.Contains("content-a", body);
        Assert.True(PullRequestCommentComposer.IsKickflipComment(body));
    }

    [Fact]
    public void IsKickflipComment_ReturnsFalse_ForNonKickflipComment()
    {
        Assert.False(PullRequestCommentComposer.IsKickflipComment("just a normal comment"));
        Assert.False(PullRequestCommentComposer.IsKickflipComment(null));
    }

    [Fact]
    public void Compose_SameAction_ReplacesSectionContent()
    {
        var first = PullRequestCommentComposer.Compose(null, "deploy", "old-content");
        var second = PullRequestCommentComposer.Compose(first, "deploy", "new-content");

        Assert.Contains("new-content", second);
        Assert.DoesNotContain("old-content", second);
    }

    [Fact]
    public void Compose_DifferentActions_KeepsBothSections()
    {
        var first = PullRequestCommentComposer.Compose(null, "staging", "staging-content");
        var second = PullRequestCommentComposer.Compose(first, "production", "production-content");

        Assert.Contains("staging-content", second);
        Assert.Contains("production-content", second);
        Assert.Contains("#### staging", second);
        Assert.Contains("#### production", second);
        // Only one header / marker for the shared comment
        Assert.Equal(1, CountOccurrences(second, PullRequestCommentComposer.CommentMarker));
        Assert.Equal(1, CountOccurrences(second, "### 🛹 Kickflip"));
    }

    [Fact]
    public void Compose_UpdatingOneAction_LeavesOtherActionUntouched()
    {
        var first = PullRequestCommentComposer.Compose(null, "staging", "staging-content");
        var second = PullRequestCommentComposer.Compose(first, "production", "production-v1");
        var third = PullRequestCommentComposer.Compose(second, "production", "production-v2");

        Assert.Contains("staging-content", third);
        Assert.Contains("production-v2", third);
        Assert.DoesNotContain("production-v1", third);
    }

    [Fact]
    public void Compose_NullOrEmptyActionName_UsesDefault()
    {
        var body = PullRequestCommentComposer.Compose(null, null, "content");
        Assert.Contains("#### default", body);

        var reused = PullRequestCommentComposer.Compose(body, "", "content-2");
        Assert.Contains("content-2", reused);
        Assert.DoesNotContain("content", reused.Replace("content-2", ""));
        Assert.Equal(1, CountOccurrences(reused, "#### default"));
    }

    [Fact]
    public void Compose_IsIdempotentAcrossMultipleReuses()
    {
        var body = PullRequestCommentComposer.Compose(null, "deploy", "content");
        for (var i = 0; i < 3; i++)
        {
            body = PullRequestCommentComposer.Compose(body, "deploy", "content");
        }

        Assert.Equal(1, CountOccurrences(body, "#### deploy"));
        Assert.Equal(1, CountOccurrences(body, PullRequestCommentComposer.CommentMarker));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
