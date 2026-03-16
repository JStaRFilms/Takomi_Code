using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Application.Configuration;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Application.Contracts.Runtime;
using TakomiCode.Domain.Entities;

namespace TakomiCode.RuntimeAdapters.Codex;

public class WorkspaceAwareCodexRuntimeAdapter : ICodexRuntimeAdapter
{
    private readonly CodexSdkAdapter _localAdapter;
    private readonly CodexCloudAdapter _cloudAdapter;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly string _defaultWorkspaceId;
    private readonly ConcurrentDictionary<string, string> _runTargets = new();

    public event EventHandler<CodexRuntimeStateEventArgs>? StateChanged;
    public event EventHandler<CodexRuntimeOutputEventArgs>? OutputReceived;

    public WorkspaceAwareCodexRuntimeAdapter(
        CodexSdkAdapter localAdapter,
        CodexCloudAdapter cloudAdapter,
        IWorkspaceRepository workspaceRepository)
    {
        _localAdapter = localAdapter;
        _cloudAdapter = cloudAdapter;
        _workspaceRepository = workspaceRepository;
        _defaultWorkspaceId = WorkspaceDefaults.ResolveWorkspaceId();

        _localAdapter.StateChanged += (s, e) => StateChanged?.Invoke(this, e);
        _localAdapter.OutputReceived += (s, e) => OutputReceived?.Invoke(this, e);

        _cloudAdapter.StateChanged += (s, e) => StateChanged?.Invoke(this, e);
        _cloudAdapter.OutputReceived += (s, e) => OutputReceived?.Invoke(this, e);
    }

    private async Task<ICodexRuntimeAdapter> GetAdapterAsync(string? workspaceId, CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(workspaceId) ? _defaultWorkspaceId : workspaceId;
        var workspace = await _workspaceRepository.GetWorkspaceAsync(id, cancellationToken);
        if (workspace != null && string.Equals(workspace.RuntimeTarget, "Cloud", StringComparison.OrdinalIgnoreCase))
        {
            return _cloudAdapter;
        }

        return _localAdapter;
    }

    public async Task<CodexRunResult> StartRunAsync(CodexRunRequest request, CancellationToken cancellationToken = default)
    {
        var adapter = await GetAdapterAsync(request.WorkspaceId, cancellationToken);
        _runTargets[request.RunId] = ReferenceEquals(adapter, _cloudAdapter) ? "Cloud" : "Local";
        var result = await adapter.StartRunAsync(request, cancellationToken);

        _runTargets.TryRemove(request.RunId, out _);
        return result;
    }

    public async Task CancelRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (_runTargets.TryGetValue(runId, out var runtimeTarget))
        {
            await GetAdapterForTarget(runtimeTarget).CancelRunAsync(runId, cancellationToken);
            return;
        }

        var adapter = await GetAdapterAsync(_defaultWorkspaceId, cancellationToken);
        await adapter.CancelRunAsync(runId, cancellationToken);
    }

    public async Task SendInterventionAsync(string runId, InterventionAction action, string? payload = null, CancellationToken cancellationToken = default)
    {
        if (_runTargets.TryGetValue(runId, out var runtimeTarget))
        {
            await GetAdapterForTarget(runtimeTarget).SendInterventionAsync(runId, action, payload, cancellationToken);
            return;
        }

        var adapter = await GetAdapterAsync(_defaultWorkspaceId, cancellationToken);
        await adapter.SendInterventionAsync(runId, action, payload, cancellationToken);
    }

    private ICodexRuntimeAdapter GetAdapterForTarget(string runtimeTarget)
    {
        return string.Equals(runtimeTarget, "Cloud", StringComparison.OrdinalIgnoreCase)
            ? _cloudAdapter
            : _localAdapter;
    }
}
