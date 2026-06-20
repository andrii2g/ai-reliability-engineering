using System.Text;
using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Core.Documentation;
using AiReliabilityEngineering.Core.Requirements;
using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class AiDocumentationAgent : IAgent
{
    private const string ReadmeMarker = "---README---";
    private const string PlanMarker = "---PLAN---";
    private readonly IAiProvider _aiProvider;
    private readonly IRunLogger _logger;
    private readonly ProjectSpecificationReader _specificationReader;
    private readonly DocumentationArtifactWriter _artifactWriter;

    public AiDocumentationAgent(
        IAiProvider aiProvider,
        IRunLogger logger,
        ProjectSpecificationReader? specificationReader = null,
        DocumentationArtifactWriter? artifactWriter = null)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _specificationReader = specificationReader ?? new ProjectSpecificationReader();
        _artifactWriter = artifactWriter ?? new DocumentationArtifactWriter();
    }

    public string Name => "AiDocumentationAgent";

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
                "You are AIRE Documentation Agent. Generate concise project documentation from the normalized specification.",
                BuildPrompt(specification),
                AiOutputFormat.Text,
                AiProviderOptions.DefaultFake);

            var response = await _aiProvider.GenerateAsync(request, cancellationToken);
            if (!response.Succeeded)
            {
                await _logger.InfoAsync($"{Name} provider failed: {response.ErrorMessage}", cancellationToken);
                return AgentResult.Failure(response.ErrorMessage ?? "AI provider failed.");
            }

            var documentation = string.Equals(_aiProvider.Name, "fake", StringComparison.OrdinalIgnoreCase)
                ? CreateDeterministicDocumentation(specification)
                : ParseDocumentation(response.Content);
            var artifacts = await _artifactWriter.WriteAsync(documentation, context.Run, cancellationToken);

            await _logger.InfoAsync($"{Name} completed", cancellationToken);
            return AgentResult.Success("AI documentation artifacts generated.", artifacts);
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
        builder.AppendLine("Generate Markdown using exactly these markers:");
        builder.AppendLine(ReadmeMarker);
        builder.AppendLine("<README markdown>");
        builder.AppendLine();
        builder.AppendLine(PlanMarker);
        builder.AppendLine("<PLAN markdown>");
        builder.AppendLine();
        AppendSpecification(builder, specification);
        return builder.ToString();
    }

    private static ProjectDocumentation ParseDocumentation(string content)
    {
        var readmeIndex = content.IndexOf(ReadmeMarker, StringComparison.Ordinal);
        var planIndex = content.IndexOf(PlanMarker, StringComparison.Ordinal);
        if (readmeIndex < 0 || planIndex < 0 || planIndex <= readmeIndex)
        {
            throw new InvalidOperationException("Documentation response did not contain required README and PLAN markers.");
        }

        var readmeStart = readmeIndex + ReadmeMarker.Length;
        var readme = content[readmeStart..planIndex].Trim();
        var planStart = planIndex + PlanMarker.Length;
        var plan = content[planStart..].Trim();
        return new ProjectDocumentation(readme, plan);
    }

    private static ProjectDocumentation CreateDeterministicDocumentation(ProjectSpecification specification)
    {
        var readme = new StringBuilder();
        readme.AppendLine($"# {specification.ProjectName}");
        readme.AppendLine();
        readme.AppendLine(specification.Summary);
        readme.AppendLine();
        AppendList(readme, "Goals", specification.Goals);
        AppendList(readme, "Functional Requirements", specification.FunctionalRequirements);

        var plan = new StringBuilder();
        plan.AppendLine($"# Plan: {specification.ProjectName}");
        plan.AppendLine();
        plan.AppendLine("## Implementation Approach");
        plan.AppendLine();
        plan.AppendLine("Build the project in small, verifiable steps based on the normalized requirements.");
        plan.AppendLine();
        AppendList(plan, "Acceptance Criteria", specification.AcceptanceCriteria);

        return new ProjectDocumentation(readme.ToString(), plan.ToString());
    }

    private static void AppendSpecification(StringBuilder builder, ProjectSpecification specification)
    {
        builder.AppendLine($"Project: {specification.ProjectName}");
        builder.AppendLine($"Summary: {specification.Summary}");
        AppendList(builder, "Goals", specification.Goals);
        AppendList(builder, "Non-Goals", specification.NonGoals);
        AppendList(builder, "Functional Requirements", specification.FunctionalRequirements);
        AppendList(builder, "Acceptance Criteria", specification.AcceptanceCriteria);
    }

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
