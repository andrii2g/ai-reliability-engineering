using AiReliabilityEngineering.Core.Review;

namespace AiReliabilityEngineering.Core.Tests.Review;

public sealed class RequiredArtifactCheckTests
{
    [Fact]
    public void Constructor_AcceptsValidValues()
    {
        var check = new RequiredArtifactCheck("artifacts/specification.json", true, "artifact");

        Assert.Equal("artifacts/specification.json", check.RelativePath);
        Assert.True(check.Exists);
        Assert.Equal("artifact", check.Category);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankRelativePath(string? relativePath)
    {
        Assert.Throws<ArgumentException>(() => new RequiredArtifactCheck(relativePath!, true, "artifact"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankCategory(string? category)
    {
        Assert.Throws<ArgumentException>(() => new RequiredArtifactCheck("artifacts/specification.json", true, category!));
    }

    [Fact]
    public void Constructor_NormalizesPathSeparators()
    {
        var check = new RequiredArtifactCheck(@"artifacts\specification.json", true, "artifact");

        Assert.Equal("artifacts/specification.json", check.RelativePath);
    }
}
