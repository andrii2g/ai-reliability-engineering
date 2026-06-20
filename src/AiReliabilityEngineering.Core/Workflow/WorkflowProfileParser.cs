namespace AiReliabilityEngineering.Core.Workflow;

public static class WorkflowProfileParser
{
    public static IReadOnlyList<string> SupportedCliNames { get; } =
    [
        "fake",
        "ai-requirements",
        "ai-demo",
        "ai-demo-dotnet",
        "ai-demo-dotnet-review",
        "ai-demo-dotnet-review-git",
        "ai-demo-dotnet-opencode",
        "ai-demo-dotnet-codex"
    ];

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

            case "ai-demo-dotnet-review-git":
                profile = WorkflowProfile.AiDemoDotnetReviewGit;
                return true;

            case "ai-demo-dotnet-opencode":
                profile = WorkflowProfile.AiDemoDotnetOpenCode;
                return true;

            case "ai-demo-dotnet-codex":
                profile = WorkflowProfile.AiDemoDotnetCodex;
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
            WorkflowProfile.AiDemoDotnetReviewGit => "ai-demo-dotnet-review-git",
            WorkflowProfile.AiDemoDotnetOpenCode => "ai-demo-dotnet-opencode",
            WorkflowProfile.AiDemoDotnetCodex => "ai-demo-dotnet-codex",
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null)
        };
    }
}
