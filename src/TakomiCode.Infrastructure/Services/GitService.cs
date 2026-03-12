using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Application.Contracts.Services;
using TakomiCode.Domain.Entities;

namespace TakomiCode.Infrastructure.Services;

public class GitService : IGitService
{
    public async Task<GitState> GetCurrentStateAsync(string workspacePath, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureWorktreeIsValidAsync(workspacePath, cancellationToken);

            var branchName = await RunCommandAsync("git", "rev-parse --abbrev-ref HEAD", workspacePath, cancellationToken);
            var statusOutput = await RunCommandAsync("git", "status --porcelain", workspacePath, cancellationToken);

            return new GitState
            {
                BranchName = string.IsNullOrWhiteSpace(branchName) ? "unknown" : branchName.Trim(),
                IsDirty = !string.IsNullOrWhiteSpace(statusOutput)
            };
        }
        catch
        {
            return new GitState
            {
                BranchName = "unknown",
                IsDirty = false
            };
        }
    }

    public async Task<string> CreateWorktreeAsync(string workspacePath, string worktreeName, string branchName, CancellationToken cancellationToken = default)
    {
        await EnsureWorktreeIsValidAsync(workspacePath, cancellationToken);

        var normalizedWorkspacePath = Path.GetFullPath(workspacePath);
        var parentDir = Path.GetDirectoryName(normalizedWorkspacePath) ?? normalizedWorkspacePath;
        var parentDirPath = Path.GetFullPath(parentDir);
        var repoName = Path.GetFileName(normalizedWorkspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var safeWorktreeName = ValidateWorktreeName(worktreeName);
        var newPath = Path.GetFullPath(Path.Combine(parentDirPath, $"{repoName}-{safeWorktreeName}"));

        EnsurePathStaysWithinParent(parentDirPath, newPath);

        if (Directory.Exists(newPath))
        {
            throw new InvalidOperationException($"Target worktree path already exists: {newPath}");
        }

        var branchExistsOutput = await RunCommandAsync(
            "git",
            $"branch --list \"{branchName}\"",
            workspacePath,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(branchExistsOutput))
        {
            await RunCommandAsync(
                "git",
                $"worktree add -b \"{branchName}\" \"{newPath}\" HEAD",
                workspacePath,
                cancellationToken);
        }
        else
        {
            await RunCommandAsync(
                "git",
                $"worktree add \"{newPath}\" \"{branchName}\"",
                workspacePath,
                cancellationToken);
        }

        await EnsureWorktreeIsValidAsync(newPath, cancellationToken);
        return newPath;
    }

    public async Task EnsureWorktreeIsValidAsync(string workspacePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            throw new InvalidOperationException("Worktree path is required.");
        }

        if (!Directory.Exists(workspacePath))
        {
            throw new InvalidOperationException($"Worktree path does not exist: {workspacePath}");
        }

        var output = await RunCommandAsync("git", "rev-parse --is-inside-work-tree", workspacePath, cancellationToken);
        if (!string.Equals(output.Trim(), "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Path is not a git worktree: {workspacePath}");
        }
    }

    private static async Task<string> RunCommandAsync(string command, string args, string workingDir, CancellationToken cancellationToken)
    {
        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        proc.Start();
        var output = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await proc.StandardError.ReadToEndAsync(cancellationToken);
        await proc.WaitForExitAsync(cancellationToken);

        if (proc.ExitCode != 0)
        {
            var detail = string.IsNullOrWhiteSpace(error) ? output : error;
            throw new InvalidOperationException(detail.Trim());
        }

        return output;
    }

    private static string ValidateWorktreeName(string worktreeName)
    {
        if (string.IsNullOrWhiteSpace(worktreeName))
        {
            throw new InvalidOperationException("Worktree name is required.");
        }

        var normalizedName = worktreeName.Trim();
        if (normalizedName is "." or "..")
        {
            throw new InvalidOperationException("Worktree name must not be a relative path segment.");
        }

        if (normalizedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidOperationException($"Worktree name contains invalid path characters: {worktreeName}");
        }

        if (normalizedName.Contains(Path.DirectorySeparatorChar) || normalizedName.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new InvalidOperationException("Worktree name must be a single path segment.");
        }

        return normalizedName;
    }

    private static void EnsurePathStaysWithinParent(string parentDirPath, string candidatePath)
    {
        var relativePath = Path.GetRelativePath(parentDirPath, candidatePath);
        var escapesParent = relativePath.Equals("..", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)
            || Path.IsPathRooted(relativePath);

        if (escapesParent)
        {
            throw new InvalidOperationException("Worktree path would escape the repository parent directory.");
        }
    }
}
