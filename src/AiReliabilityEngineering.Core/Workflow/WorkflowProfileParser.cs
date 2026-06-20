namespace AiReliabilityEngineering.Core.Workflow;

public static class WorkflowProfileParser
{
    public static IReadOnlyList<string> SupportedCliNames { get; } =
        ["fake", "ai-requirements", "ai-demo", "ai-demo-dotnet", "ai-demo-dotnet-review"];

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

            case "ai-demo":
                profile = WorkflowProfile.AiDemo;
                return true;

            case "ai-demo-dotnet":
                profile = WorkflowProfile.AiDemoDotnet;
                return true;

            case "ai-demo-dotnet-review":
                profile = WorkflowProfile.AiDemoDotnetReview;
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
            WorkflowProfile.AiDemo => "ai-demo",
            WorkflowProfile.AiDemoDotnet => "ai-demo-dotnet",
            WorkflowProfile.AiDemoDotnetReview => "ai-demo-dotnet-review",
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null)
        };
    }
}
