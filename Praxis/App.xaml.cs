using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
#if MACCATALYST
using UIKit;
#endif

namespace Praxis;

public partial class App : Application
{
    private readonly IServiceProvider services;
    private Page? rootPage;
    public static event Action<string>? ThemeShortcutRequested;
    public static event Action<string>? EditorShortcutRequested;
    public static event Action<string>? CommandInputShortcutRequested;
    public static event Action? MiddleMouseClickRequested;
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
#if MACCATALYST
        window.HandlerChanged += (_, _) =>
        {
            ActivateMacWindow(window);
            if (window.Dispatcher is not null)
            {
                window.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(120), () => ActivateMacWindow(window));
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

#if MACCATALYST
    private static void ActivateMacWindow(Window window)
    {
        try
        {
            if (window.Handler?.PlatformView is not UIWindow nativeWindow)
            {
                return;
            }

            if (!nativeWindow.IsKeyWindow)
            {
                nativeWindow.MakeKeyAndVisible();
            }

            if (nativeWindow.RootViewController is UIResponder responder && responder.CanBecomeFirstResponder)
            {
                responder.BecomeFirstResponder();
            }

            if (UIApplication.SharedApplication.Delegate is UIResponder appDelegate && appDelegate.CanBecomeFirstResponder)
            {
                appDelegate.BecomeFirstResponder();
            }

            ActivateMacApplication();
        }
        catch
        {
        }
    }

    private static void ActivateMacApplication()
    {
        try
        {
            foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
            {
                if (scene is not UIWindowScene windowScene)
                {
                    continue;
                }

                var activateMethod = FindSceneActivationMethod("ActivateSceneSession");
                if (activateMethod is not null)
                {
                    activateMethod.Invoke(UIApplication.SharedApplication, [windowScene.Session, null, null, null]);
                    continue;
                }

                var requestMethod = FindSceneActivationMethod("RequestSceneSessionActivation");
                requestMethod?.Invoke(UIApplication.SharedApplication, [windowScene.Session, null, null, null]);
            }
        }
        catch
        {
        }
    }

    private static MethodInfo? FindSceneActivationMethod(string name)
    {
        foreach (var method in typeof(UIApplication).GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (method.Name == name && method.GetParameters().Length == 4)
            {
                return method;
            }
        }

        return null;
    }
#endif
}
