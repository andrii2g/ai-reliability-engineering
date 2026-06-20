namespace AiReliabilityEngineering.Core.Review;

public sealed record ArtifactReviewResult
{
    public ArtifactReviewResult(
        IReadOnlyList<RequiredArtifactCheck> checks,
        WorkspaceSummary workspaceSummary,
        IReadOnlyList<string?>? warnings)
    {
        ArgumentNullException.ThrowIfNull(checks);

        if (checks.Any(check => check is null))
        {
            throw new ArgumentException("Checks must not contain null entries.", nameof(checks));
        }

        WorkspaceSummary = workspaceSummary ?? throw new ArgumentNullException(nameof(workspaceSummary));
        Warnings = (warnings ?? [])
            .Where(warning => !string.IsNullOrWhiteSpace(warning))
            .Select(warning => warning!)
            .ToArray();
        Checks = checks.ToArray();
    }

    public IReadOnlyList<RequiredArtifactCheck> Checks { get; }

    public WorkspaceSummary WorkspaceSummary { get; }

    public IReadOnlyList<string> Warnings { get; }

    public IReadOnlyList<RequiredArtifactCheck> Missing =>
        Checks.Where(check => !check.Exists).ToArray();

    public bool HasMissingRequiredFiles => Missing.Count > 0;
}
