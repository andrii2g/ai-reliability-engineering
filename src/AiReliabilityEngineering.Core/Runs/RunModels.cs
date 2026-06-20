using System.Globalization;
using AiReliabilityEngineering.Core.Steps;

namespace AiReliabilityEngineering.Core.Runs;

public sealed record RunId(string Value)
{
    public override string ToString() => Value;

    public static RunId Create(DateTimeOffset utcNow)
    {
        var timestamp = utcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new RunId($"{timestamp}-{suffix}");
    }
}

public sealed record RunPaths(
    string RootDirectory,
    string InputDirectory,
    string WorkspaceDirectory,
    string ArtifactsDirectory,
    string ReportsDirectory,
    string LogsDirectory,
    string StateFilePath);

public sealed record RunContext(
    RunId Id,
    string OriginalIdeaFilePath,
    string CopiedIdeaFilePath,
    RunPaths Paths,
    DateTimeOffset CreatedAtUtc);

public enum RunStatus
{
    Created = 0,
    Running = 1,
    RequirementsCompleted = 2,
    DocumentationCompleted = 3,
    PlanningCompleted = 4,
    CodeCompleted = 5,
    TestingCompleted = 6,
    ReviewCompleted = 7,
    FinalizationCompleted = 8,
    Completed = 9,
    Failed = 10
}

public sealed record RunState(
    string RunId,
    RunStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<WorkflowStepResult> Steps,
    string? FailureMessage);
