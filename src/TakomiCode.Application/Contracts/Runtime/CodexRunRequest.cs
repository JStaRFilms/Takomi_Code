using System.Collections.Generic;

namespace TakomiCode.Application.Contracts.Runtime;

public class CodexRunRequest
{
    public string RunId { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string? WorkspaceId { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}
