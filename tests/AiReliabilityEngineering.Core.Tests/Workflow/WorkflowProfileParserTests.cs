using AiReliabilityEngineering.Core.Workflow;

namespace AiReliabilityEngineering.Core.Tests.Workflow;

public sealed class WorkflowProfileParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_NullOrEmptyProfileMapsToFake(string? value)
    {
        var succeeded = WorkflowProfileParser.TryParse(value, out var profile);

        Assert.True(succeeded);
        Assert.Equal(WorkflowProfile.Fake, profile);
    }

    [Fact]
    public void TryParse_FakeParses()
    {
        var succeeded = WorkflowProfileParser.TryParse("fake", out var profile);

        Assert.True(succeeded);
        Assert.Equal(WorkflowProfile.Fake, profile);
    }

    [Fact]
    public void TryParse_AiRequirementsParses()
    {
        var succeeded = WorkflowProfileParser.TryParse("ai-requirements", out var profile);

        Assert.True(succeeded);
        Assert.Equal(WorkflowProfile.AiRequirements, profile);
    }

    [Theory]
    [InlineData("FAKE", WorkflowProfile.Fake)]
    [InlineData("Ai-Requirements", WorkflowProfile.AiRequirements)]
    public void TryParse_IsCaseInsensitive(string value, WorkflowProfile expected)
    {
        var succeeded = WorkflowProfileParser.TryParse(value, out var profile);

        Assert.True(succeeded);
        Assert.Equal(expected, profile);
    }

    [Fact]
    public void TryParse_UnknownProfileReturnsFalse()
    {
        var succeeded = WorkflowProfileParser.TryParse("unknown", out var profile);

        Assert.False(succeeded);
        Assert.Equal(WorkflowProfile.Fake, profile);
    }

    [Theory]
    [InlineData(WorkflowProfile.Fake, "fake")]
    [InlineData(WorkflowProfile.AiRequirements, "ai-requirements")]
    public void ToCliName_ReturnsKebabCaseNames(WorkflowProfile profile, string expected)
    {
        Assert.Equal(expected, WorkflowProfileParser.ToCliName(profile));
    }
}
