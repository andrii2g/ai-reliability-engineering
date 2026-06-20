using System.Text;
using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Core.Planning;
using AiReliabilityEngineering.Core.Requirements;
using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class AiPlannerAgent : IAgent
{
    private readonly IAiProvider _aiProvider;
    private readonly IRunLogger _logger;
    private readonly ProjectSpecificationReader _specificationReader;
    private readonly PlanningResponseParser _parser;
    private readonly PlannerArtifactWriter _artifactWriter;

    public AiPlannerAgent(
        IAiProvider aiProvider,
        IRunLogger logger,
        ProjectSpecificationReader? specificationReader = null,
        PlanningResponseParser? parser = null,
        PlannerArtifactWriter? artifactWriter = null)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _specificationReader = specificationReader ?? new ProjectSpecificationReader();
        _parser = parser ?? new PlanningResponseParser();
        _artifactWriter = artifactWriter ?? new PlannerArtifactWriter();
    }

    public string Name => "AiPlannerAgent";

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        await _logger.InfoAsync($"{Name} started", cancellationToken);

        try
        {
            var specification = await _specificationReader.ReadAsync(context.Run, cancellationToken);
            var request = AiRequest.FromPrompts(
                "You are AIRE Planner Agent. Generate implementation tasks from the normalized specification.",
                BuildPrompt(specification),
                AiOutputFormat.Json,
                AiProviderOptions.DefaultFake);

            var response = await _aiProvider.GenerateAsync(request, cancellationToken);
            if (!response.Succeeded)
            {
                await _logger.InfoAsync($"{Name} provider failed: {response.ErrorMessage}", cancellationToken);
                return AgentResult.Failure(response.ErrorMessage ?? "AI provider failed.");
            }

            var plan = string.Equals(_aiProvider.Name, "fake", StringComparison.OrdinalIgnoreCase)
                ? CreateDeterministicPlan(specification)
                : _parser.Parse(response.Content);
            var artifacts = await _artifactWriter.WriteAsync(plan, context.Run, cancellationToken);

            await _logger.InfoAsync($"{Name} completed", cancellationToken);
            return AgentResult.Success("AI planning artifacts generated.", artifacts);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await _logger.ErrorAsync($"{Name} failed", exception, cancellationToken);
            return AgentResult.Failure(exception.Message);
        }
    }

    private static string BuildPrompt(ProjectSpecification specification)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Return valid JSON only with this shape:");
        builder.AppendLine("""
            {
              "tasks": [
                {
                  "id": "T001",
                  "title": "Create CLI skeleton",
                  "description": "Create the basic command line structure.",
                  "acceptanceCriteria": [
                    "CLI starts",
                    "Help command works"
                  ]
                }
              ]
            }
            """);
        builder.AppendLine();
        builder.AppendLine($"Project: {specification.ProjectName}");
        builder.AppendLine($"Summary: {specification.Summary}");
        AppendList(builder, "Goals", specification.Goals);
        AppendList(builder, "Functional Requirements", specification.FunctionalRequirements);
        AppendList(builder, "Acceptance Criteria", specification.AcceptanceCriteria);
        return builder.ToString();
    }

    private static ImplementationPlan CreateDeterministicPlan(ProjectSpecification specification)
        => new(
            [
                new ImplementationTask(
                    "T001",
                    $"Create {specification.ProjectName} skeleton",
                    "Create the initial project structure and command line entry point.",
                    [
                        "Project builds successfully",
                        "CLI help command works"
                    ]),
                new ImplementationTask(
                    "T002",
                    "Implement core requirements",
                    specification.FunctionalRequirements[0],
                    specification.AcceptanceCriteria)
            ]);

    private static void AppendList(StringBuilder builder, string heading, IReadOnlyList<string> values)
    {
        builder.AppendLine($"## {heading}");
        builder.AppendLine();
        foreach (var value in values)
        {
            builder.AppendLine($"- {value}");
        }

        builder.AppendLine();
    }
}
