namespace Praxis;

public partial class App : Application
{
    private readonly MainPage mainPage;
    private static readonly string StartupLogPath = Services.AppStoragePaths.StartupLogPath;
    public static event Action<string>? ThemeShortcutRequested;
#if WINDOWS
    private static readonly Microsoft.UI.Xaml.Input.KeyEventHandler WindowRootKeyDownHandler = NativeRootOnKeyDown;
#endif

    public App(MainPage mainPage)
    {
        WriteStartupLog("App ctor begin");
        InitializeComponent();
        this.mainPage = mainPage;
        WriteStartupLog("App ctor end");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        WriteStartupLog("CreateWindow begin");
        var window = new Window(mainPage)
        {
            Width = 1000,
            Height = 700,
            Title = "Praxis",
        };

#if WINDOWS
        window.HandlerChanged += (_, _) =>
        {
            try
            {
                if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                {
                    if (nativeWindow.Content is Microsoft.UI.Xaml.UIElement rootElement)
                    {
                        rootElement.RemoveHandler(Microsoft.UI.Xaml.UIElement.KeyDownEvent, WindowRootKeyDownHandler);
                        rootElement.AddHandler(Microsoft.UI.Xaml.UIElement.KeyDownEvent, WindowRootKeyDownHandler, true);
                    }
                    nativeWindow.Activate();
                    WriteStartupLog("Window activated");
                }
                else
                {
                    WriteStartupLog("Window handler exists but native window is null");
                }
            }
            catch (Exception ex)
            {
                WriteStartupLog($"Window activate error: {ex.Message}");
            }
        };
#endif

        WriteStartupLog("CreateWindow end");
        return window;
    }

#if WINDOWS
    private static void NativeRootOnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        var ctrlDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(global::Windows.System.VirtualKey.Control)
            .HasFlag(global::Windows.UI.Core.CoreVirtualKeyStates.Down);
        var shiftDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(global::Windows.System.VirtualKey.Shift)
            .HasFlag(global::Windows.UI.Core.CoreVirtualKeyStates.Down);
        if (!ctrlDown || !shiftDown)
        {
            return;
        }

        if (e.Key == global::Windows.System.VirtualKey.L)
        {
            RaiseThemeShortcut("Light");
            e.Handled = true;
            return;
        }

        if (e.Key == global::Windows.System.VirtualKey.D)
        {
            RaiseThemeShortcut("Dark");
            e.Handled = true;
            return;
        }

        if (e.Key == global::Windows.System.VirtualKey.H)
        {
            RaiseThemeShortcut("System");
            e.Handled = true;
        }
    }
#endif

    public static void WriteStartupLog(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StartupLogPath)!);
            File.AppendAllText(StartupLogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    public static void RaiseThemeShortcut(string mode)
    {
        try
        {
            ThemeShortcutRequested?.Invoke(mode);
        }
        catch
        {
        }
    }
}
