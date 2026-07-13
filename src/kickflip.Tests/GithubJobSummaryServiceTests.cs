using kickflip.Services;

namespace kickflip.Tests;

public class GithubJobSummaryServiceTests
{
    [Fact]
    public void IsAvailable_ReturnsFalse_WhenPathIsNullOrEmpty()
    {
        Assert.False(new GithubJobSummaryService(null).IsAvailable);
        Assert.False(new GithubJobSummaryService("").IsAvailable);
        Assert.False(new GithubJobSummaryService("   ").IsAvailable);
    }

    [Fact]
    public void IsAvailable_ReturnsTrue_WhenPathProvided()
    {
        Assert.True(new GithubJobSummaryService("/tmp/summary.md").IsAvailable);
    }

    [Fact]
    public async Task AppendSummaryAsync_WritesContentToFile()
    {
        var path = Path.GetTempFileName();
        try
        {
            var service = new GithubJobSummaryService(path);
            var result = await service.AppendSummaryAsync("## Hello\ncontent");

            Assert.True(result);
            var written = await File.ReadAllTextAsync(path);
            Assert.Contains("## Hello", written);
            Assert.Contains("content", written);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task AppendSummaryAsync_AppendsToExistingContent()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "existing\n");

            var service = new GithubJobSummaryService(path);
            await service.AppendSummaryAsync("added");

            var written = await File.ReadAllTextAsync(path);
            Assert.Contains("existing", written);
            Assert.Contains("added", written);
            Assert.True(written.IndexOf("existing", StringComparison.Ordinal) <
                        written.IndexOf("added", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task AppendSummaryAsync_ReturnsFalse_WhenNotAvailable()
    {
        var service = new GithubJobSummaryService(null);
        var result = await service.AppendSummaryAsync("content");
        Assert.False(result);
    }
}
