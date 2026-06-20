using AiReliabilityEngineering.Orchestration.Agents;

namespace AiReliabilityEngineering.Orchestration.Tests.Agents;

public sealed class RequirementsNormalizerTests
{
    [Fact]
    public void Normalize_ExtractsH1AsProjectName()
    {
        var normalizer = new RequirementsNormalizer();

        var specification = normalizer.Normalize("""
            # Redis TTL Audit Tool

            Scan Redis keys and report keys without TTL.
            """);

        Assert.Equal("Redis TTL Audit Tool", specification.ProjectName);
        Assert.Equal("Scan Redis keys and report keys without TTL.", specification.Summary);
    }

    [Fact]
    public void Normalize_UsesFirstNonEmptyLineWhenNoH1Exists()
    {
        var normalizer = new RequirementsNormalizer();

        var specification = normalizer.Normalize("""
            Redis TTL Audit Tool

            Scan Redis keys.
            """);

        Assert.Equal("Redis TTL Audit Tool", specification.ProjectName);
        Assert.Equal("Scan Redis keys.", specification.Summary);
    }

    [Fact]
    public void Normalize_EmptyIdeaTextProducesUntitledProject()
    {
        var normalizer = new RequirementsNormalizer();

        var specification = normalizer.Normalize("   ");

        Assert.Equal("Untitled AIRE Project", specification.ProjectName);
        Assert.Equal("Untitled AIRE Project", specification.Summary);
    }

    [Fact]
    public void Normalize_NullIdeaTextThrows()
    {
        var normalizer = new RequirementsNormalizer();

        Assert.Throws<ArgumentNullException>(() => normalizer.Normalize(null!));
    }

    [Fact]
    public void Normalize_PopulatesDefaults()
    {
        var normalizer = new RequirementsNormalizer();

        var specification = normalizer.Normalize("# Project");

        Assert.NotEmpty(specification.Goals);
        Assert.NotEmpty(specification.NonGoals);
        Assert.NotEmpty(specification.FunctionalRequirements);
        Assert.NotEmpty(specification.AcceptanceCriteria);
    }
}
