namespace AiReliabilityEngineering.Core.CodeExecution;

public sealed record CodeExecutionRequest
{
    public CodeExecutionRequest(
        string workspaceDirectory,
        string promptFilePath,
        TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(workspaceDirectory))
        {
            throw new ArgumentException("Workspace directory is required.", nameof(workspaceDirectory));
        }

        if (string.IsNullOrWhiteSpace(promptFilePath))
        {
            throw new ArgumentException("Prompt file path is required.", nameof(promptFilePath));
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        WorkspaceDirectory = workspaceDirectory;
        PromptFilePath = promptFilePath;
        Timeout = timeout;
    }

    public string WorkspaceDirectory { get; }

    public string PromptFilePath { get; }

    public TimeSpan Timeout { get; }
}
