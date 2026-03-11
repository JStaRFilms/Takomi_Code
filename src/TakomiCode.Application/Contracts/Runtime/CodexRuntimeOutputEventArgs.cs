using System;

namespace TakomiCode.Application.Contracts.Runtime;

public class CodexRuntimeOutputEventArgs : EventArgs
{
    public string RunId { get; }
    public string Content { get; }
    public bool IsError { get; }

    public CodexRuntimeOutputEventArgs(string runId, string content, bool isError = false)
    {
        RunId = runId;
        Content = content;
        IsError = isError;
    }
}
