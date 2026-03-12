using System.Text.Json;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Domain.Entities;

namespace TakomiCode.Infrastructure.Persistence;

public class LocalWorkspaceRepository : IWorkspaceRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;
    private Dictionary<string, Workspace>? _workspaces;

    public LocalWorkspaceRepository()
    {
        var baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TakomiCode");

        Directory.CreateDirectory(baseDirectory);
        _storePath = Path.Combine(baseDirectory, "workspaces.json");
    }

    public async Task<IEnumerable<Workspace>> GetAllWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        return _workspaces!.Values
            .Select(Clone)
            .Where(workspace => workspace is not null)!
            .Cast<Workspace>()
            .ToList();
    }

    public async Task<Workspace?> GetWorkspaceAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        _workspaces!.TryGetValue(id, out var workspace);
        return Clone(workspace);
    }

    public async Task SaveWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        _workspaces![workspace.Id] = Clone(workspace)!;
        await PersistAsync(cancellationToken);
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_workspaces is not null)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_workspaces is not null)
            {
                return;
            }

            if (!File.Exists(_storePath))
            {
                _workspaces = new Dictionary<string, Workspace>();
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_storePath, cancellationToken);
                _workspaces = JsonSerializer.Deserialize<Dictionary<string, Workspace>>(json, SerializerOptions)
                    ?? new Dictionary<string, Workspace>();
            }
            catch (JsonException)
            {
                var corruptStorePath = Path.Combine(
                    Path.GetDirectoryName(_storePath)!,
                    $"workspaces.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json");

                File.Move(_storePath, corruptStorePath, overwrite: true);
                _workspaces = new Dictionary<string, Workspace>();
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
            var json = JsonSerializer.Serialize(_workspaces, SerializerOptions);
            await File.WriteAllTextAsync(_storePath, json, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static Workspace? Clone(Workspace? workspace)
    {
        if (workspace is null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(workspace, SerializerOptions);
        return JsonSerializer.Deserialize<Workspace>(json, SerializerOptions);
    }
}
