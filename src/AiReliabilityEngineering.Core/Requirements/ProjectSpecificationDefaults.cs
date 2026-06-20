namespace AiReliabilityEngineering.Core.Requirements;

public static class ProjectSpecificationDefaults
{
    public static IReadOnlyList<string> DefaultNonGoals { get; } =
        new[]
        {
            "Do not implement real AI provider calls in this step.",
            "Do not modify source repositories outside the run workspace."
        };

    public static IReadOnlyList<string> DefaultFunctionalRequirements { get; } =
        new[]
        {
            "Read the project idea from the copied input Markdown file.",
            "Generate a normalized project specification.",
            "Write specification.json.",
            "Write requirements.md."
        };

    public static IReadOnlyList<string> DefaultAcceptanceCriteria { get; } =
        new[]
        {
            "The generated specification.json is valid JSON.",
            "The generated requirements.md file exists.",
            "The requirements agent returns a successful AgentResult.",
            "The AI provider abstraction is called."
        };
}
