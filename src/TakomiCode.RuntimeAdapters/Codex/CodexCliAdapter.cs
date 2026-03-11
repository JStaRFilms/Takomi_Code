using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TakomiCode.Application.Contracts.Runtime;

namespace TakomiCode.RuntimeAdapters.Codex;

public class CodexCliAdapter : ICodexRuntimeAdapter
{
    public event EventHandler<CodexRuntimeStateEventArgs>? StateChanged;
    public event EventHandler<CodexRuntimeOutputEventArgs>? OutputReceived;

    private readonly ConcurrentDictionary<string, Process> _runningProcesses = new();

    public async Task<CodexRunResult> StartRunAsync(CodexRunRequest request, CancellationToken cancellationToken = default)
    {
        StateChanged?.Invoke(this, new CodexRuntimeStateEventArgs(request.RunId, CodexRuntimeState.Starting));

        try
        {
            // Ensure codex cli is available
            bool isInstalled = await CheckCodexInstalledAsync();
            if (!isInstalled)
            {
                StateChanged?.Invoke(this, new CodexRuntimeStateEventArgs(request.RunId, CodexRuntimeState.Failed, "Codex CLI is not installed or not in PATH"));
                return new CodexRunResult 
                { 
                    RunId = request.RunId, 
                    FinalState = CodexRuntimeState.Failed, 
                    ErrorMessage = "Missing CLI" 
                };
            }

            var startInfo = new ProcessStartInfo
            {
                // We use cmd.exe as a fallback / Windows-specific wrapper to locate codex via PATH or local aliases
                FileName = "cmd.exe",
                Arguments = $"/c codex {request.Command}",
                WorkingDirectory = request.WorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var envVar in request.EnvironmentVariables)
            {
                startInfo.Environment[envVar.Key] = envVar.Value;
            }

            var process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(request.RunId, e.Data, false));
                    // Basic check for auth issues in output
                    if (e.Data.Contains("Authentication required", StringComparison.OrdinalIgnoreCase) || 
                        e.Data.Contains("Not logged in", StringComparison.OrdinalIgnoreCase))
                    {
                        StateChanged?.Invoke(this, new CodexRuntimeStateEventArgs(request.RunId, CodexRuntimeState.Failed, "Authentication Issue detected in output"));
                    }
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    OutputReceived?.Invoke(this, new CodexRuntimeOutputEventArgs(request.RunId, e.Data, true));
                }
            };

            StateChanged?.Invoke(this, new CodexRuntimeStateEventArgs(request.RunId, CodexRuntimeState.Running));
            
            bool started = process.Start();
            if (!started)
            {
                throw new InvalidOperationException("Failed to start process.");
            }

            _runningProcesses[request.RunId] = process;
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            _runningProcesses.TryRemove(request.RunId, out _);

            var finalState = process.ExitCode == 0 ? CodexRuntimeState.Completed : CodexRuntimeState.Failed;
            StateChanged?.Invoke(this, new CodexRuntimeStateEventArgs(request.RunId, finalState, $"Process exited with code {process.ExitCode}"));

            return new CodexRunResult
            {
                RunId = request.RunId,
                FinalState = finalState,
                ExitCode = process.ExitCode
            };
        }
        catch (OperationCanceledException)
        {
            StateChanged?.Invoke(this, new CodexRuntimeStateEventArgs(request.RunId, CodexRuntimeState.Cancelled));
            return new CodexRunResult
            {
                RunId = request.RunId,
                FinalState = CodexRuntimeState.Cancelled
            };
        }
        catch (Exception ex)
        {
            StateChanged?.Invoke(this, new CodexRuntimeStateEventArgs(request.RunId, CodexRuntimeState.Failed, ex.Message));
            return new CodexRunResult
            {
                RunId = request.RunId,
                FinalState = CodexRuntimeState.Failed,
                ErrorMessage = ex.Message
            };
        }
    }

    public Task CancelRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (_runningProcesses.TryGetValue(runId, out var process) && !process.HasExited)
        {
            try
            {
                process.Kill(entireProcessTree: true); 
                StateChanged?.Invoke(this, new CodexRuntimeStateEventArgs(runId, CodexRuntimeState.Cancelled));
            }
            catch (Exception ex)
            {
                StateChanged?.Invoke(this, new CodexRuntimeStateEventArgs(runId, CodexRuntimeState.Failed, $"Cancel failed: {ex.Message}"));
            }
        }
        
        return Task.CompletedTask;
    }

    private async Task<bool> CheckCodexInstalledAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c codex --version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
