namespace TakomiCode.Application.Contracts.Runtime;

public class CodexRunResult
{
    public string RunId { get; set; } = string.Empty;
    public CodexRuntimeState FinalState { get; set; }
    public int? ExitCode { get; set; }
    public string? ErrorMessage { get; set; }
}
