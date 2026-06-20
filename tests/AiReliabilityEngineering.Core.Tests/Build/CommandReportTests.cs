using AiReliabilityEngineering.Core.Build;

namespace AiReliabilityEngineering.Core.Tests.Build;

public sealed class CommandReportTests
{
    [Fact]
    public void Constructor_AcceptsValidValues()
    {
        var report = new CommandReport("dotnet build", "workspace", 0, "out", "err", TimeSpan.FromSeconds(1));

        Assert.Equal("dotnet build", report.Command);
        Assert.Equal("workspace", report.WorkingDirectory);
        Assert.Equal(0, report.ExitCode);
        Assert.Equal("out", report.StandardOutput);
        Assert.Equal("err", report.StandardError);
        Assert.True(report.Succeeded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankCommand(string? command)
    {
        Assert.Throws<ArgumentException>(() => new CommandReport(command!, "workspace", 0, null, null, TimeSpan.Zero));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankWorkingDirectory(string? workingDirectory)
    {
        Assert.Throws<ArgumentException>(() => new CommandReport("dotnet build", workingDirectory!, 0, null, null, TimeSpan.Zero));
    }

    [Fact]
    public void Constructor_RejectsNegativeDuration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CommandReport("dotnet build", "workspace", 0, null, null, TimeSpan.FromTicks(-1)));
    }

    [Fact]
    public void Succeeded_IsFalseWhenExitCodeIsNonZero()
    {
        var report = new CommandReport("dotnet build", "workspace", 1, null, null, TimeSpan.Zero);

        Assert.False(report.Succeeded);
    }
}
