using System.Text.Json;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Domain.Entities;

namespace TakomiCode.Infrastructure.Persistence;

public class LocalChatSessionRepository : IChatSessionRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;
    private Dictionary<string, ChatSession>? _sessions;

    public LocalChatSessionRepository()
    {
        var baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TakomiCode");

        Directory.CreateDirectory(baseDirectory);
        _storePath = Path.Combine(baseDirectory, "chat-sessions.json");
    }

    public async Task<ChatSession?> GetSessionAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        _sessions!.TryGetValue(id, out var session);
        return Clone(session);
    }

    public async Task<IEnumerable<ChatSession>> GetSessionsByWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        return _sessions!.Values
            .Where(s => s.WorkspaceId == workspaceId)
            .OrderByDescending(s => s.UpdatedAt)
            .Select(Clone)
            .Where(s => s is not null)!
            .Cast<ChatSession>()
            .ToList();
    }

    public async Task<IEnumerable<ChatSession>> GetChildSessionsAsync(string parentSessionId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        return _sessions!.Values
            .Where(s => s.ParentSessionId == parentSessionId)
            .OrderBy(s => s.CreatedAt)
            .Select(Clone)
            .Where(s => s is not null)!
            .Cast<ChatSession>()
            .ToList();
    }

    public async Task SaveSessionAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        _sessions![session.Id] = Clone(session)!;
        await PersistAsync(cancellationToken);
    }

    public async Task DeleteSessionAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        _sessions!.Remove(id);
        await PersistAsync(cancellationToken);
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_sessions is not null)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_sessions is not null)
            {
                return;
            }

            if (!File.Exists(_storePath))
            {
                _sessions = new Dictionary<string, ChatSession>();
                return;
            }

            _sessions = await LoadSessionsAsync(cancellationToken);
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
            var json = JsonSerializer.Serialize(_sessions, SerializerOptions);
            await File.WriteAllTextAsync(_storePath, json, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<string, ChatSession>> LoadSessionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var json = await File.ReadAllTextAsync(_storePath, cancellationToken);
            return JsonSerializer.Deserialize<Dictionary<string, ChatSession>>(json, SerializerOptions)
                ?? new Dictionary<string, ChatSession>();
        }
        catch (JsonException)
        {
            var corruptStorePath = Path.Combine(
                Path.GetDirectoryName(_storePath)!,
                $"chat-sessions.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json");

            File.Move(_storePath, corruptStorePath, overwrite: true);
            return new Dictionary<string, ChatSession>();
        }
    }

    private static ChatSession? Clone(ChatSession? session)
    {
        if (session is null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(session, SerializerOptions);
        return JsonSerializer.Deserialize<ChatSession>(json, SerializerOptions);
    }
}
