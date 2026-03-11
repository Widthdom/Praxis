using Microsoft.Extensions.DependencyInjection;
using Praxis.Core.Logic;
using Praxis.Services;

namespace Praxis;

public partial class App : Application
{
    private readonly IServiceProvider services;
    private static IErrorLogger? errorLogger;
    private Page? rootPage;
    private static volatile bool isEditorOpen;
    private static volatile bool isContextMenuOpen;
    private static volatile bool isConflictDialogOpen;
    public static event Action<string>? ThemeShortcutRequested;
    public static event Action<string>? EditorShortcutRequested;
    public static event Action<string>? CommandInputShortcutRequested;
    public static event Action<string>? HistoryShortcutRequested;
    public static event Action? MiddleMouseClickRequested;
#if MACCATALYST
    public static event Action? MacApplicationDeactivating;
    public static event Action? MacApplicationActivated;
    private static long lastActivatedAt;
    private static volatile bool isMacApplicationActive = true;
    public static void RecordActivation() => lastActivatedAt = Environment.TickCount64;
    public static bool IsActivationSuppressionActive() => Environment.TickCount64 - lastActivatedAt < UiTimingPolicy.MacActivationSuppressionWindowMs;
    public static bool IsMacApplicationActive() => isMacApplicationActive;
    public static void SetMacApplicationActive(bool value) => isMacApplicationActive = value;
    public static void RaiseMacApplicationDeactivating()
    {
        try { MacApplicationDeactivating?.Invoke(); }
        catch (Exception ex) { errorLogger?.Log(ex, nameof(RaiseMacApplicationDeactivating)); }
    }

    public static void RaiseMacApplicationActivated()
    {
        try { MacApplicationActivated?.Invoke(); }
        catch (Exception ex) { errorLogger?.Log(ex, nameof(RaiseMacApplicationActivated)); }
    }
#endif
#if WINDOWS
    private static readonly Microsoft.UI.Xaml.Input.KeyEventHandler WindowRootKeyDownHandler = NativeRootOnKeyDown;
#endif

    public App(IServiceProvider services)
    {
        this.services = services;
        errorLogger = services.GetRequiredService<IErrorLogger>();

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            errorLogger?.Log(ex, nameof(InitializeComponent));
            Resources = new ResourceDictionary();
        }

        errorLogger?.LogInfo("App started.", nameof(App));
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            errorLogger?.Log(ex, "AppDomain.UnhandledException");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        errorLogger?.Log(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        rootPage ??= ResolveRootPage();

        var window = new Window(rootPage)
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
                }
            }
            catch (Exception ex)
            {
                errorLogger?.Log(ex, "Window.HandlerChanged");
            }
        };
#endif
        return window;
    }

    private Page ResolveRootPage()
    {
        try
        {
            return services.GetRequiredService<MainPage>();
        }
        catch (Exception ex)
        {
            return new ContentPage
            {
                Title = "Praxis",
                Content = new ScrollView
                {
                    Content = new VerticalStackLayout
                    {
                        Padding = 24,
                        Spacing = 10,
                        Children =
                        {
                            new Label { Text = "Failed to initialize MainPage." },
                            new Label { Text = ex.Message },
                        },
                    },
                },
            };
        }
    }

#if WINDOWS
    private static void NativeRootOnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        var ctrlDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(global::Windows.System.VirtualKey.Control)
            .HasFlag(global::Windows.UI.Core.CoreVirtualKeyStates.Down);
        var shiftDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(global::Windows.System.VirtualKey.Shift)
            .HasFlag(global::Windows.UI.Core.CoreVirtualKeyStates.Down);
        if (!ctrlDown)
        {
            return;
        }

        if (!shiftDown && e.Key == global::Windows.System.VirtualKey.Z)
        {
            if (ShouldHandleWindowsHistoryShortcut(sender))
            {
                RaiseHistoryShortcut("Undo");
                e.Handled = true;
            }
            return;
        }

        if ((!shiftDown && e.Key == global::Windows.System.VirtualKey.Y) ||
            (shiftDown && e.Key == global::Windows.System.VirtualKey.Z))
        {
            if (ShouldHandleWindowsHistoryShortcut(sender))
            {
                RaiseHistoryShortcut("Redo");
                e.Handled = true;
            }
            return;
        }

        if (!shiftDown)
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

    private static bool ShouldHandleWindowsHistoryShortcut(object sender)
    {
        if (isEditorOpen || isContextMenuOpen || isConflictDialogOpen)
        {
            return false;
        }

        if (sender is not Microsoft.UI.Xaml.UIElement element)
        {
            return true;
        }

        var xamlRoot = element.XamlRoot;
        if (xamlRoot is null)
        {
            return true;
        }

        try
        {
            var focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(xamlRoot) as Microsoft.UI.Xaml.DependencyObject;
            while (focused is not null)
            {
                if (focused is Microsoft.UI.Xaml.Controls.TextBox ||
                    focused is Microsoft.UI.Xaml.Controls.PasswordBox ||
                    focused is Microsoft.UI.Xaml.Controls.RichEditBox)
                {
                    return false;
                }

                focused = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(focused);
            }
        }
        catch (Exception ex)
        {
            errorLogger?.Log(ex, nameof(ShouldHandleWindowsHistoryShortcut));
        }

        return true;
    }
#endif

    public static void RaiseThemeShortcut(string mode)
    {
        try
        {
            ThemeShortcutRequested?.Invoke(mode);
        }
        catch (Exception ex)
        {
            errorLogger?.Log(ex, nameof(RaiseThemeShortcut));
        }
    }

    public static void RaiseEditorShortcut(string action)
    {
        try
        {
            EditorShortcutRequested?.Invoke(action);
        }
        catch (Exception ex)
        {
            errorLogger?.Log(ex, nameof(RaiseEditorShortcut));
        }
    }

    public static void RaiseCommandInputShortcut(string action)
    {
        try
        {
            CommandInputShortcutRequested?.Invoke(action);
        }
        catch (Exception ex)
        {
            errorLogger?.Log(ex, nameof(RaiseCommandInputShortcut));
        }
    }

    public static void RaiseHistoryShortcut(string action)
    {
        try
        {
            HistoryShortcutRequested?.Invoke(action);
        }
        catch (Exception ex)
        {
            errorLogger?.Log(ex, nameof(RaiseHistoryShortcut));
        }
    }

    public static void RaiseMiddleMouseClick()
    {
        try
        {
            MiddleMouseClickRequested?.Invoke();
        }
        catch (Exception ex)
        {
            errorLogger?.Log(ex, nameof(RaiseMiddleMouseClick));
        }
    }

    public static bool IsEditorOpen => isEditorOpen;
    public static bool IsContextMenuOpen => isContextMenuOpen;
    public static bool IsConflictDialogOpen => isConflictDialogOpen;

    public static void SetEditorOpenState(bool isOpen)
    {
        isEditorOpen = isOpen;
    }

    public static void SetContextMenuOpenState(bool isOpen)
    {
        isContextMenuOpen = isOpen;
    }

    public static void SetConflictDialogOpenState(bool isOpen)
    {
        isConflictDialogOpen = isOpen;
    }
}
