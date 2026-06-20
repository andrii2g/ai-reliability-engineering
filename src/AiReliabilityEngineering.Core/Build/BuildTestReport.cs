namespace AiReliabilityEngineering.Core.Build;

public sealed record BuildTestReport
{
    public BuildTestReport(
        CommandReport build,
        CommandReport? test)
    {
        Build = build ?? throw new ArgumentNullException(nameof(build));
        Test = test;
    }

    public CommandReport Build { get; }

    public CommandReport? Test { get; }

    public bool Succeeded => Build.Succeeded && (Test?.Succeeded ?? true);
}
