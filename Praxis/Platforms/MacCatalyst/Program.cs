using System.Diagnostics;
using Foundation;
using ObjCRuntime;
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
		if (string.IsNullOrWhiteSpace(bundlePath) ||
		    !bundlePath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		try
		{
			var startInfo = new ProcessStartInfo("/usr/bin/open")
			{
				UseShellExecute = false,
			};
			startInfo.ArgumentList.Add(bundlePath);
			startInfo.ArgumentList.Add("--args");
			startInfo.ArgumentList.Add(OpenRelayArg);
			Process.Start(startInfo);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
