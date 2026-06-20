using AiReliabilityEngineering.Core.Requirements;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class RequirementsNormalizer
{
    private const string UntitledProjectName = "Untitled AIRE Project";

    public ProjectSpecification Normalize(string ideaText)
    {
        ArgumentNullException.ThrowIfNull(ideaText);

        var meaningfulLines = ideaText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        var titleLine = meaningfulLines.FirstOrDefault(line => line.StartsWith("# ", StringComparison.Ordinal));
        var titleSourceLine = titleLine ?? meaningfulLines.FirstOrDefault();
        var projectName = NormalizeProjectName(titleSourceLine ?? UntitledProjectName);
        var summary = FindSummary(meaningfulLines, titleSourceLine, projectName);

        return new ProjectSpecification(
            projectName,
            summary,
            ["Convert the provided idea into a structured project specification."],
            ProjectSpecificationDefaults.DefaultNonGoals,
            ProjectSpecificationDefaults.DefaultFunctionalRequirements,
            ProjectSpecificationDefaults.DefaultAcceptanceCriteria);
    }

    private static string NormalizeProjectName(string value)
    {
        var normalized = value.Trim().TrimStart('#').Trim();
        return string.IsNullOrWhiteSpace(normalized) ? UntitledProjectName : normalized;
    }

    private static string FindSummary(
        IReadOnlyList<string> meaningfulLines,
        string? titleSourceLine,
        string projectName)
    {
        foreach (var line in meaningfulLines)
        {
            if (titleSourceLine is not null && string.Equals(line, titleSourceLine, StringComparison.Ordinal))
            {
                continue;
            }

            var candidate = line.Trim();
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return NormalizeProjectName(candidate);
            }
        }

        return projectName;
    }
}
