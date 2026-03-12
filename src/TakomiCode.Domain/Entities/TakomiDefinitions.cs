namespace TakomiCode.Domain.Entities;

public record TakomiMode(
    string Slug,
    string Name,
    string IconName,
    string Description,
    string RoleDefinition,
    string WhenToUse,
    IReadOnlyList<TakomiToolGroup> Groups,
    string CustomInstructions,
    string Source,
    string FilePath
);

public record TakomiToolGroup(
    string Name,
    string? FileRegex = null,
    string? Description = null
);

public record TakomiWorkflow(
    string Slug,
    string Description,
    string Body,
    string FilePath
);
