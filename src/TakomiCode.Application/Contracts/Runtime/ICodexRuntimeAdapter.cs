namespace TakomiCode.Application.Contracts.Runtime;

public interface ICodexRuntimeAdapter
{
    Task ExecuteCommandAsync(string command, CancellationToken cancellationToken = default);
    Task<string> GetStatusAsync(CancellationToken cancellationToken = default);
}
