using AiReliabilityEngineering.Core.Git;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class GitStatusParser
{
    public IReadOnlyList<GitStatusEntry> Parse(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        return output
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseLine)
            .Where(entry => entry is not null)
            .Cast<GitStatusEntry>()
            .ToArray();
    }

    private static GitStatusEntry? ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || line.Length < 4)
        {
            return null;
        }

        var status = line[..2].Trim();
        var path = line[3..].Trim();
        if (string.IsNullOrWhiteSpace(status) || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var arrowIndex = path.IndexOf(" -> ", StringComparison.Ordinal);
        if (arrowIndex >= 0)
        {
            path = path[(arrowIndex + 4)..];
        }

        return new GitStatusEntry(status, path);
    }
}
