namespace kickflip.Enums;

public enum FindMode
{
    Tags,
    GitHubMergePR,
    /// <summary>
    /// Diff HEAD against its merge-base with a base branch (--base-ref, defaults to
    /// GITHUB_BASE_REF) - i.e. exactly the changes a pull request introduces.
    /// </summary>
    MergeBase,
    Folder
}