using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class AiRequirementsAgent : IAgent
{
    private readonly IAiProvider _aiProvider;
    private readonly IRunLogger _logger;
    private readonly RequirementsNormalizer _normalizer;
    private readonly RequirementsArtifactWriter _artifactWriter;

    public AiRequirementsAgent(
        IAiProvider aiProvider,
        IRunLogger logger,
        RequirementsNormalizer? normalizer = null,
        RequirementsArtifactWriter? artifactWriter = null)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _normalizer = normalizer ?? new RequirementsNormalizer();
        _artifactWriter = artifactWriter ?? new RequirementsArtifactWriter();
    }

    public string Name => "AiRequirementsAgent";

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        await _logger.InfoAsync($"{Name} started", cancellationToken);

        try
        {
            if (!File.Exists(context.Run.CopiedIdeaFilePath))
            {
                return AgentResult.Failure($"Copied idea file not found: {context.Run.CopiedIdeaFilePath}");
            }

            var ideaText = await File.ReadAllTextAsync(context.Run.CopiedIdeaFilePath, cancellationToken);
            var request = AiRequest.FromPrompts(
                "You are AIRE Requirements Agent. Analyze the project idea and help produce a normalized project specification.",
                $"Project idea:{Environment.NewLine}{ideaText}",
                AiOutputFormat.Text,
                AiProviderOptions.DefaultFake);

            var response = await _aiProvider.GenerateAsync(request, cancellationToken);
            if (!response.Succeeded)
            {
                await _logger.InfoAsync($"{Name} provider failed: {response.ErrorMessage}", cancellationToken);
                return AgentResult.Failure(response.ErrorMessage ?? "AI provider failed.");
            }

            var specification = _normalizer.Normalize(ideaText);
            var artifacts = await _artifactWriter.WriteAsync(specification, context.Run, cancellationToken);

            await _logger.InfoAsync($"{Name} completed", cancellationToken);
            return AgentResult.Success("AI requirements artifacts generated.", artifacts);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await _logger.ErrorAsync($"{Name} failed", exception, cancellationToken);
            return AgentResult.Failure(exception.Message);
        }
    }
}
