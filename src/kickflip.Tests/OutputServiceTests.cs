using kickflip.Enums;
using kickflip.Models;
using kickflip.Services;

namespace kickflip.Tests;

public class OutputServiceTests
{
    private static List<DeploymentChange> SampleChanges() =>
    [
        new(DeploymentAction.Add, Source.Git, "added.txt", "/added.txt"),
        new(DeploymentAction.Delete, Source.Git, "gone.txt", "/gone.txt"),
        new(DeploymentAction.Ignore, Source.Git, "app.log", ""),
    ];

    [Fact]
    public void GetChangesMarkdown_RendersMarkdownTableWithEveryFile()
    {
        var output = new OutputService().GetChangesMarkdown(SampleChanges());

        Assert.Contains("|", output);
        Assert.Contains("added.txt", output);
        Assert.Contains("gone.txt", output);
        Assert.Contains("app.log", output);
        Assert.Contains("Deployment", output);
    }

    [Fact]
    public void GetChangesConsole_RendersConsoleTableWithHeading()
    {
        var output = new OutputService().GetChangesConsole(SampleChanges());

        Assert.Contains("Deployment Changes", output);
        Assert.Contains("added.txt", output);
        Assert.Contains("gone.txt", output);
    }

    [Fact]
    public void GetChanges_WithEmptyList_StillRendersHeaders()
    {
        var service = new OutputService();

        Assert.Contains("Deployment Changes", service.GetChangesConsole([]));
        Assert.Contains("Change", service.GetChangesMarkdown([]));
    }

    [Theory]
    [InlineData(DeploymentAction.Add, "Upload")]
    [InlineData(DeploymentAction.Modify, "Upload")]
    [InlineData(DeploymentAction.AddOrModify, "Upload")]
    [InlineData(DeploymentAction.Delete, "Delete")]
    [InlineData(DeploymentAction.Ignore, "None")]
    public void GetChangesConsole_MapsActionsToFriendlyLabels(DeploymentAction action, string expectedLabel)
    {
        var changes = new List<DeploymentChange>
        {
            new(action, Source.Git, "file.txt", "/file.txt"),
        };

        var output = new OutputService().GetChangesConsole(changes);

        Assert.Contains(expectedLabel, output);
    }
}
