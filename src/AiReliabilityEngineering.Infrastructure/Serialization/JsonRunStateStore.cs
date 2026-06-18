using System.Text.Json;
using System.Text.Json.Serialization;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Orchestration.State;

namespace AiReliabilityEngineering.Infrastructure.Serialization;

public sealed class JsonRunStateStore(string stateFilePath) : IRunStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task SaveAsync(RunState state, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(stateFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(stateFilePath);
        await JsonSerializer.SerializeAsync(stream, state, SerializerOptions, cancellationToken);
    }
}
