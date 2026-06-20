namespace AiReliabilityEngineering.Core.Requirements;

public sealed record ProjectSpecification
{
    public ProjectSpecification(
        string projectName,
        string summary,
        IReadOnlyList<string> goals,
        IReadOnlyList<string> nonGoals,
        IReadOnlyList<string> functionalRequirements,
        IReadOnlyList<string> acceptanceCriteria)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("Project name is required.", nameof(projectName));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Summary is required.", nameof(summary));
        }

        ProjectName = projectName;
        Summary = summary;
        Goals = CopyRequiredList(goals, nameof(goals));
        NonGoals = CopyRequiredList(nonGoals, nameof(nonGoals));
        FunctionalRequirements = CopyRequiredList(functionalRequirements, nameof(functionalRequirements));
        AcceptanceCriteria = CopyRequiredList(acceptanceCriteria, nameof(acceptanceCriteria));
    }

    public string ProjectName { get; }

    public string Summary { get; }

    public IReadOnlyList<string> Goals { get; }

    public IReadOnlyList<string> NonGoals { get; }

    public IReadOnlyList<string> FunctionalRequirements { get; }

    public IReadOnlyList<string> AcceptanceCriteria { get; }

    private static IReadOnlyList<string> CopyRequiredList(
        IReadOnlyList<string> values,
        string parameterName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (values.Count == 0)
        {
            throw new ArgumentException("At least one item is required.", parameterName);
        }

        if (values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Items must not be null, empty, or whitespace.", parameterName);
        }

        return values.ToArray();
    }
}
