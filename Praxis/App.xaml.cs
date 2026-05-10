using Microsoft.Extensions.DependencyInjection;
using Praxis.Core.Logic;
using Praxis.Services;

namespace Praxis;

public partial class App : Application
{
    private readonly IServiceProvider services;
    private static IErrorLogger? errorLogger;
    private static int globalExceptionHandlersRegistered;
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
        TryRaise(MacApplicationDeactivating, nameof(RaiseMacApplicationDeactivating));
    }

    public static void RaiseMacApplicationActivated()
    {
        TryRaise(MacApplicationActivated, nameof(RaiseMacApplicationActivated));
    }
#endif
#if WINDOWS
    private static readonly Microsoft.UI.Xaml.Input.KeyEventHandler WindowRootKeyDownHandler = NativeRootOnKeyDown;
#endif

    public App(IServiceProvider services)
    {
        this.services = services;
        errorLogger = services.GetRequiredService<IErrorLogger>();
        errorLogger?.LogInfo("App constructor started.", nameof(App));

        if (Interlocked.Exchange(ref globalExceptionHandlersRegistered, 1) == 0)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            errorLogger?.Log(ex, nameof(InitializeComponent));
            Resources = new ResourceDictionary();
        }

        errorLogger?.LogInfo($"App started. CrashLog={CrashFileLogger.LogFilePath}", nameof(App));
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var isTerminating = e.IsTerminating;
        if (e.ExceptionObject is Exception ex)
        {
            // Always write to crash file synchronously first — DB write may not complete.
            CrashFileLogger.WriteException(
                $"AppDomain.UnhandledException (IsTerminating={isTerminating})", ex);
            errorLogger?.Log(ex, $"AppDomain.UnhandledException (IsTerminating={isTerminating})");
        }
        else
        {
            var safePayload = CrashFileLogger.SafeObjectDescription(e.ExceptionObject);
            var message = $"Non-Exception object thrown (IsTerminating={isTerminating}): {safePayload}";
            CrashFileLogger.WriteWarning(
                "AppDomain.UnhandledException",
                message);
            errorLogger?.LogWarning(message, "AppDomain.UnhandledException");
        }

        if (isTerminating)
        {
            TryFlushLogs(TimeSpan.FromSeconds(2), "AppDomain.UnhandledException");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        CrashFileLogger.WriteException("TaskScheduler.UnobservedTaskException", e.Exception);
        errorLogger?.Log(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        CrashFileLogger.WriteInfo("App", "Process exiting — flushing logs.");
        TryFlushLogs(TimeSpan.FromSeconds(3), "App.ProcessExit");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        errorLogger?.LogInfo($"CreateWindow called. ExistingRootPage={rootPage is not null}", nameof(CreateWindow));
        var page = rootPage;
        if (page is null)
        {
            page = ResolveRootPage();
            if (page is MainPage)
            {
                rootPage = page;
            }
            else
            {
                errorLogger?.LogWarning("Root page resolution fell back to an error page; cache not updated.", nameof(CreateWindow));
            }
        }

        var window = new Window(page)
        {
            Width = 1000,
            Height = 700,
            // The editor modal is 760 wide + 18 padding × 2 = 796, sitting inside the
            // RootGrid's 18 px padding. Plus a little chrome margin gives ~860.
            // Height is set so all editor rows + Cancel/Save buttons stay visible
            // without scroll-clipping the modal itself.
            MinimumWidth = 860,
            MinimumHeight = 600,
            Title = string.Empty,
        };


#if WINDOWS
        window.HandlerChanged += (_, _) =>
        {
            try
            {
                if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                {
                    // Apply the WinUI 3 XAML Window title-bar customization BEFORE Activate(): if the OS title bar has already been laid out, setting ExtendsContentIntoTitleBar later does not always remove it under MAUI's WinUI host.
                    // The WinUI 3 equivalent of WPF's WindowStyle="None" is OverlappedPresenter.SetBorderAndTitleBar(true, false): the resize border stays so the window can still be sized from the edges, but the system title bar and the system caption buttons are fully removed. ExtendsContentIntoTitleBar=true alone leaves the system caption buttons drawn in the top-right corner regardless of ButtonForegroundColor=transparent overrides.
                    try
                    {
                        nativeWindow.ExtendsContentIntoTitleBar = true;
                        var applied = nativeWindow.ExtendsContentIntoTitleBar;
                        // MAUI's WinUI host applies a Mica SystemBackdrop by default, which paints the lavender Win11-style tint in the title-bar area. Setting it to null lets the page background fill the entire window flush against the WinUI client edge — required for the "single solid colour window" the design calls for.
                        var backdropTypeBefore = nativeWindow.SystemBackdrop?.GetType().Name ?? "(null)";
                        nativeWindow.SystemBackdrop = null;
                        var presenterKind = "(null)";
                        if (nativeWindow.AppWindow?.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                        {
                            // hasBorder=true keeps edge-resize / Aero Snap; hasTitleBar=false removes the system title bar AND system caption buttons.
                            presenter.SetBorderAndTitleBar(true, false);
                            presenterKind = $"OverlappedPresenter(HasTitleBar={presenter.HasTitleBar}, HasBorder={presenter.HasBorder})";
                        }

                        // During a window resize the WinUI layout pass lags behind the native HWND geometry, so the OS-painted client area shows pure white where MAUI's content has not yet stretched to the new edge. Painting the WinUI Window's root content panel with the theme's page colour eliminates that "stretching gap" — the new strip is filled with the matching gray instead of white.
                        ApplyWindowsRootContentBackground(nativeWindow);
                        // The XAML brush only covers the WinUI client area; when the user drags the right or bottom resize border outward, the OS extends the HWND first and paints the new client edge with the window class's background brush before WinUI gets a chance to render. The default class brush is white. Win32 SetClassLongPtrW lets us swap it for a brush matching the page background — minimal Win32 surface (no acrylic, no NC paint hooks) and the only way to eliminate the right/bottom resize flash.
                        ApplyWindowsClassBackgroundBrush(nativeWindow);
                        // DWM also paints the resize-border / caption strip directly during compositor-managed resizes. Tell it our page colour so the right/bottom drag lookup matches the rest of the window.
                        ApplyWindowsDwmColors(nativeWindow);

                        // MAUI's WindowRootViewContainer has Row 0 reserved (~32px Auto) for an AppTitleBar and Row 1 for the page content. The reserve is kept even when no MAUI TitleBar is set, so MainPage's row 0 is pushed down 32px and the OS treats that phantom strip above as default drag area. Collapse Row 0 to 0 so MainPage starts flush against the top of the window and our InputNonClientPointerSource Caption region (computed from the visible WindowTitleBarDragRegion's TransformToVisual) lands on the visually rendered title bar row.
                        CollapseWindowsTitleBarReserveRow(nativeWindow);

                        // MAUI's WindowRootView re-applies _appTitleBarHeight=32 on every measure pass (after the initial null-out, the next resize/measure pass undoes our work). Hook SizeChanged to re-null the title-bar fields whenever the window relayouts so the page never gets pushed down 32px again.
                        nativeWindow.SizeChanged += (_, _) =>
                        {
                            TryNullAppTitleBarRecursive(nativeWindow.Content, depth: 0);
                            if (nativeWindow.Content is Microsoft.UI.Xaml.UIElement el)
                            {
                                el.InvalidateMeasure();
                            }
                        };
                        // Diagnostic logs showed the drag region's TransformToVisual returns (0,0) at startup but jumps to (0,32) after the user manually resizes the window — i.e., the visible caption-buttons row only matches the declared Caption region after the layout has had one full resize cycle to settle. Trigger that cycle programmatically via a 1-pixel resize-and-restore right after activation, so the "drag works" state is reached before the user touches anything.
                        nativeWindow.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                        {
                            try
                            {
                                if (nativeWindow.AppWindow is { } aw)
                                {
                                    var size = aw.Size;
                                    aw.Resize(new global::Windows.Graphics.SizeInt32(size.Width + 1, size.Height));
                                    aw.Resize(size);
                                }
                            }
                            catch (Exception nudgeEx)
                            {
                                CrashFileLogger.WriteWarning("Window.HandlerChanged", $"Startup nudge resize failed: {CrashFileLogger.SafeExceptionMessage(nudgeEx)}");
                            }
                            TryNullAppTitleBarRecursive(nativeWindow.Content, depth: 0);
                            if (nativeWindow.Content is Microsoft.UI.Xaml.UIElement el)
                            {
                                el.InvalidateMeasure();
                            }
                        });

                        CrashFileLogger.WriteInfo("Window.HandlerChanged", $"ExtendsContentIntoTitleBar set; readback={applied}; backdropBefore={backdropTypeBefore}; presenter={presenterKind}");
                    }
                    catch (Exception titleBarEx)
                    {
                        CrashFileLogger.WriteWarning("Window.HandlerChanged", $"ExtendsContentIntoTitleBar setup failed: {CrashFileLogger.SafeExceptionMessage(titleBarEx)}");
                    }

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
                var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
                var platformViewType = window.Handler?.PlatformView?.GetType().Name ?? "(null)";
                errorLogger?.LogWarning(
                    $"Window handler activation failed for root page '{page.GetType().Name}' with platformView='{platformViewType}': {safeMessage}",
                    "Window.HandlerChanged");
            }
        };
#endif
        errorLogger?.LogInfo($"Window created. RootPage={page.GetType().Name}", nameof(CreateWindow));
        return window;
    }

    private static void TryFlushLogs(TimeSpan timeout, string context)
    {
        try
        {
            errorLogger?.FlushAsync(timeout).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteException(context, ex);
            CrashFileLogger.WriteWarning(context, $"Log flush failed: {safeMessage}");
        }
    }

#if WINDOWS
    private static void TryNullAppTitleBarRecursive(object? element, int depth)
    {
        if (depth > 4 || element is null)
        {
            return;
        }

        var type = element.GetType();
        if (type.FullName == "Microsoft.Maui.Platform.WindowRootView")
        {
            // Per WRV.TitleProps / WRV.TitleFields diagnostic dump on MAUI 10:
            // WindowTitleBarContentControlVisibility (Visibility, settable) — hide the title-bar content control entirely
            // WindowTitleBarContentControlMinHeight (Double, settable) — drop its min-height to 0
            // _appTitleBar (FrameworkElement field) — clear the cached title-bar element
            // _appTitleBarHeight (Double field) — clear the cached height that drives the layout reserve
            // _useCustomAppTitleBar (Boolean field) — switch to "no custom title bar" mode
            TrySetProperty(type, element, "WindowTitleBarContentControlVisibility", Microsoft.UI.Xaml.Visibility.Collapsed);
            TrySetProperty(type, element, "WindowTitleBarContentControlMinHeight", 0.0);
            TrySetProperty(type, element, "WindowTitleBarContent", null);
            TrySetProperty(type, element, "AppTitleBarTemplate", null);
            TrySetProperty(type, element, "AppTitleBarContainer", null);
            TrySetProperty(type, element, "AppTitleBarContentControl", null);
            TrySetField(type, element, "_appTitleBar", null);
            TrySetField(type, element, "_appTitleBarHeight", 0.0);
            TrySetField(type, element, "_useCustomAppTitleBar", false);
            TrySetField(type, element, "_titleBar", null);

            if (element is Microsoft.UI.Xaml.UIElement uiEl)
            {
                uiEl.InvalidateMeasure();
            }
            return;
        }

        // Recurse into children if this is a Panel.
        if (element is Microsoft.UI.Xaml.Controls.Panel panel)
        {
            foreach (var child in panel.Children)
            {
                TryNullAppTitleBarRecursive(child, depth + 1);
            }
        }
    }

    private static void TrySetProperty(Type type, object instance, string name, object? value)
    {
        try
        {
            var prop = type.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prop is null)
            {
                return;
            }
            if (!prop.CanWrite)
            {
                CrashFileLogger.WriteInfo("WRV.Set", $"{name} not writable");
                return;
            }
            prop.SetValue(instance, value);
            CrashFileLogger.WriteInfo("WRV.Set", $"{name}={value ?? "null"}");
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning("WRV.Set", $"{name} failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }

    private static void TrySetField(Type type, object instance, string name, object? value)
    {
        try
        {
            var field = type.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field is null)
            {
                return;
            }
            field.SetValue(instance, value);
            CrashFileLogger.WriteInfo("WRV.Set", $"{name}={value ?? "null"}");
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning("WRV.Set", $"{name} failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetClassLongPtrW")]
    private static extern IntPtr SetClassLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(uint crColor);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref uint attrValue, int attrSize);

    private const int GCLP_HBRBACKGROUND = -10;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_CAPTION_COLOR = 35;

    private static void ApplyWindowsDwmColors(Microsoft.UI.Xaml.Window nativeWindow)
    {
        try
        {
            var windowId = nativeWindow.AppWindow?.Id;
            if (windowId is null)
            {
                return;
            }
            var hwnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(windowId.Value);
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            // DWM uses these attributes to paint the resize border + (logical) caption strip during compositor-managed resizes. Painting them with the page background colour stops the white flash that appears when the user grabs the right or bottom resize handle. Format is COLORREF (0x00BBGGRR).
            var dark = Microsoft.UI.Xaml.Application.Current?.RequestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Dark;
            var color = dark ? 0x00161616u : 0x00F2F2F2u;
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref color, sizeof(uint));
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref color, sizeof(uint));
            CrashFileLogger.WriteInfo(nameof(ApplyWindowsDwmColors), $"DWM border+caption color set to 0x{color:X6}");
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(ApplyWindowsDwmColors), $"Failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }

    private static void ApplyWindowsClassBackgroundBrush(Microsoft.UI.Xaml.Window nativeWindow)
    {
        try
        {
            var windowId = nativeWindow.AppWindow?.Id;
            if (windowId is null)
            {
                return;
            }

            var hwnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(windowId.Value);
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            // SetClassLongPtrW expects COLORREF (0x00BBGGRR). Mirror the page idle background from Resources/Styles/Colors.xaml.
            var dark = Microsoft.UI.Xaml.Application.Current?.RequestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Dark;
            var colorRef = dark ? 0x00161616u : 0x00F2F2F2u;
            var brush = CreateSolidBrush(colorRef);
            if (brush == IntPtr.Zero)
            {
                CrashFileLogger.WriteWarning(nameof(ApplyWindowsClassBackgroundBrush), "CreateSolidBrush returned NULL");
                return;
            }

            var previous = SetClassLongPtrW(hwnd, GCLP_HBRBACKGROUND, brush);
            InvalidateRect(hwnd, IntPtr.Zero, true);
            CrashFileLogger.WriteInfo(nameof(ApplyWindowsClassBackgroundBrush), $"GCLP_HBRBACKGROUND set: previous=0x{previous.ToInt64():X} new=0x{brush.ToInt64():X} colorRef=0x{colorRef:X6}");
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(ApplyWindowsClassBackgroundBrush), $"Failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }

    private static void CollapseWindowsTitleBarReserveRow(Microsoft.UI.Xaml.Window nativeWindow)
    {
        try
        {
            // MAUI's WindowRootViewContainer is a custom internal Panel whose MeasureOverride reserves an AppTitleBar slot at the top before sizing the page content. The exact member name has changed across MAUI versions — enumerate properties and fields so we can null out whatever holds the title-bar element.
            var content = nativeWindow.Content;
            if (content is null)
            {
                CrashFileLogger.WriteInfo(nameof(CollapseWindowsTitleBarReserveRow), "Window.Content is null");
                return;
            }

            var contentType = content.GetType();
            if (contentType.FullName != "Microsoft.Maui.Platform.WindowRootViewContainer")
            {
                CrashFileLogger.WriteInfo(nameof(CollapseWindowsTitleBarReserveRow), $"Window.Content is not WindowRootViewContainer; type={contentType.FullName}");
                return;
            }

            const System.Reflection.BindingFlags allInstance = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // Diagnostic: log every property and field so we can identify the right name once and for all.
            var properties = new System.Text.StringBuilder();
            foreach (var p in contentType.GetProperties(allInstance))
            {
                properties.Append($"{p.Name}:{p.PropertyType.Name}(r={p.CanRead}w={p.CanWrite}) ");
            }
            CrashFileLogger.WriteInfo("WRVC.Properties", properties.ToString());

            var fields = new System.Text.StringBuilder();
            foreach (var f in contentType.GetFields(allInstance))
            {
                fields.Append($"{f.Name}:{f.FieldType.Name} ");
            }
            CrashFileLogger.WriteInfo("WRVC.Fields", fields.ToString());

            // Try the documented candidate names; first one that's writable wins.
            var candidateProperties = new[] { "AppTitleBar", "TitleBar", "AppTitleBarContainer", "TitleBarContainer" };
            foreach (var name in candidateProperties)
            {
                var prop = contentType.GetProperty(name, allInstance);
                if (prop?.CanWrite == true)
                {
                    var hadValue = prop.GetValue(content) is not null;
                    prop.SetValue(content, null);
                    CrashFileLogger.WriteInfo(nameof(CollapseWindowsTitleBarReserveRow), $"{name}=null (hadValue={hadValue})");
                    return;
                }
            }

            var candidateFields = new[] { "_titleBar", "_appTitleBar", "_titleBarContainer", "_appTitleBarContainer" };
            foreach (var name in candidateFields)
            {
                var field = contentType.GetField(name, allInstance);
                if (field is not null)
                {
                    var hadValue = field.GetValue(content) is not null;
                    field.SetValue(content, null);
                    if (content is Microsoft.UI.Xaml.UIElement uiContent)
                    {
                        uiContent.InvalidateMeasure();
                    }
                    CrashFileLogger.WriteInfo(nameof(CollapseWindowsTitleBarReserveRow), $"{name}=null (hadValue={hadValue})");
                    return;
                }
            }

            // The actual title-bar slot lives one level deeper, in Microsoft.Maui.Platform.WindowRootView (Children[0] of WindowRootViewContainer). Recursively walk the visual tree and null out any AppTitleBar / TitleBar property we find on the way.
            TryNullAppTitleBarRecursive(content, depth: 0);

            // Diagnostic: dump WindowRootView's members so we can see exactly what's there.
            if (content is Microsoft.UI.Xaml.Controls.Panel rootPanel)
            {
                foreach (var child in rootPanel.Children)
                {
                    if (child?.GetType().FullName == "Microsoft.Maui.Platform.WindowRootView")
                    {
                        var rvType = child.GetType();
                        var rvProps = new System.Text.StringBuilder();
                        foreach (var p in rvType.GetProperties(allInstance))
                        {
                            if (p.Name.Contains("Title", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Caption", StringComparison.OrdinalIgnoreCase))
                            {
                                rvProps.Append($"{p.Name}:{p.PropertyType.Name}(r={p.CanRead}w={p.CanWrite}) ");
                            }
                        }
                        CrashFileLogger.WriteInfo("WRV.TitleProps", rvProps.ToString());
                        var rvFields = new System.Text.StringBuilder();
                        foreach (var f in rvType.GetFields(allInstance))
                        {
                            if (f.Name.Contains("title", StringComparison.OrdinalIgnoreCase) || f.Name.Contains("caption", StringComparison.OrdinalIgnoreCase))
                            {
                                rvFields.Append($"{f.Name}:{f.FieldType.Name} ");
                            }
                        }
                        CrashFileLogger.WriteInfo("WRV.TitleFields", rvFields.ToString());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(CollapseWindowsTitleBarReserveRow), $"Failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }

    private static void ApplyWindowsRootContentBackground(Microsoft.UI.Xaml.Window nativeWindow)
    {
        try
        {
            // Match the page idle background from Resources/Styles/Colors.xaml (`Light=#F2F2F2 / Dark=#161616`). Using the OS RequestedTheme as the theme oracle means the background tracks the OS even before MAUI's app theme has loaded.
            var dark = Microsoft.UI.Xaml.Application.Current?.RequestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Dark;
            var bg = dark
                ? global::Windows.UI.Color.FromArgb(255, 0x16, 0x16, 0x16)
                : global::Windows.UI.Color.FromArgb(255, 0xF2, 0xF2, 0xF2);
            var brush = new Microsoft.UI.Xaml.Media.SolidColorBrush(bg);
            if (nativeWindow.Content is Microsoft.UI.Xaml.Controls.Panel panel)
            {
                panel.Background = brush;
            }
            else if (nativeWindow.Content is Microsoft.UI.Xaml.Controls.Control control)
            {
                control.Background = brush;
            }
            else if (nativeWindow.Content is Microsoft.UI.Xaml.Controls.Border border)
            {
                border.Background = brush;
            }
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(ApplyWindowsRootContentBackground), $"Failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }

#endif

    private Page ResolveRootPage()
    {
        try
        {
            var page = services.GetRequiredService<MainPage>();
            errorLogger?.LogInfo($"Resolved root page: {page.GetType().Name}", nameof(ResolveRootPage));
            return page;
        }
        catch (Exception ex)
        {
            errorLogger?.Log(ex, nameof(ResolveRootPage));
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            return new ContentPage
            {
                Title = string.Empty,
                Content = new ScrollView
                {
                    Content = new VerticalStackLayout
                    {
                        Padding = 24,
                        Spacing = 10,
                        Children =
                        {
                            new Label { Text = "Failed to initialize MainPage." },
                            new Label { Text = safeMessage },
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
        TryRaise(ThemeShortcutRequested, mode, nameof(RaiseThemeShortcut));
    }

    public static void RaiseEditorShortcut(string action)
    {
        TryRaise(EditorShortcutRequested, action, nameof(RaiseEditorShortcut));
    }

    public static void RaiseCommandInputShortcut(string action)
    {
        TryRaise(CommandInputShortcutRequested, action, nameof(RaiseCommandInputShortcut));
    }

    public static void RaiseHistoryShortcut(string action)
    {
        TryRaise(HistoryShortcutRequested, action, nameof(RaiseHistoryShortcut));
    }

    public static void RaiseMiddleMouseClick()
    {
        TryRaise(MiddleMouseClickRequested, nameof(RaiseMiddleMouseClick));
    }

    private static void TryRaise(Action? handler, string context)
    {
        try
        {
            handler?.Invoke();
        }
        catch (Exception ex)
        {
            errorLogger?.Log(ex, context);
        }
    }

    private static void TryRaise<T>(Action<T>? handler, T argument, string context)
    {
        try
        {
            handler?.Invoke(argument);
        }
        catch (Exception ex)
        {
            errorLogger?.Log(ex, context);
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
