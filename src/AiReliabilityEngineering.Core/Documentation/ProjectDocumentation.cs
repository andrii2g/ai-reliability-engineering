namespace AiReliabilityEngineering.Core.Documentation;

public sealed record ProjectDocumentation
{
    public ProjectDocumentation(string readmeMarkdown, string planMarkdown)
    {
        if (string.IsNullOrWhiteSpace(readmeMarkdown))
        {
            throw new ArgumentException("README markdown is required.", nameof(readmeMarkdown));
        }

        if (string.IsNullOrWhiteSpace(planMarkdown))
        {
            throw new ArgumentException("PLAN markdown is required.", nameof(planMarkdown));
        }

        ReadmeMarkdown = readmeMarkdown;
        PlanMarkdown = planMarkdown;
    }

    public string ReadmeMarkdown { get; }

    public string PlanMarkdown { get; }
}
