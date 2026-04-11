using System.Diagnostics;
using Praxis.Core.Logic;

namespace Praxis.Services;

public sealed class CommandExecutor : ICommandExecutor
{
    public Task<(bool Success, string Message)> ExecuteAsync(string tool, string arguments, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult((false, "Canceled."));
        }

        var normalizedTool = ExpandHomePath(NormalizeToolPath(tool));
        if (HasUsableTool(normalizedTool))
        {
            return RunProcess(normalizedTool, arguments);
        }

        return OpenWithDefaultHandler(arguments);
    }

    private static Task<(bool Success, string Message)> RunProcess(string tool, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = tool,
            Arguments = arguments,
            UseShellExecute = true,
        };

        ApplyWorkingDirectoryOverride(psi, tool);
        return Task.FromResult(StartProcess(psi, "Executed.", $"Process launch failed for tool '{tool}'."));
    }

    private static Task<(bool Success, string Message)> OpenHttpUrl(string url)
    {
        return Task.FromResult(StartProcess(
            new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            },
            "Opened in browser.",
            $"Failed to open URL '{url}'."));
    }

    private static Task<(bool Success, string Message)> OpenWithDefaultHandler(string arguments)
    {
        try
        {
            var resolved = LaunchTargetResolver.Resolve(arguments);
            if (resolved.Kind == LaunchTargetKind.HttpUrl)
            {
                return OpenHttpUrl(resolved.Target);
            }

            if (resolved.Kind != LaunchTargetKind.FileSystemPath)
            {
                return Task.FromResult((false, "Tool is empty, and Arguments is not a valid HTTP(S) URL or filesystem path."));
            }

            var expanded = ExpandHomePath(resolved.Target);

            // Absolute file URI is also supported.
            if (Uri.TryCreate(expanded, UriKind.Absolute, out var uri) && uri.IsFile)
            {
                expanded = uri.LocalPath;
            }

            if (File.Exists(expanded))
            {
                return Task.FromResult(StartProcess(
                    new ProcessStartInfo
                    {
                        FileName = expanded,
                        UseShellExecute = true,
                    },
                    "Opened with default app.",
                    $"Failed to open file '{expanded}'."));
            }

            // UNC shares can require auth before Directory.Exists/File.Exists become true.
            // Let Explorer handle auth prompt by attempting open directly.
            if (OperatingSystem.IsWindows() && WindowsPathPolicy.IsUncPath(expanded))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{expanded}\"",
                    UseShellExecute = true,
                };

                return Task.FromResult(StartProcess(
                    psi,
                    "Opening UNC path with Explorer.",
                    $"Failed to open UNC path '{expanded}'."));
            }

            if (Directory.Exists(expanded))
            {
                ProcessStartInfo psi;
                if (OperatingSystem.IsWindows())
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{expanded}\"",
                        UseShellExecute = true,
                    };
                }
                else if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"\"{expanded}\"",
                        UseShellExecute = true,
                    };
                }
                else
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = expanded,
                        UseShellExecute = true,
                    };
                }

                return Task.FromResult(StartProcess(
                    psi,
                    "Opened folder.",
                    $"Failed to open folder '{expanded}'."));
            }

            return Task.FromResult((false, $"Path not found: {expanded}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, $"Launch target resolution failed: {ex.Message}"));
        }
    }

    private static (bool Success, string Message) StartProcess(ProcessStartInfo startInfo, string successMessage, string failurePrefix)
    {
        try
        {
            var process = Process.Start(startInfo);
            if (process is null)
            {
                return (false, $"{failurePrefix} No process handle was returned.");
            }

            return (true, successMessage);
        }
        catch (Exception ex)
        {
            return (false, $"{failurePrefix} {ex.Message}");
        }
    }

    private static string ExpandHomePath(string value)
    {
        if (string.Equals(value, "~", StringComparison.Ordinal))
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (!(value.StartsWith("~/", StringComparison.Ordinal) || value.StartsWith("~\\", StringComparison.Ordinal)))
        {
            return value;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(home))
        {
            return value;
        }

        return Path.Combine(home, value[2..]);
    }

    private static void ApplyWorkingDirectoryOverride(ProcessStartInfo startInfo, string tool)
    {
        ApplyWorkingDirectoryOverride(startInfo, tool, OperatingSystem.IsWindows());
    }

    private static void ApplyWorkingDirectoryOverride(ProcessStartInfo startInfo, string tool, bool isWindows)
    {
        if (!CommandWorkingDirectoryPolicy.RequiresUserProfileWorkingDirectory(tool, isWindows))
        {
            return;
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile) || !Directory.Exists(userProfile))
        {
            return;
        }

        startInfo.WorkingDirectory = userProfile;
    }

    private static string NormalizeToolPath(string tool)
    {
        var trimmed = tool?.Trim() ?? string.Empty;
        if (trimmed.Length >= 2 &&
            ((trimmed[0] == '"' && trimmed[^1] == '"') || (trimmed[0] == '\'' && trimmed[^1] == '\'')))
        {
            return trimmed[1..^1].Trim();
        }

        return trimmed;
    }

    private static bool HasUsableTool(string tool)
        => !string.IsNullOrWhiteSpace(tool);
}
