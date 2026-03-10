using TakomiCode.Application.Contracts.Runtime;

namespace TakomiCode.RuntimeAdapters.Codex;

public class CodexCliAdapter : ICodexRuntimeAdapter
{
    public Task ExecuteCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        // Placeholder for executing commands via Codex CLI
        return Task.CompletedTask;
    }

    public Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder for fetching runtime status
        return Task.FromResult("Idle");
    }
}
