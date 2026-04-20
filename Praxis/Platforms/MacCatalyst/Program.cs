using System.Diagnostics;
using Foundation;
using ObjCRuntime;
using Praxis.Services;
using UIKit;

namespace Praxis;

public class Program
{
    private const string OpenRelayArg = "--praxis-open-relay";

    // This is the main entry point of the application.
    static void Main(string[] args)
    {
        if (ShouldRelaunchViaOpen(args) && TryRelaunchViaOpen())
        {
            return;
        }

        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }

    private static bool ShouldRelaunchViaOpen(string[] args)
    {
        foreach (var arg in args)
        {
            if (string.Equals(arg, OpenRelayArg, StringComparison.Ordinal))
            {
                return false;
            }

            // LaunchServices/Finder launch usually includes a process serial number token.
            if (arg.StartsWith("-psn_", StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryRelaunchViaOpen()
    {
        var bundlePath = NSBundle.MainBundle.BundlePath;
        const string relayExecutable = "/usr/bin/open";
        if (string.IsNullOrWhiteSpace(bundlePath) ||
            !bundlePath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var normalizedBundlePath = NormalizePathForLog(bundlePath);
            var normalizedRelayExecutable = NormalizePathForLog(relayExecutable);
            var startInfo = new ProcessStartInfo(relayExecutable)
            {
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add(bundlePath);
            startInfo.ArgumentList.Add("--args");
            startInfo.ArgumentList.Add(OpenRelayArg);
            var process = Process.Start(startInfo);
            if (process is null)
            {
                CrashFileLogger.WriteWarning(nameof(Program), $"LaunchServices relay returned no process for bundle '{normalizedBundlePath}' via '{normalizedRelayExecutable}'.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            var normalizedBundlePath = NormalizePathForLog(bundlePath);
            var normalizedRelayExecutable = NormalizePathForLog(relayExecutable);
            var normalizedRelayArg = NormalizePathForLog(OpenRelayArg);
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(Program), $"LaunchServices relay failed for bundle '{normalizedBundlePath}' via '{normalizedRelayExecutable}' with relayArg='{normalizedRelayArg}': {safeMessage}");
            return false;
        }
    }

    private static string NormalizePathForLog(string path)
        => CrashFileLogger.NormalizeMessagePayload(path);
}
