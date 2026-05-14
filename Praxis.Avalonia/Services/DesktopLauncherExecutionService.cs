using System.Diagnostics;
using Praxis.Core.Models;
using Praxis.Core.Services;

namespace Praxis.Avalonia.Services;

internal sealed class DesktopLauncherExecutionService : ILauncherExecutionService
{
    public Task<LauncherExecutionResult> ExecuteAsync(
        LauncherButtonModel button,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(LauncherExecutionResult.Failure("Canceled."));
        }

        try
        {
            var tool = Normalize(button.CommandPath);
            if (!string.IsNullOrWhiteSpace(tool))
            {
                return Task.FromResult(Start(
                    new ProcessStartInfo
                    {
                        FileName = ExpandHome(tool),
                        Arguments = button.Arguments,
                        UseShellExecute = true,
                    },
                    "Executed."));
            }

            var target = ExpandHome(Normalize(button.Arguments));
            if (string.IsNullOrWhiteSpace(target))
            {
                return Task.FromResult(LauncherExecutionResult.Failure("No command target."));
            }

            return Task.FromResult(OpenTarget(target));
        }
        catch (Exception ex)
        {
            return Task.FromResult(LauncherExecutionResult.Failure($"Launch failed: {ex.Message}"));
        }
    }

    private static LauncherExecutionResult OpenTarget(string target)
    {
        if (Uri.TryCreate(target, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return Start(new ProcessStartInfo { FileName = target, UseShellExecute = true }, "Opened URL.");
        }

        if (Uri.TryCreate(target, UriKind.Absolute, out uri) && uri.IsFile)
        {
            target = uri.LocalPath;
        }

        if (File.Exists(target))
        {
            return Start(new ProcessStartInfo { FileName = target, UseShellExecute = true }, "Opened file.");
        }

        if (Directory.Exists(target))
        {
            if (OperatingSystem.IsWindows())
            {
                return Start(
                    new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{target}\"",
                        UseShellExecute = true,
                    },
                    "Opened folder.");
            }

            if (OperatingSystem.IsMacOS())
            {
                return Start(
                    new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"\"{target}\"",
                        UseShellExecute = true,
                    },
                    "Opened folder.");
            }

            return Start(
                new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{target}\"",
                    UseShellExecute = false,
                },
                "Opened folder.");
        }

        return LauncherExecutionResult.Failure($"Path not found: {target}");
    }

    private static LauncherExecutionResult Start(ProcessStartInfo startInfo, string successMessage)
    {
        using var process = Process.Start(startInfo);
        return process is null
            ? LauncherExecutionResult.Failure($"No process was started for {startInfo.FileName}.")
            : LauncherExecutionResult.Success(successMessage);
    }

    private static string Normalize(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length >= 2
            && ((trimmed[0] == '"' && trimmed[^1] == '"')
                || (trimmed[0] == '\'' && trimmed[^1] == '\'')))
        {
            trimmed = trimmed[1..^1].Trim();
        }

        return Environment.ExpandEnvironmentVariables(trimmed);
    }

    private static string ExpandHome(string value)
    {
        if (string.Equals(value, "~", StringComparison.Ordinal))
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (!value.StartsWith("~/", StringComparison.Ordinal)
            && !value.StartsWith("~\\", StringComparison.Ordinal))
        {
            return value;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(home) ? value : Path.Combine(home, value[2..]);
    }
}
