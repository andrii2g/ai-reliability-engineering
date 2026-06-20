using AiReliabilityEngineering.Core.Planning;

namespace AiReliabilityEngineering.Core.Tests.Planning;

public sealed class ImplementationTaskTests
{
    [Fact]
    public void Constructor_AcceptsValidValues()
    {
        var task = new ImplementationTask(
            "T001",
            "Create skeleton",
            "Create the first implementation skeleton.",
            ["Build passes"]);

        Assert.Equal("T001", task.Id);
        Assert.Equal("Create skeleton", task.Title);
        Assert.Equal("Create the first implementation skeleton.", task.Description);
        Assert.Equal(["Build passes"], task.AcceptanceCriteria);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsInvalidId(string? id)
    {
        Assert.Throws<ArgumentException>(() => CreateTask(id: id!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsInvalidTitle(string? title)
    {
        Assert.Throws<ArgumentException>(() => CreateTask(title: title!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsInvalidDescription(string? description)
    {
        Assert.Throws<ArgumentException>(() => CreateTask(description: description!));
    }

    [Fact]
    public void Constructor_RejectsNullAcceptanceCriteria()
    {
        Assert.Throws<ArgumentNullException>(() => new ImplementationTask(
            "T001",
            "Create skeleton",
            "Create the first implementation skeleton.",
            null!));
    }

    [Fact]
    public void Constructor_RejectsEmptyAcceptanceCriteria()
    {
        Assert.Throws<ArgumentException>(() => CreateTask(acceptanceCriteria: []));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankAcceptanceCriteriaItems(string? value)
    {
        Assert.Throws<ArgumentException>(() => CreateTask(acceptanceCriteria: ["Build passes", value!]));
    }

    [Fact]
    public void Constructor_DefensivelyCopiesAcceptanceCriteria()
    {
        var criteria = new List<string> { "Build passes" };

        var task = CreateTask(acceptanceCriteria: criteria);
        criteria.Add("Mutation");

        Assert.Equal(["Build passes"], task.AcceptanceCriteria);
    }

    private static ImplementationTask CreateTask(
        string id = "T001",
        string title = "Create skeleton",
        string description = "Create the first implementation skeleton.",
        IReadOnlyList<string>? acceptanceCriteria = null)
        => new(
            id,
            title,
            description,
            acceptanceCriteria ?? ["Build passes"]);
}
