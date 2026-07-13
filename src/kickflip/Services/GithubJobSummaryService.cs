namespace kickflip.Services;

/// <summary>
/// Writes deployment change summaries to the GitHub Actions job summary.
/// GitHub Actions exposes the summary as a file via the GITHUB_STEP_SUMMARY
/// environment variable. Anything appended to that file (as markdown) is
/// rendered on the workflow run's summary page.
/// See https://github.blog/2022-05-09-supercharging-github-actions-with-job-summaries/
/// </summary>
public class GithubJobSummaryService
{
    private readonly string? _summaryFilePath;

    public GithubJobSummaryService(string? summaryFilePath)
    {
        _summaryFilePath = summaryFilePath;
    }

    /// <summary>
    /// Whether a job summary file is available to write to. This is only the
    /// case when running inside GitHub Actions.
    /// </summary>
    public bool IsAvailable => !string.IsNullOrWhiteSpace(_summaryFilePath);

    /// <summary>
    /// Appends the given markdown content to the GitHub Actions job summary.
    /// Returns false when no job summary is available (i.e. not running in
    /// GitHub Actions).
    /// </summary>
    public async Task<bool> AppendSummaryAsync(string content)
    {
        if (!IsAvailable)
        {
            return false;
        }

        await File.AppendAllTextAsync(_summaryFilePath!, content + Environment.NewLine);
        return true;
    }
}
