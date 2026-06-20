using AiReliabilityEngineering.Core.Ai;
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

    [Fact]
    public void Constructor_WithNoProviderDefaultsToFakeProvider()
    {
        var request = new RunRequest("idea.md", "runs");

        Assert.Equal(AiProviderKind.Fake, request.EffectiveAiProvider.Kind);
        Assert.Equal(AiProviderOptions.DefaultFake.Model, request.EffectiveAiProvider.Model);
    }

    [Fact]
    public void Constructor_WithExplicitProviderRetainsProvider()
    {
        var selection = new AiProviderSelection(AiProviderKind.OpenAi, "test-model");

        var request = new RunRequest("idea.md", "runs", WorkflowProfile.AiRequirements, selection);

        Assert.Same(selection, request.EffectiveAiProvider);
    }
}
