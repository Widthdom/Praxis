using Microsoft.UI.Xaml;
using Praxis.Services;
using System.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Praxis.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	private static readonly object LogLock = new();
	private static bool globalExceptionLoggingHooked;
	private static readonly string StartupLogPath = Path.Combine(
		AppStoragePaths.WindowsLocalAppDataRoot,
		"Praxis",
		"startup.log");

	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		var startupLogDirectory = Path.GetDirectoryName(StartupLogPath);
		if (!string.IsNullOrWhiteSpace(startupLogDirectory))
		{
			try
			{
				Directory.CreateDirectory(startupLogDirectory);
			}
			catch (Exception ex)
			{
				SecondaryFailureLogger.ReportStartupLogFailure(
					nameof(App),
					startupLogDirectory,
					"Failed to create startup log directory",
					ex);
			}
		}

		HookGlobalExceptionLogging();
		this.InitializeComponent();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	private void HookGlobalExceptionLogging()
	{
		if (globalExceptionLoggingHooked)
		{
			return;
		}

		globalExceptionLoggingHooked = true;
		this.UnhandledException += (_, e) =>
		{
			CrashFileLogger.WriteException("WinUI.UnhandledException", e.Exception);
			WriteStartupLog("WinUI.UnhandledException", e.Exception);
		};

		AppDomain.CurrentDomain.UnhandledException += (_, e) =>
		{
			if (e.ExceptionObject is Exception exception)
			{
				CrashFileLogger.WriteException(
					$"Win.AppDomain.UnhandledException (IsTerminating={e.IsTerminating})",
					exception);
				WriteStartupLog("AppDomain.UnhandledException", exception);
			}
			else
			{
				var payload = $"Non-Exception object thrown (IsTerminating={e.IsTerminating}): {e.ExceptionObject}";
				CrashFileLogger.WriteWarning("Win.AppDomain.UnhandledException", payload);
				WriteStartupLog("AppDomain.UnhandledException", payload);
			}
		};

		TaskScheduler.UnobservedTaskException += (_, e) =>
		{
			CrashFileLogger.WriteException("Win.TaskScheduler.UnobservedTaskException", e.Exception);
			WriteStartupLog("TaskScheduler.UnobservedTaskException", e.Exception);
			e.SetObserved();
		};
	}

	private static void WriteStartupLog(string source, Exception? exception)
	{
		try
		{
			var content = BuildStartupExceptionLogContent(source, exception);

			try
			{
				AppendStartupLogContent(content);
			}
			catch (Exception ex)
			{
				SecondaryFailureLogger.ReportStartupLogFailure(
					nameof(App),
					StartupLogPath,
					"Failed to append startup log",
					ex,
					exception);
			}
		}
		catch (Exception ex)
		{
			SecondaryFailureLogger.ReportStartupLogFailure(
				nameof(App),
				StartupLogPath,
				"Failed to build startup log payload for",
				ex,
				exception);
		}
	}

	private static void WriteStartupLog(string source, string message)
	{
		try
		{
			var content = BuildStartupMessageLogContent(source, message);

			try
			{
				AppendStartupLogContent(content);
			}
			catch (Exception ex)
			{
				SecondaryFailureLogger.ReportStartupLogFailure(
					nameof(App),
					StartupLogPath,
					"Failed to append startup log",
					ex,
					originalMessage: message);
			}
		}
		catch (Exception ex)
		{
			SecondaryFailureLogger.ReportStartupLogFailure(
				nameof(App),
				StartupLogPath,
				"Failed to build startup log payload for",
				ex,
				originalMessage: message);
		}
	}

	private static string BuildStartupExceptionLogContent(string source, Exception? exception)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] {source}");
		sb.Append(CrashFileLogger.FormatExceptionPayload(exception));
		sb.AppendLine(new string('-', 80));
		return sb.ToString();
	}

	private static string BuildStartupMessageLogContent(string source, string message)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] {source}");
		sb.AppendLine(message);
		sb.AppendLine(new string('-', 80));
		return sb.ToString();
	}

	private static void AppendStartupLogContent(string content)
	{
		lock (LogLock)
		{
			File.AppendAllText(StartupLogPath, content, Encoding.UTF8);
		}
	}
}
