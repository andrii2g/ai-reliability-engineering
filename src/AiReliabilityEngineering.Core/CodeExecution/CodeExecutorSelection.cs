namespace AiReliabilityEngineering.Core.CodeExecution;

public sealed record CodeExecutorSelection
{
    public CodeExecutorSelection(CodeExecutorKind kind)
    {
        Kind = kind;
    }

    public CodeExecutorKind Kind { get; }

    public static CodeExecutorSelection Fake { get; } = new(CodeExecutorKind.Fake);
}
