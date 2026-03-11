using System.Text.Json;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Domain.Entities;

namespace TakomiCode.Infrastructure.Persistence;

public class LocalOrchestrationRepository : IOrchestrationRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;
    private OrchestrationStore? _store;

    public LocalOrchestrationRepository()
    {
        var baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TakomiCode");

        Directory.CreateDirectory(baseDirectory);
        _storePath = Path.Combine(baseDirectory, "orchestration-store.json");
    }

    public async Task<OrchestrationSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        if (!_store!.Sessions.TryGetValue(sessionId, out var session))
        {
            return null;
        }

        return RehydrateSession(session);
    }

    public async Task<IEnumerable<OrchestrationSession>> GetSessionsByWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _store!.Sessions.Values
            .Where(session => session.WorkspaceId == workspaceId)
            .Select(RehydrateSession)
            .Where(session => session is not null)!
            .Cast<OrchestrationSession>()
            .ToList();
    }

    public async Task SaveSessionAsync(OrchestrationSession session, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        var copy = Clone(session)!;
        copy.Tasks.Clear();
        _store!.Sessions[copy.SessionId] = copy;
        await PersistAsync(cancellationToken);
    }

    public async Task<OrchestrationTask?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        if (!_store!.Tasks.TryGetValue(taskId, out var task))
        {
            return null;
        }

        return RehydrateTask(task);
    }

    public async Task<IEnumerable<OrchestrationTask>> GetTasksForSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _store!.Tasks.Values
            .Where(task => task.SessionId == sessionId)
            .Select(RehydrateTask)
            .Where(task => task is not null)!
            .Cast<OrchestrationTask>()
            .OrderBy(task => task.CreatedAt)
            .ToList();
    }

    public async Task SaveTaskAsync(OrchestrationTask task, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        var copy = Clone(task)!;
        copy.ExecutionRuns.Clear();
        copy.Artifacts.Clear();
        _store!.Tasks[copy.Id] = copy;
        await PersistAsync(cancellationToken);
    }

    public async Task<OrchestrationRun?> GetRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        _store!.Runs.TryGetValue(runId, out var run);
        return Clone(run);
    }

    public async Task<IEnumerable<OrchestrationRun>> GetRunsForTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _store!.Runs.Values
            .Where(run => run.TaskId == taskId)
            .Select(Clone)
            .Where(run => run is not null)!
            .Cast<OrchestrationRun>()
            .OrderBy(run => run.StartedAt)
            .ToList();
    }

    public async Task SaveRunAsync(OrchestrationRun run, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        _store!.Runs[run.RunId] = Clone(run)!;
        await PersistAsync(cancellationToken);
    }

    public async Task<IEnumerable<OrchestrationRun>> GetActiveRunsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _store!.Runs.Values
            .Where(run => run.Status is TaskStatus.InProgress or TaskStatus.Queued or TaskStatus.Paused)
            .Select(Clone)
            .Where(run => run is not null)!
            .Cast<OrchestrationRun>()
            .OrderByDescending(run => run.StartedAt)
            .ToList();
    }

    public async Task<IEnumerable<TaskArtifact>> GetArtifactsForTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _store!.Artifacts.Values
            .Where(artifact => artifact.TaskId == taskId)
            .Select(Clone)
            .Where(artifact => artifact is not null)!
            .Cast<TaskArtifact>()
            .OrderBy(artifact => artifact.CreatedAt)
            .ToList();
    }

    public async Task SaveArtifactAsync(TaskArtifact artifact, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        _store!.Artifacts[artifact.ArtifactId] = Clone(artifact)!;
        await PersistAsync(cancellationToken);
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_store is not null)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_store is not null)
            {
                return;
            }

            if (!File.Exists(_storePath))
            {
                _store = new OrchestrationStore();
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_storePath, cancellationToken);
                _store = JsonSerializer.Deserialize<OrchestrationStore>(json, SerializerOptions)
                    ?? new OrchestrationStore();
            }
            catch (JsonException)
            {
                var corruptStorePath = Path.Combine(
                    Path.GetDirectoryName(_storePath)!,
                    $"orchestration-store.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json");

                File.Move(_storePath, corruptStorePath, overwrite: true);
                _store = new OrchestrationStore();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(_store, SerializerOptions);
            await File.WriteAllTextAsync(_storePath, json, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private OrchestrationSession? RehydrateSession(OrchestrationSession? session)
    {
        var copy = Clone(session);
        if (copy is null)
        {
            return null;
        }

        copy.Tasks = _store!.Tasks.Values
            .Where(task => task.SessionId == copy.SessionId)
            .Select(RehydrateTask)
            .Where(task => task is not null)!
            .Cast<OrchestrationTask>()
            .OrderBy(task => task.CreatedAt)
            .ToList();

        return copy;
    }

    private OrchestrationTask? RehydrateTask(OrchestrationTask? task)
    {
        var copy = Clone(task);
        if (copy is null)
        {
            return null;
        }

        copy.ExecutionRuns = _store!.Runs.Values
            .Where(run => run.TaskId == copy.Id)
            .Select(Clone)
            .Where(run => run is not null)!
            .Cast<OrchestrationRun>()
            .OrderBy(run => run.StartedAt)
            .ToList();

        copy.Artifacts = _store.Artifacts.Values
            .Where(artifact => artifact.TaskId == copy.Id)
            .Select(Clone)
            .Where(artifact => artifact is not null)!
            .Cast<TaskArtifact>()
            .OrderBy(artifact => artifact.CreatedAt)
            .ToList();

        return copy;
    }

    private static T? Clone<T>(T? value)
    {
        if (value is null)
        {
            return default;
        }

        var json = JsonSerializer.Serialize(value, SerializerOptions);
        return JsonSerializer.Deserialize<T>(json, SerializerOptions);
    }

    private sealed class OrchestrationStore
    {
        public Dictionary<string, OrchestrationSession> Sessions { get; set; } = new();
        public Dictionary<string, OrchestrationTask> Tasks { get; set; } = new();
        public Dictionary<string, OrchestrationRun> Runs { get; set; } = new();
        public Dictionary<string, TaskArtifact> Artifacts { get; set; } = new();
    }
}
