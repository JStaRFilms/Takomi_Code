namespace TakomiCode.Domain.Entities;

public class GitState
{
    public string BranchName { get; set; } = string.Empty;
    public bool IsDirty { get; set; }
}
