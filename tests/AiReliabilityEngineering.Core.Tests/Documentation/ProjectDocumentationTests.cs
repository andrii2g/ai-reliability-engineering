using AiReliabilityEngineering.Core.Documentation;

namespace AiReliabilityEngineering.Core.Tests.Documentation;

public sealed class ProjectDocumentationTests
{
    [Fact]
    public void Constructor_AcceptsValidValues()
    {
        var documentation = new ProjectDocumentation("# README", "# PLAN");

        Assert.Equal("# README", documentation.ReadmeMarkdown);
        Assert.Equal("# PLAN", documentation.PlanMarkdown);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsInvalidReadmeMarkdown(string? readmeMarkdown)
    {
        Assert.Throws<ArgumentException>(() => new ProjectDocumentation(readmeMarkdown!, "# PLAN"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsInvalidPlanMarkdown(string? planMarkdown)
    {
        Assert.Throws<ArgumentException>(() => new ProjectDocumentation("# README", planMarkdown!));
    }
}
