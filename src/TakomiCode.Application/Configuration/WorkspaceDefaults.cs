using System;

namespace TakomiCode.Application.Configuration;

public static class WorkspaceDefaults
{
    public const string DefaultWorkspaceId = "workspace-default";
    private const string WorkspaceIdEnvironmentVariable = "TAKOMI_WORKSPACE_ID";

    public static string ResolveWorkspaceId()
    {
        var configuredWorkspaceId = Environment.GetEnvironmentVariable(WorkspaceIdEnvironmentVariable);
        return string.IsNullOrWhiteSpace(configuredWorkspaceId)
            ? DefaultWorkspaceId
            : configuredWorkspaceId.Trim();
    }
}
