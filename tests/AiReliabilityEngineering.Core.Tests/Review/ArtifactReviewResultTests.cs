using AiReliabilityEngineering.Core.Review;

namespace AiReliabilityEngineering.Core.Tests.Review;

public sealed class ArtifactReviewResultTests
{
    [Fact]
    public void Constructor_AcceptsValidValues()
    {
        var result = new ArtifactReviewResult([CreateCheck(true)], new WorkspaceSummary("workspace", []), ["warning"]);

        Assert.Single(result.Checks);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Constructor_RejectsNullChecks()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ArtifactReviewResult(null!, new WorkspaceSummary("workspace", []), []));
    }

    [Fact]
    public void Constructor_RejectsNullWorkspaceSummary()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ArtifactReviewResult([CreateCheck(true)], null!, []));
    }

    [Fact]
    public void Missing_ContainsOnlyMissingFiles()
    {
        var result = new ArtifactReviewResult(
            [CreateCheck(true), CreateCheck(false)],
            new WorkspaceSummary("workspace", []),
            []);

        Assert.Single(result.Missing);
        Assert.True(result.HasMissingRequiredFiles);
    }

    [Fact]
    public void Warnings_IgnoreNullAndBlankValues()
    {
        var result = new ArtifactReviewResult(
            [CreateCheck(true)],
            new WorkspaceSummary("workspace", []),
            [null, "", "  ", "warning"]);

        Assert.Equal(["warning"], result.Warnings);
    }

    private static RequiredArtifactCheck CreateCheck(bool exists)
        => new("artifacts/specification.json", exists, "artifact");
}
