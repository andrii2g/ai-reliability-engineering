using AiReliabilityEngineering.Core.Requirements;

namespace AiReliabilityEngineering.Core.Tests.Requirements;

public sealed class ProjectSpecificationTests
{
    private static readonly string[] Goals = ["Goal"];
    private static readonly string[] NonGoals = ["Non-goal"];
    private static readonly string[] FunctionalRequirements = ["Functional requirement"];
    private static readonly string[] AcceptanceCriteria = ["Acceptance criterion"];

    [Fact]
    public void Constructor_AcceptsValidValues()
    {
        var specification = CreateSpecification();

        Assert.Equal("Project", specification.ProjectName);
        Assert.Equal("Summary", specification.Summary);
        Assert.Equal(Goals, specification.Goals);
        Assert.Equal(NonGoals, specification.NonGoals);
        Assert.Equal(FunctionalRequirements, specification.FunctionalRequirements);
        Assert.Equal(AcceptanceCriteria, specification.AcceptanceCriteria);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankProjectName(string projectName)
    {
        Assert.Throws<ArgumentException>(() =>
            new ProjectSpecification(projectName, "Summary", Goals, NonGoals, FunctionalRequirements, AcceptanceCriteria));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankSummary(string summary)
    {
        Assert.Throws<ArgumentException>(() =>
            new ProjectSpecification("Project", summary, Goals, NonGoals, FunctionalRequirements, AcceptanceCriteria));
    }

    [Fact]
    public void Constructor_RejectsNullLists()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectSpecification("Project", "Summary", null!, NonGoals, FunctionalRequirements, AcceptanceCriteria));
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectSpecification("Project", "Summary", Goals, null!, FunctionalRequirements, AcceptanceCriteria));
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectSpecification("Project", "Summary", Goals, NonGoals, null!, AcceptanceCriteria));
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectSpecification("Project", "Summary", Goals, NonGoals, FunctionalRequirements, null!));
    }

    [Fact]
    public void Constructor_RejectsEmptyLists()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProjectSpecification("Project", "Summary", [], NonGoals, FunctionalRequirements, AcceptanceCriteria));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankListItems(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            new ProjectSpecification("Project", "Summary", [value!], NonGoals, FunctionalRequirements, AcceptanceCriteria));
    }

    [Fact]
    public void Constructor_DefensivelyCopiesLists()
    {
        var goals = new List<string> { "Goal" };

        var specification = new ProjectSpecification(
            "Project",
            "Summary",
            goals,
            NonGoals,
            FunctionalRequirements,
            AcceptanceCriteria);
        goals.Add("Changed");

        Assert.Single(specification.Goals);
        Assert.Equal("Goal", specification.Goals[0]);
    }

    private static ProjectSpecification CreateSpecification()
        => new(
            "Project",
            "Summary",
            Goals,
            NonGoals,
            FunctionalRequirements,
            AcceptanceCriteria);
}
