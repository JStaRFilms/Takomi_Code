using System;
using System.Threading;
using System.Threading.Tasks;

namespace TakomiCode.Application.Contracts.Runtime;

public interface ICodexRuntimeAdapter
{
    event EventHandler<CodexRuntimeStateEventArgs>? StateChanged;
    event EventHandler<CodexRuntimeOutputEventArgs>? OutputReceived;

    Task<CodexRunResult> StartRunAsync(CodexRunRequest request, CancellationToken cancellationToken = default);
    Task CancelRunAsync(string runId, CancellationToken cancellationToken = default);
}
