using AiReliabilityEngineering.Core.Planning;

namespace AiReliabilityEngineering.Core.Tests.Planning;

public sealed class ImplementationPlanTests
{
    [Fact]
    public void Constructor_AcceptsValidTasks()
    {
        var task = CreateTask();

        var plan = new ImplementationPlan([task]);

        Assert.Equal([task], plan.Tasks);
    }

    [Fact]
    public void Constructor_RejectsNullTasks()
    {
        Assert.Throws<ArgumentNullException>(() => new ImplementationPlan(null!));
    }

    [Fact]
    public void Constructor_RejectsEmptyTasks()
    {
        Assert.Throws<ArgumentException>(() => new ImplementationPlan([]));
    }

    [Fact]
    public void Constructor_RejectsNullTaskItems()
    {
        Assert.Throws<ArgumentException>(() => new ImplementationPlan([CreateTask(), null!]));
    }

    [Fact]
    public void Constructor_DefensivelyCopiesTasks()
    {
        var task = CreateTask();
        var tasks = new List<ImplementationTask> { task };

        var plan = new ImplementationPlan(tasks);
        tasks.Add(CreateTask("T002"));

        Assert.Equal([task], plan.Tasks);
    }

    private static ImplementationTask CreateTask(string id = "T001")
        => new(
            id,
            "Create skeleton",
            "Create the first implementation skeleton.",
            ["Build passes"]);
}
