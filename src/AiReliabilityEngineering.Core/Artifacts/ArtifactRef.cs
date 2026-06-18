namespace AiReliabilityEngineering.Core.Artifacts;

public sealed record ArtifactRef(
    ArtifactType Type,
    string RelativePath,
    string Description);

public enum ArtifactType
{
    Specification = 0,
    Documentation = 1,
    Plan = 2,
    Tasks = 3,
    Code = 4,
    Tests = 5,
    Review = 6,
    Log = 7,
    Report = 8,
    Other = 100
}
