using System.Text.RegularExpressions;

namespace kickflip.Services;

/// <summary>
/// Composes the body of the single, reusable kickflip pull request comment.
/// A kickflip comment is identified by a hidden marker so it can be found and
/// updated on subsequent runs. When multiple kickflip flows run in the same
/// workflow, each flow owns a section within the comment identified by its
/// action name, so the flows share a single comment but keep their output separate.
/// </summary>
public static class PullRequestCommentComposer
{
    public const string CommentMarker = "<!-- kickflip-comment -->";

    private const string Header = "### 🛹 Kickflip";
    private const string DefaultActionName = "default";

    private static readonly Regex SectionRegex = new(
        @"<!-- kickflip-section:(?<name>.*?) -->(?<content>.*?)<!-- /kickflip-section:\k<name> -->",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Determines whether the given comment body belongs to kickflip.
    /// </summary>
    public static bool IsKickflipComment(string? body)
    {
        return body != null && body.Contains(CommentMarker, StringComparison.Ordinal);
    }

    /// <summary>
    /// Resolves the section name that a kickflip flow owns within the shared
    /// comment. When an explicit <paramref name="actionName"/> is provided it is
    /// used verbatim. Otherwise the <paramref name="deploymentPath"/> is used so
    /// that multiple flows in the same workflow (for example deploying to
    /// different paths) keep their own section instead of all colliding on the
    /// default name and overwriting each other.
    /// </summary>
    public static string ResolveSectionName(string? actionName, string? deploymentPath)
    {
        if (!string.IsNullOrWhiteSpace(actionName) && actionName.Trim() != DefaultActionName)
        {
            return actionName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(deploymentPath))
        {
            var normalized = deploymentPath.Trim();
            if (normalized != "/")
            {
                return normalized;
            }
        }

        return DefaultActionName;
    }

    /// <summary>
    /// Produces the full comment body for a kickflip comment, upserting the
    /// section owned by <paramref name="actionName"/> with <paramref name="sectionContent"/>.
    /// Existing sections owned by other actions are preserved.
    /// </summary>
    /// <param name="existingBody">The existing kickflip comment body, or null if none exists yet.</param>
    /// <param name="actionName">The action name that owns this section. When null or empty a default is used.</param>
    /// <param name="sectionContent">The markdown content for this action's section.</param>
    public static string Compose(string? existingBody, string? actionName, string sectionContent)
    {
        var name = string.IsNullOrWhiteSpace(actionName) ? DefaultActionName : actionName.Trim();

        var sections = ParseSections(existingBody);

        // Upsert the section for this action, preserving order of existing sections.
        var index = sections.FindIndex(s => s.Name == name);
        var section = new Section(name, sectionContent.TrimEnd());
        if (index >= 0)
        {
            sections[index] = section;
        }
        else
        {
            sections.Add(section);
        }

        var builder = new StringBuilder();
        builder.AppendLine(CommentMarker);
        builder.AppendLine(Header);
        builder.AppendLine();

        foreach (var s in sections)
        {
            builder.AppendLine(SectionStart(s.Name));
            builder.AppendLine();
            builder.AppendLine($"#### {s.Name}");
            builder.AppendLine();
            builder.AppendLine(s.Content);
            builder.AppendLine();
            builder.AppendLine(SectionEnd(s.Name));
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd() + "\n";
    }

    private static string SectionStart(string name) => $"<!-- kickflip-section:{name} -->";

    private static string SectionEnd(string name) => $"<!-- /kickflip-section:{name} -->";

    private static List<Section> ParseSections(string? body)
    {
        var sections = new List<Section>();
        if (string.IsNullOrEmpty(body))
        {
            return sections;
        }

        foreach (Match match in SectionRegex.Matches(body))
        {
            var name = match.Groups["name"].Value.Trim();
            var content = StripSectionHeading(match.Groups["content"].Value, name).Trim();
            sections.Add(new Section(name, content));
        }

        return sections;
    }

    private static string StripSectionHeading(string content, string name)
    {
        // Remove the visible heading we add for each section so re-parsing is idempotent.
        var heading = $"#### {name}";

        var lines = content.Replace("\r\n", "\n").Split('\n').ToList();
        lines.RemoveAll(line => string.Equals(line.Trim(), heading, StringComparison.Ordinal));

        return string.Join("\n", lines);
    }

    private record Section(string Name, string Content);
}
