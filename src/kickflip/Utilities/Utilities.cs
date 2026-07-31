namespace kickflip;

public static class Utilities
{
    public static string UrlCombine(string url1, string url2)
    {
        if (url1.Length == 0)
        {
            return url2;
        }

        if (url2.Length == 0)
        {
            return url1;
        }

        url1 = url1.TrimEnd('/', '\\');
        url2 = url2.TrimStart('/', '\\');

        url1 = url1.Replace(Path.DirectorySeparatorChar, '/');
        url2 = url2.Replace(Path.DirectorySeparatorChar, '/');

        return $"{url1}/{url2}";
    }

    /// <summary>
    /// Deployment order: new files first, changed files next, deletes last.
    /// Uploading before deleting means an interrupted run leaves extra files behind rather than missing ones.
    /// Sorts by DeploymentAction declaration order; the sort is stable so files keep their relative order within each group.
    /// </summary>
    public static List<DeploymentChange> OrderForDeployment(this List<DeploymentChange> changes)
    {
        return changes.OrderBy(change => change.Action).ToList();
    }
}