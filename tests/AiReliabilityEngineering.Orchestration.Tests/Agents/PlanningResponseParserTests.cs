using AiReliabilityEngineering.Orchestration.Agents;

namespace AiReliabilityEngineering.Orchestration.Tests.Agents;

public sealed class PlanningResponseParserTests
{
    [Fact]
    public void Parse_ParsesValidTasksJson()
    {
        var parser = new PlanningResponseParser();

        var plan = parser.Parse(ValidJson);

        Assert.Single(plan.Tasks);
        Assert.Equal("T001", plan.Tasks[0].Id);
        Assert.Equal("Create skeleton", plan.Tasks[0].Title);
        Assert.Equal(["Build passes", "Tests pass"], plan.Tasks[0].AcceptanceCriteria);
    }

    [Fact]
    public void Parse_RejectsNullInput()
    {
        var parser = new PlanningResponseParser();

        Assert.Throws<ArgumentNullException>(() => parser.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_RejectsBlankInput(string json)
    {
        var parser = new PlanningResponseParser();

        Assert.Throws<ArgumentException>(() => parser.Parse(json));
    }

    [Fact]
    public void Parse_RejectsInvalidJson()
    {
        var parser = new PlanningResponseParser();

        Assert.Throws<InvalidOperationException>(() => parser.Parse("not json"));
    }

    [Fact]
    public void Parse_RejectsMissingTasks()
    {
        var parser = new PlanningResponseParser();

        Assert.Throws<InvalidOperationException>(() => parser.Parse("{}"));
    }

    [Fact]
    public void Parse_RejectsEmptyTasks()
    {
        var parser = new PlanningResponseParser();

        Assert.Throws<InvalidOperationException>(() => parser.Parse("""{ "tasks": [] }"""));
    }

    [Theory]
    [InlineData("id")]
    [InlineData("title")]
    [InlineData("description")]
    public void Parse_RejectsBlankTaskFields(string propertyName)
    {
        var parser = new PlanningResponseParser();
        var json = ValidJson.Replace($"\"{propertyName}\": \"T001\"", $"\"{propertyName}\": \"\"");
        json = json.Replace($"\"{propertyName}\": \"Create skeleton\"", $"\"{propertyName}\": \"\"");
        json = json.Replace($"\"{propertyName}\": \"Create the first implementation skeleton.\"", $"\"{propertyName}\": \"\"");

        Assert.Throws<InvalidOperationException>(() => parser.Parse(json));
    }

    [Fact]
    public void Parse_RejectsBlankAcceptanceCriteria()
    {
        var parser = new PlanningResponseParser();
        var json = ValidJson.Replace("\"Tests pass\"", "\"\"");

        Assert.Throws<InvalidOperationException>(() => parser.Parse(json));
    }

    private const string ValidJson = """
        {
          "tasks": [
            {
              "id": "T001",
              "title": "Create skeleton",
              "description": "Create the first implementation skeleton.",
              "acceptanceCriteria": [
                "Build passes",
                "Tests pass"
              ]
            }
          ]
        }
        """;
}
