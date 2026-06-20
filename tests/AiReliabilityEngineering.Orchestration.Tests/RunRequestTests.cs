using AiReliabilityEngineering.Core.Workflow;
using AiReliabilityEngineering.Orchestration.RunManagement;

namespace AiReliabilityEngineering.Orchestration.Tests;

public sealed class RunRequestTests
{
    [Fact]
    public void Constructor_WithTwoArgumentsDefaultsProfileToFake()
    {
        var request = new RunRequest("idea.md", "runs");

        Assert.Equal(WorkflowProfile.Fake, request.Profile);
    }

    [Fact]
    public void Constructor_WithExplicitProfileRetainsProfile()
    {
        var request = new RunRequest("idea.md", "runs", WorkflowProfile.AiRequirements);

        Assert.Equal(WorkflowProfile.AiRequirements, request.Profile);
    }
}
