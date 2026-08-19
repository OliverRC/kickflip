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

    public Commit Commit(string message) => Commit(message, DateTimeOffset.Now);

    /// <summary>Commit with an explicit timestamp (to exercise date-ordered history walks).</summary>
    public Commit Commit(string message, DateTimeOffset when)
    {
        using var repo = new Repository(Path);
        Commands.Stage(repo, "*");
        var signature = new Signature(_signature.Name, _signature.Email, when);
        return repo.Commit(message, signature, signature, new CommitOptions { AllowEmptyCommit = true });
    }

    public void Tag(string name)
    {
        using var repo = new Repository(Path);
        repo.ApplyTag(name);
    }

    /// <summary>Create <paramref name="name"/> at HEAD and check it out.</summary>
    public GitRepositoryBuilder Branch(string name)
    {
        using var repo = new Repository(Path);
        Commands.Checkout(repo, repo.CreateBranch(name));
        return this;
    }

    public GitRepositoryBuilder Checkout(string name)
    {
        using var repo = new Repository(Path);
        Commands.Checkout(repo, repo.Branches[name]);
        return this;
    }

    /// <summary>
    /// Merge <paramref name="name"/> into the checked-out branch (always a merge
    /// commit), optionally with a GitHub-style message such as
    /// "Merge pull request #1 from owner/branch".
    /// </summary>
    public GitRepositoryBuilder Merge(string name, string? message = null)
    {
        using var repo = new Repository(Path);
        repo.Merge(repo.Branches[name], _signature, new MergeOptions { FastForwardStrategy = FastForwardStrategy.NoFastForward });
        if (message != null)
        {
            repo.Commit(message, _signature, _signature, new CommitOptions { AmendPreviousCommit = true });
        }
        return this;
    }

    public void Dispose()
    {
        _tempDirectory.Dispose();
    }
}
