namespace AiReliabilityEngineering.Core.Workflow;

public static class WorkflowProfileParser
{
    public static IReadOnlyList<string> SupportedCliNames { get; } =
        ["fake", "ai-requirements"];

    public static bool TryParse(
        string? value,
        out WorkflowProfile profile)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            profile = WorkflowProfile.Fake;
            return true;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "fake":
                profile = WorkflowProfile.Fake;
                return true;

            case "ai-requirements":
                profile = WorkflowProfile.AiRequirements;
                return true;

            default:
                profile = WorkflowProfile.Fake;
                return false;
        }
    }

    public static string ToCliName(WorkflowProfile profile)
    {
        return profile switch
        {
            WorkflowProfile.Fake => "fake",
            WorkflowProfile.AiRequirements => "ai-requirements",
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null)
        };
    }
}
