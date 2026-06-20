using AiReliabilityEngineering.Core.Review;

namespace AiReliabilityEngineering.Core.Tests.Review;

public sealed class WorkspaceSummaryTests
{
    [Fact]
    public void Constructor_AcceptsValidValues()
    {
        var summary = new WorkspaceSummary("workspace", ["src/Program.cs"]);

        Assert.Equal("workspace", summary.WorkspaceRoot);
        Assert.Equal(["src/Program.cs"], summary.Files);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankWorkspaceRoot(string? workspaceRoot)
    {
        Assert.Throws<ArgumentException>(() => new WorkspaceSummary(workspaceRoot!, []));
    }

    [Fact]
    public void Constructor_RejectsNullFiles()
    {
        Assert.Throws<ArgumentNullException>(() => new WorkspaceSummary("workspace", null!));
    }

    [Fact]
    public void Constructor_RejectsBlankFileEntries()
    {
        Assert.Throws<ArgumentException>(() => new WorkspaceSummary("workspace", ["src/Program.cs", " "]));
    }

    [Fact]
    public void Constructor_NormalizesAndSortsFiles()
    {
        var summary = new WorkspaceSummary("workspace", [@"tests\SmokeTests.cs", "src/Program.cs"]);

        Assert.Equal(["src/Program.cs", "tests/SmokeTests.cs"], summary.Files);
    }
}
