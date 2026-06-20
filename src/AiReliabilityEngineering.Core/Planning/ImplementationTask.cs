namespace AiReliabilityEngineering.Core.Planning;

public sealed record ImplementationTask
{
    public ImplementationTask(
        string id,
        string title,
        string description,
        IReadOnlyList<string> acceptanceCriteria)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Task id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Task title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Task description is required.", nameof(description));
        }

        if (acceptanceCriteria is null)
        {
            throw new ArgumentNullException(nameof(acceptanceCriteria));
        }

        if (acceptanceCriteria.Count == 0)
        {
            throw new ArgumentException("At least one acceptance criterion is required.", nameof(acceptanceCriteria));
        }

        if (acceptanceCriteria.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Acceptance criteria must not contain empty items.", nameof(acceptanceCriteria));
        }

        Id = id;
        Title = title;
        Description = description;
        AcceptanceCriteria = acceptanceCriteria.ToArray();
    }

    public string Id { get; }

    public string Title { get; }

    public string Description { get; }

    public IReadOnlyList<string> AcceptanceCriteria { get; }
}
