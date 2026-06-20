using System.Text.Json;
using AiReliabilityEngineering.Core.Requirements;
using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class ProjectSpecificationReader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ProjectSpecification> ReadAsync(
        RunContext runContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(runContext);

        var path = Path.Combine(runContext.Paths.ArtifactsDirectory, "specification.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Project specification artifact was not found.", path);
        }

        ProjectSpecificationDto? dto;
        try
        {
            await using var stream = File.OpenRead(path);
            dto = await JsonSerializer.DeserializeAsync<ProjectSpecificationDto>(
                stream,
                SerializerOptions,
                cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Project specification JSON is invalid.", exception);
        }

        if (dto is null)
        {
            throw new InvalidOperationException("Project specification JSON is empty.");
        }

        try
        {
            return new ProjectSpecification(
                dto.ProjectName!,
                dto.Summary!,
                dto.Goals!,
                dto.NonGoals!,
                dto.FunctionalRequirements!,
                dto.AcceptanceCriteria!);
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentNullException)
        {
            throw new InvalidOperationException("Project specification shape is invalid.", exception);
        }
    }

    private sealed record ProjectSpecificationDto
    {
        public string? ProjectName { get; init; }

        public string? Summary { get; init; }

        public IReadOnlyList<string>? Goals { get; init; }

        public IReadOnlyList<string>? NonGoals { get; init; }

        public IReadOnlyList<string>? FunctionalRequirements { get; init; }

        public IReadOnlyList<string>? AcceptanceCriteria { get; init; }
    }
}
