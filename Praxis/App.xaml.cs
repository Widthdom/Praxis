using Microsoft.Extensions.DependencyInjection;

namespace Praxis;

public partial class App : Application
{
    private readonly IServiceProvider services;
    private Page? rootPage;
    private static volatile bool isEditorOpen;
    private static volatile bool isContextMenuOpen;
    private static volatile bool isConflictDialogOpen;
    public static event Action<string>? ThemeShortcutRequested;
    public static event Action<string>? EditorShortcutRequested;
    public static event Action<string>? CommandInputShortcutRequested;
    public static event Action? MiddleMouseClickRequested;
#if MACCATALYST
    public static event Action? MacApplicationDeactivating;
    private static long lastActivatedAt;
    private static volatile bool isMacApplicationActive = true;
    public static void RecordActivation() => lastActivatedAt = Environment.TickCount64;
    public static bool IsActivationSuppressionActive() => Environment.TickCount64 - lastActivatedAt < 500;
    public static bool IsMacApplicationActive() => isMacApplicationActive;
    public static void SetMacApplicationActive(bool value) => isMacApplicationActive = value;
    public static void RaiseMacApplicationDeactivating()
    {
        try { MacApplicationDeactivating?.Invoke(); } catch { }
    }
#endif
#if WINDOWS
    private static readonly Microsoft.UI.Xaml.Input.KeyEventHandler WindowRootKeyDownHandler = NativeRootOnKeyDown;
#endif

    public App(IServiceProvider services)
    {
        this.services = services;
        try
        {
            InitializeComponent();
        }
        catch
        {
            Resources = new ResourceDictionary();
        }
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
            catch
            {
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

    public static void RaiseEditorShortcut(string action)
    {
        try
        {
            EditorShortcutRequested?.Invoke(action);
        }
        catch
        {
        }
    }

    public static void RaiseCommandInputShortcut(string action)
    {
        try
        {
            CommandInputShortcutRequested?.Invoke(action);
        }
        catch
        {
        }
    }

    public static void RaiseMiddleMouseClick()
    {
        try
        {
            MiddleMouseClickRequested?.Invoke();
        }
        catch
        {
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
