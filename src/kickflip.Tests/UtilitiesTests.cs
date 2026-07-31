using kickflip.Enums;
using kickflip.Models;

namespace kickflip.Tests;

public class UtilitiesTests
{
    [Fact]
    public void OrderForDeployment_AddsFirst_ThenModifies_ThenDeletes()
    {
        var changes = new List<DeploymentChange>
        {
            new(DeploymentAction.Delete, Source.Git, "old.php", "/old.php"),
            new(DeploymentAction.Modify, Source.Git, "changed.php", "/changed.php"),
            new(DeploymentAction.Ignore, Source.Git, "ignored.php", ""),
            new(DeploymentAction.Add, Source.Git, "new-b.php", "/new-b.php"),
            new(DeploymentAction.AddOrModify, Source.Folder, "synced.php", "/synced.php"),
            new(DeploymentAction.Add, Source.Git, "new-a.php", "/new-a.php"),
        };

        var ordered = changes.OrderForDeployment();

        Assert.Equal(new[]
        {
            DeploymentAction.Add,
            DeploymentAction.Add,
            DeploymentAction.Modify,
            DeploymentAction.AddOrModify,
            DeploymentAction.Delete,
            DeploymentAction.Ignore,
        }, ordered.Select(c => c.Action));

        // Stable within a group: the two adds keep their original relative order
        Assert.Equal(new[] { "new-b.php", "new-a.php" }, ordered.Where(c => c.Action == DeploymentAction.Add).Select(c => c.Path));
    }

    [Theory]
    [InlineData("/public_html", "index.html", "/public_html/index.html")]
    [InlineData("/public_html/", "/index.html", "/public_html/index.html")]
    [InlineData("/", "index.html", "/index.html")]
    [InlineData("", "index.html", "index.html")]
    [InlineData("/public_html", "", "/public_html")]
    public void UrlCombine_CombinesPathsWithSingleSeparator(string url1, string url2, string expected)
    {
        var result = kickflip.Utilities.UrlCombine(url1, url2);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void UrlCombine_NormalisesDirectorySeparatorsToForwardSlashes()
    {
        var nested = "sub" + Path.DirectorySeparatorChar + "file.txt";

        var result = kickflip.Utilities.UrlCombine("/public_html", nested);

        Assert.Equal("/public_html/sub/file.txt", result);
    }
}
