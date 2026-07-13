using LibGit2Sharp;

namespace kickflip.Tests.TestHelpers;

/// <summary>
/// Builds a real (on-disk) git repository so the git based find modes can be
/// exercised end-to-end without mocking LibGit2Sharp.
/// </summary>
public sealed class GitRepositoryBuilder : IDisposable
{
    private readonly TempDirectory _tempDirectory = new();
    private readonly Signature _signature = new("Test", "test@example.com", DateTimeOffset.Now);

    public string Path => _tempDirectory.Path;

    public GitRepositoryBuilder()
    {
        Repository.Init(Path);
    }

    public GitRepositoryBuilder WriteFile(string relativePath, string contents = "content")
    {
        _tempDirectory.WriteFile(relativePath, contents);
        return this;
    }

    public GitRepositoryBuilder DeleteFile(string relativePath)
    {
        _tempDirectory.DeleteFile(relativePath);
        return this;
    }

    public Commit Commit(string message)
    {
        using var repo = new Repository(Path);
        Commands.Stage(repo, "*");
        return repo.Commit(message, _signature, _signature, new CommitOptions { AllowEmptyCommit = true });
    }

    public void Tag(string name)
    {
        using var repo = new Repository(Path);
        repo.ApplyTag(name);
    }

    public void Dispose()
    {
        _tempDirectory.Dispose();
    }
}
