using System.Text.Json;
using AiReliabilityEngineering.Core.Planning;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class PlanningResponseParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ImplementationPlan Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Planning response JSON is required.", nameof(json));
        }

        PlanningResponseDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<PlanningResponseDto>(json, SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Planning response JSON is invalid.", exception);
        }

        if (dto?.Tasks is null)
        {
            throw new InvalidOperationException("Planning response JSON must contain tasks.");
        }

        try
        {
            var tasks = dto.Tasks
                .Select(task => new ImplementationTask(
                    task.Id!,
                    task.Title!,
                    task.Description!,
                    task.AcceptanceCriteria!))
                .ToArray();
            return new ImplementationPlan(tasks);
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentNullException)
        {
            throw new InvalidOperationException("Planning response task shape is invalid.", exception);
        }
    }

    private sealed record PlanningResponseDto
    {
        public IReadOnlyList<ImplementationTaskDto>? Tasks { get; init; }
    }

    private sealed record ImplementationTaskDto
    {
        public string? Id { get; init; }

        public string? Title { get; init; }

        public string? Description { get; init; }

        public IReadOnlyList<string>? AcceptanceCriteria { get; init; }
    }
}
