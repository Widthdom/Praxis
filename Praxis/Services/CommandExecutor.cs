using System.Diagnostics;
using Praxis.Core.Logic;

namespace Praxis.Services;

public sealed class CommandExecutor : ICommandExecutor
{
    public Task<(bool Success, string Message)> ExecuteAsync(string tool, string arguments, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(tool))
        {
            return RunProcess(tool, arguments);
        }

        return OpenWithDefaultHandler(arguments);
    }

    private static Task<(bool Success, string Message)> RunProcess(string tool, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = tool,
                Arguments = arguments,
                UseShellExecute = true,
            };

            Process.Start(psi);
            return Task.FromResult((true, "Executed."));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message));
        }
    }

    private static Task<(bool Success, string Message)> OpenHttpUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });

            return Task.FromResult((true, "Opened in browser."));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message));
        }
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
                Process.Start(new ProcessStartInfo
                {
                    FileName = expanded,
                    UseShellExecute = true,
                });
                return Task.FromResult((true, "Opened with default app."));
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

                Process.Start(psi);
                return Task.FromResult((true, "Opened folder."));
            }

            return Task.FromResult((false, $"Path not found: {expanded}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message));
        }
    }

    private static string ExpandHomePath(string value)
    {
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
}
