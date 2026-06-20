using AiReliabilityEngineering.Core.Build;

namespace AiReliabilityEngineering.Core.Tests.Build;

public sealed class BuildTestReportTests
{
    [Fact]
    public void Constructor_RejectsNullBuildReport()
    {
        Assert.Throws<ArgumentNullException>(() => new BuildTestReport(null!, null));
    }

    [Fact]
    public void Succeeded_IsTrueWhenBuildSucceedsAndTestsWereNotRun()
    {
        var report = new BuildTestReport(CreateCommandReport(0), null);

        Assert.True(report.Succeeded);
    }

    [Fact]
    public void Succeeded_IsTrueWhenBuildAndTestsSucceed()
    {
        var report = new BuildTestReport(CreateCommandReport(0), CreateCommandReport(0));

        Assert.True(report.Succeeded);
    }

    [Fact]
    public void Succeeded_IsFalseWhenBuildFails()
    {
        var report = new BuildTestReport(CreateCommandReport(1), CreateCommandReport(0));

        Assert.False(report.Succeeded);
    }

    [Fact]
    public void Succeeded_IsFalseWhenTestsFail()
    {
        var report = new BuildTestReport(CreateCommandReport(0), CreateCommandReport(1));

        Assert.False(report.Succeeded);
    }

    private static CommandReport CreateCommandReport(int exitCode)
        => new("dotnet build", "workspace", exitCode, null, null, TimeSpan.Zero);
}
