using System;

namespace TakomiCode.Application.Contracts.Runtime;

public class CodexRuntimeOutputEventArgs : EventArgs
{
    public string RunId { get; }
    public string Content { get; }
    public bool IsError { get; }
    public CodexRuntimeOutputKind Kind { get; }

    public CodexRuntimeOutputEventArgs(
        string runId,
        string content,
        bool isError = false,
        CodexRuntimeOutputKind kind = CodexRuntimeOutputKind.Progress)
    {
        RunId = runId;
        Content = content;
        IsError = isError;
        Kind = isError ? CodexRuntimeOutputKind.Error : kind;
    }
}
