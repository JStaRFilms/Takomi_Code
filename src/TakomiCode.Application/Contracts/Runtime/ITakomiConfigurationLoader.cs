using TakomiCode.Domain.Entities;

namespace TakomiCode.Application.Contracts.Runtime;

public record ModeLoadResult(
    IReadOnlyList<TakomiMode> Modes,
    IReadOnlyList<LoadDiagnostic> Diagnostics
);

public record WorkflowLoadResult(
    IReadOnlyList<TakomiWorkflow> Workflows,
    IReadOnlyList<LoadDiagnostic> Diagnostics
);

public record LoadDiagnostic(
    string FilePath,
    string ErrorMessage,
    Exception? Exception = null
);

public interface ITakomiConfigurationLoader
{
    Task<ModeLoadResult> LoadModesAsync(string workspaceRoot, CancellationToken cancellationToken = default);
    Task<WorkflowLoadResult> LoadWorkflowsAsync(string workspaceRoot, CancellationToken cancellationToken = default);
}
