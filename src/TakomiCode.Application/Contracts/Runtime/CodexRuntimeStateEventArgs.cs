using System;

namespace TakomiCode.Application.Contracts.Runtime;

public class CodexRuntimeStateEventArgs : EventArgs
{
    public string RunId { get; }
    public CodexRuntimeState State { get; }
    public string? Details { get; }

    public CodexRuntimeStateEventArgs(string runId, CodexRuntimeState state, string? details = null)
    {
        RunId = runId;
        State = state;
        Details = details;
    }
}
