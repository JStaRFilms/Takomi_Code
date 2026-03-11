using System.Text.Json;
using TakomiCode.Application.Contracts.Persistence;
using TakomiCode.Domain.Events;

namespace TakomiCode.Infrastructure.Persistence;

public class LocalAuditLogRepository : IAuditLogRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;
    private List<AuditEvent>? _events;

    public LocalAuditLogRepository()
    {
        var baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TakomiCode");

        Directory.CreateDirectory(baseDirectory);
        _storePath = Path.Combine(baseDirectory, "audit-events.json");
    }

    public async Task AppendEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        _events!.Add(Clone(auditEvent)!);
        await PersistAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditEvent>> GetEventsBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _events!
            .Where(e => e.SessionId == sessionId)
            .Select(Clone)
            .Where(e => e is not null)!
            .Cast<AuditEvent>()
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_events is not null)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_events is not null)
            {
                return;
            }

            if (!File.Exists(_storePath))
            {
                _events = new List<AuditEvent>();
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_storePath, cancellationToken);
                _events = JsonSerializer.Deserialize<List<AuditEvent>>(json, SerializerOptions)
                    ?? new List<AuditEvent>();
            }
            catch (JsonException)
            {
                var corruptStorePath = Path.Combine(
                    Path.GetDirectoryName(_storePath)!,
                    $"audit-events.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json");

                File.Move(_storePath, corruptStorePath, overwrite: true);
                _events = new List<AuditEvent>();
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
            var json = JsonSerializer.Serialize(_events, SerializerOptions);
            await File.WriteAllTextAsync(_storePath, json, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static AuditEvent? Clone(AuditEvent? auditEvent)
    {
        if (auditEvent is null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(auditEvent, SerializerOptions);
        return JsonSerializer.Deserialize<AuditEvent>(json, SerializerOptions);
    }
}
