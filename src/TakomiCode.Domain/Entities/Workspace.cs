namespace TakomiCode.Domain.Entities;

public class Workspace
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? CurrentWorktreePath { get; set; }
    public bool IsAttached { get; set; }
}
