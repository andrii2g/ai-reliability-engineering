using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Core.Steps;
using AiReliabilityEngineering.Core.Workflow;
using AiReliabilityEngineering.Infrastructure.Ai;
using AiReliabilityEngineering.Orchestration.Agents;
using AiReliabilityEngineering.Orchestration.Logging;
using AiReliabilityEngineering.Orchestration.Pipeline;
using AiReliabilityEngineering.Orchestration.State;

namespace AiReliabilityEngineering.Orchestration.Tests.Pipeline;

public sealed class AgentPipelineFactoryTests
{
    [Fact]
    public void Create_FakeProfileCreatesFakeRequirementsAgent()
    {
        var pipeline = CreateFactory().Create(
            WorkflowProfile.Fake,
            AiProviderSelection.DefaultFake,
            new InMemoryRunLogger(),
            new InMemoryRunStateStore());

        Assert.Equal(6, pipeline.Steps.Count);
        Assert.Equal(WorkflowStep.Requirements, pipeline.Steps[0].Step);
        Assert.IsType<FakeRequirementsAgent>(pipeline.Steps[0].Agent);
        Assert.DoesNotContain(pipeline.Steps, step => step.Agent is AiRequirementsAgent);
    }

    [Fact]
    public void Create_AiRequirementsProfileCreatesAiRequirementsAgent()
    {
        var pipeline = CreateFactory().Create(
            WorkflowProfile.AiRequirements,
            AiProviderSelection.DefaultFake,
            new InMemoryRunLogger(),
            new InMemoryRunStateStore());

        Assert.Equal(WorkflowStep.Requirements, pipeline.Steps[0].Step);
        Assert.IsType<AiRequirementsAgent>(pipeline.Steps[0].Agent);
        Assert.IsType<FakeDocumentationAgent>(pipeline.Steps[1].Agent);
        Assert.IsType<FakePlannerAgent>(pipeline.Steps[2].Agent);
        Assert.IsType<FakeCodeAgent>(pipeline.Steps[3].Agent);
        Assert.IsType<FakeTestAgent>(pipeline.Steps[4].Agent);
        Assert.IsType<FakeReviewerAgent>(pipeline.Steps[5].Agent);
    }

    [Fact]
    public void Create_BothProfilesKeepSameStepOrder()
    {
        var factory = CreateFactory();

        var fakeSteps = factory.Create(
            WorkflowProfile.Fake,
            AiProviderSelection.DefaultFake,
            new InMemoryRunLogger(),
            new InMemoryRunStateStore()).Steps.Select(step => step.Step);
        var aiSteps = factory.Create(
            WorkflowProfile.AiRequirements,
            AiProviderSelection.DefaultFake,
            new InMemoryRunLogger(),
            new InMemoryRunStateStore()).Steps.Select(step => step.Step);

        WorkflowStep[] expected =
        [
            WorkflowStep.Requirements,
            WorkflowStep.Documentation,
            WorkflowStep.Planning,
            WorkflowStep.Code,
            WorkflowStep.Testing,
            WorkflowStep.Review
        ];
        Assert.Equal(expected, fakeSteps);
        Assert.Equal(expected, aiSteps);
    }

    [Fact]
    public void Create_RejectsUnsupportedProfile()
    {
        var factory = CreateFactory();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            factory.Create(
                (WorkflowProfile)999,
                AiProviderSelection.DefaultFake,
                new InMemoryRunLogger(),
                new InMemoryRunStateStore()));
    }

    [Fact]
    public void Create_AiRequirementsProfilePassesProviderSelectionToFactory()
    {
        AiProviderSelection? receivedSelection = null;
        var expectedSelection = new AiProviderSelection(AiProviderKind.OpenAi, "test-model");
        var factory = new AgentPipelineFactory(selection =>
        {
            receivedSelection = selection;
            return new FakeAiProvider();
        });

        _ = factory.Create(
            WorkflowProfile.AiRequirements,
            expectedSelection,
            new InMemoryRunLogger(),
            new InMemoryRunStateStore());

        Assert.Same(expectedSelection, receivedSelection);
    }

    private static AgentPipelineFactory CreateFactory()
        => new(_ => new FakeAiProvider());

    private sealed class InMemoryRunLogger : IRunLogger
    {
        public Task InfoAsync(string message, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ErrorAsync(string message, Exception? exception, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemoryRunStateStore : IRunStateStore
    {
        public Task SaveAsync(RunState state, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
