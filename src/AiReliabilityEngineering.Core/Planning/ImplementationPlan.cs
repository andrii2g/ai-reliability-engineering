namespace AiReliabilityEngineering.Core.Planning;

public sealed record ImplementationPlan
{
    public ImplementationPlan(IReadOnlyList<ImplementationTask> tasks)
    {
        if (tasks is null)
        {
            throw new ArgumentNullException(nameof(tasks));
        }

        if (tasks.Count == 0)
        {
            throw new ArgumentException("At least one implementation task is required.", nameof(tasks));
        }

        if (tasks.Any(task => task is null))
        {
            throw new ArgumentException("Tasks must not contain null entries.", nameof(tasks));
        }

        Tasks = tasks.ToArray();
    }

    public IReadOnlyList<ImplementationTask> Tasks { get; }
}
