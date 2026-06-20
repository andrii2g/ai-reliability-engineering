using AiReliabilityEngineering.Core.Ai;

namespace AiReliabilityEngineering.Core.Tests.Ai;

public sealed class AiResponseTests
{
    [Fact]
    public void Success_CreatesSuccessfulResponse()
    {
        var response = AiResponse.Success("content", "fake", "fake-model");

        Assert.True(response.Succeeded);
        Assert.Equal("content", response.Content);
        Assert.Null(response.ErrorMessage);
        Assert.Equal("fake", response.Provider);
        Assert.Equal("fake-model", response.Model);
    }

    [Fact]
    public void Success_RejectsNullContentProviderAndModel()
    {
        Assert.Throws<ArgumentNullException>(() => AiResponse.Success(null!, "fake", "fake-model"));
        Assert.Throws<ArgumentNullException>(() => AiResponse.Success("content", null!, "fake-model"));
        Assert.Throws<ArgumentNullException>(() => AiResponse.Success("content", "fake", null!));
    }

    [Fact]
    public void Failure_CreatesFailedResponse()
    {
        var response = AiResponse.Failure("failed", "fake", "fake-model");

        Assert.False(response.Succeeded);
        Assert.Equal(string.Empty, response.Content);
        Assert.Equal("failed", response.ErrorMessage);
        Assert.Equal("fake", response.Provider);
        Assert.Equal("fake-model", response.Model);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Failure_RejectsBlankErrorMessage(string? errorMessage)
    {
        Assert.Throws<ArgumentException>(() => AiResponse.Failure(errorMessage!, "fake", "fake-model"));
    }
}
