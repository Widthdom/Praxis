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

                        // Apply every theme-dependent Windows chrome attribute (WinUI root background, Win32 window-class brush, DWM border/caption colors, and DWMWA_USE_IMMERSIVE_DARK_MODE). One entry point so subsequent re-application on theme change stays in sync.
                        ApplyWindowsThemeChrome(nativeWindow);

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
                        // Even though the drag region's TransformToVisual ends up at (0,32) (matching the visible caption-buttons row), the OS does NOT honour the InputNonClientPointerSource.SetRegionRects call until something forces it to re-evaluate the window's non-client area. The user's report is the smoking gun: clicking a caption button (which triggers OverlappedPresenter.Maximize/Restore — a presenter-level state change) causes Windows to re-run NC hit-testing, and from that point on drag works. A programmatic 1-pixel AppWindow.Resize doesn't reproduce that effect because it only updates the size, not the frame. Win32 SetWindowPos with SWP_FRAMECHANGED is the canonical way to force the OS to recompute NC areas (it synthesises WM_NCCALCSIZE), which is exactly what gets the SetRegionRects declaration to take effect.
                        nativeWindow.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                        {
                            try
                            {
                                if (nativeWindow.AppWindow is { } aw)
                                {
                                    var size = aw.Size;
                                    aw.Resize(new global::Windows.Graphics.SizeInt32(size.Width + 1, size.Height));
                                    aw.Resize(size);

                                    // Force the OS to re-run NC hit-testing so the InputNonClientPointerSource.SetRegionRects Caption declaration becomes effective without requiring the user to click a caption button first.
                                    var hwnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(aw.Id);
                                    if (hwnd != IntPtr.Zero)
                                    {
                                        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
                                        CrashFileLogger.WriteInfo("Window.HandlerChanged", $"SWP_FRAMECHANGED issued on HWND 0x{hwnd.ToInt64():X} to commit Caption rects");
                                    }
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

            // The ~32 px phantom row above our visible caption-buttons row originates inside MAUI's RootNavigationView template, NOT in WindowRootView itself. A visual-tree dump showed that when `ExtendsContentIntoTitleBar = true`, WinUI's NavigationView template stamps `Margin="32,0,0,0"` onto the named ContentGrid template part. The public `NavigationView.IsTitleBarAutoPaddingEnabled` property is supposed to gate this, but on the current Windows App SDK it reads false by default yet the margin still gets applied — so we directly find the ContentGrid template part and force its Margin to zero on every measure pass. Re-applied (not one-shot) because the template re-stamps the margin during layout.
            if (element is Microsoft.UI.Xaml.DependencyObject navRoot)
            {
                try
                {
                    ForceNavigationViewContentGridMarginZero(navRoot, depth: 0);
                }
                catch (Exception ex)
                {
                    CrashFileLogger.WriteWarning("NavView.ContentGrid", $"Walk failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
                }
            }

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

    private static bool navigationViewContentGridLogged;
    private static System.Runtime.CompilerServices.ConditionalWeakTable<Microsoft.UI.Xaml.FrameworkElement, object> contentGridGuards = new();

    private static void AttachContentGridMarginGuard(Microsoft.UI.Xaml.FrameworkElement contentGrid)
    {
        // ConditionalWeakTable keyed by the element itself dedupes attach calls without leaking memory if the visual tree is rebuilt — each ContentGrid only gets one LayoutUpdated handler attached.
        if (contentGridGuards.TryGetValue(contentGrid, out _))
        {
            return;
        }
        var sentinel = new object();
        contentGridGuards.Add(contentGrid, sentinel);

        contentGrid.LayoutUpdated += (_, _) =>
        {
            try
            {
                var m = contentGrid.Margin;
                if (m.Top != 0 || m.Left != 0 || m.Right != 0 || m.Bottom != 0)
                {
                    contentGrid.Margin = new Microsoft.UI.Xaml.Thickness(0);
                }
            }
            catch (Exception ex)
            {
                CrashFileLogger.WriteWarning("NavView.ContentGrid", $"LayoutUpdated guard failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
            }
        };
        CrashFileLogger.WriteInfo("NavView.ContentGrid", $"LayoutUpdated guard attached on {contentGrid.GetType().FullName}");
    }

    private static int ForceNavigationViewContentGridMarginZero(Microsoft.UI.Xaml.DependencyObject? node, int depth)
    {
        if (node is null || depth > 14)
        {
            return 0;
        }

        if (node is Microsoft.UI.Xaml.Controls.NavigationView navView)
        {
            try
            {
                // Belt-and-suspenders: also flip the public gate. Logs from a previous run showed this property is already false by default on the active SDK, but if a future build changes the default we still want it off.
                if (navView.IsTitleBarAutoPaddingEnabled)
                {
                    navView.IsTitleBarAutoPaddingEnabled = false;
                    CrashFileLogger.WriteInfo("NavView.Padding", $"IsTitleBarAutoPaddingEnabled false on {navView.GetType().FullName}");
                }
            }
            catch (Exception ex)
            {
                CrashFileLogger.WriteWarning("NavView.Padding", $"Set failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
            }
        }

        if (node is Microsoft.UI.Xaml.FrameworkElement fe && fe.Name == "ContentGrid")
        {
            var m = fe.Margin;
            if (m.Top != 0 || m.Left != 0 || m.Right != 0 || m.Bottom != 0)
            {
                if (!navigationViewContentGridLogged)
                {
                    navigationViewContentGridLogged = true;
                    CrashFileLogger.WriteInfo("NavView.ContentGrid", $"Forcing Margin {m.Top:0}/{m.Left:0}/{m.Right:0}/{m.Bottom:0} → 0/0/0/0 (type={fe.GetType().FullName})");
                }
                fe.Margin = new Microsoft.UI.Xaml.Thickness(0);
            }
            // Attach a persistent guard the first time we encounter ContentGrid. Window resize triggers a NavigationView measure pass that re-stamps Margin="32,0,0,0" AFTER our SizeChanged null-out runs, so the one-shot or per-SizeChanged approach races with the template. LayoutUpdated fires on every layout cycle, so the guard catches the template's re-stamp and zeroes it back immediately — no race window where the phantom row reappears.
            AttachContentGridMarginGuard(fe);
            return 1;
        }

        var n = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(node);
        var hits = 0;
        for (var i = 0; i < n; i++)
        {
            hits += ForceNavigationViewContentGridMarginZero(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(node, i), depth + 1);
        }
        return hits;
    }

    private static void TrySetProperty(Type type, object instance, string name, object? value)
    {
        try
        {
            var prop = type.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prop is null || !prop.CanWrite)
            {
                return;
            }
            prop.SetValue(instance, value);
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

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [System.Runtime.InteropServices.DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, EraseBkgndSubclassProcDelegate pfnSubclass, UIntPtr uIdSubclass, IntPtr dwRefData);

    [System.Runtime.InteropServices.DllImport("comctl32.dll")]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private delegate IntPtr EraseBkgndSubclassProcDelegate(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData);

    private const int GCLP_HBRBACKGROUND = -10;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint WM_ERASEBKGND = 0x0014;

    // Single entry point for every theme-dependent Win32/DWM/WinUI chrome attribute. Called once at startup from Window.HandlerChanged, and again whenever MAUI's effective theme changes (RequestedThemeChanged fires for both OS theme changes and user-driven UserAppTheme toggles).
    internal static void ApplyWindowsThemeChrome(Microsoft.UI.Xaml.Window nativeWindow)
    {
        var isDark = ResolveWindowsAppThemeIsDark();
        ApplyWindowsImmersiveDarkMode(nativeWindow, isDark);
        ApplyWindowsRootContentBackground(nativeWindow, isDark);
        ApplyWindowsClassBackgroundBrush(nativeWindow, isDark);
        ApplyWindowsDwmColors(nativeWindow, isDark);
        UpdateEraseBkgndBrush(isDark);
        InstallEraseBkgndSubclass(nativeWindow);
    }

    // Keep delegate, brush handle, and subclassed HWND set alive at static scope. The SubclassProc runs on the OS message pump for the lifetime of the HWND, so the marshalled thunk must not be GC'd; the brush must not be deleted while the subclass is still installed.
    private static EraseBkgndSubclassProcDelegate? eraseBkgndSubclassDelegate;
    private static IntPtr eraseBkgndBrush = IntPtr.Zero;
    private static uint eraseBkgndBrushColor;
    private static readonly UIntPtr EraseBkgndSubclassId = unchecked((UIntPtr)0xE7A5EBC0u);
    private static readonly System.Collections.Generic.HashSet<IntPtr> eraseBkgndSubclassedHwnds = new();

    private static void UpdateEraseBkgndBrush(bool isDark)
    {
        // The class brush set via SetClassLongPtrW does not always get used by the OS during the brief window between a Win32 resize and WinUI's next swapchain present — the OS fills the freshly-exposed client strip with a white default. Owning the WM_ERASEBKGND path with our own FillRect closes that gap. Recreate the brush whenever the color changes (theme toggle); the old brush is GDI-leaked otherwise.
        var newColor = isDark ? 0x00161616u : 0x00F2F2F2u;
        if (eraseBkgndBrush != IntPtr.Zero && newColor == eraseBkgndBrushColor)
        {
            return;
        }
        var newBrush = CreateSolidBrush(newColor);
        if (newBrush == IntPtr.Zero)
        {
            CrashFileLogger.WriteWarning(nameof(UpdateEraseBkgndBrush), $"CreateSolidBrush returned NULL for 0x{newColor:X6}");
            return;
        }
        var oldBrush = eraseBkgndBrush;
        eraseBkgndBrush = newBrush;
        eraseBkgndBrushColor = newColor;
        if (oldBrush != IntPtr.Zero)
        {
            DeleteObject(oldBrush);
        }
    }

    private static void InstallEraseBkgndSubclass(Microsoft.UI.Xaml.Window nativeWindow)
    {
        try
        {
            var windowId = nativeWindow.AppWindow?.Id;
            if (windowId is null)
            {
                return;
            }
            var hwnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(windowId.Value);
            if (hwnd == IntPtr.Zero || eraseBkgndSubclassedHwnds.Contains(hwnd))
            {
                return;
            }
            eraseBkgndSubclassDelegate ??= EraseBkgndSubclassProc;
            if (SetWindowSubclass(hwnd, eraseBkgndSubclassDelegate, EraseBkgndSubclassId, IntPtr.Zero))
            {
                eraseBkgndSubclassedHwnds.Add(hwnd);
            }
            else
            {
                var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                CrashFileLogger.WriteWarning(nameof(InstallEraseBkgndSubclass), $"SetWindowSubclass failed: Win32Error={err}");
            }
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(InstallEraseBkgndSubclass), $"Failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }

    private static IntPtr EraseBkgndSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData)
    {
        // Only intercept WM_ERASEBKGND. Everything else passes through to DefSubclassProc so we don't disturb NC painting, hit-testing, rounded corners, resize borders, or anything else WinUI / the OS rely on.
        if (uMsg == WM_ERASEBKGND && eraseBkgndBrush != IntPtr.Zero)
        {
            if (GetClientRect(hWnd, out var rc))
            {
                FillRect(wParam, ref rc, eraseBkgndBrush);
            }
            // Returning non-zero tells the OS the background has already been erased — DefWindowProc will not re-fill the area with the class brush (which on WinUI 3 windows tends to be white regardless of GCLP_HBRBACKGROUND overrides).
            return (IntPtr)1;
        }
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private static bool ResolveWindowsAppThemeIsDark()
    {
        // Resolve the *effective* app theme: an explicit UserAppTheme override wins, otherwise fall back to the OS RequestedTheme. Reading WinUI's Microsoft.UI.Xaml.Application.RequestedTheme (which mirrors the OS) bypasses any Ctrl+Shift+L/D/H override the user has made, so we must consult MAUI's Application.UserAppTheme first.
        try
        {
            var maui = Microsoft.Maui.Controls.Application.Current;
            if (maui is not null)
            {
                if (maui.UserAppTheme != AppTheme.Unspecified)
                {
                    return maui.UserAppTheme == AppTheme.Dark;
                }
                return maui.RequestedTheme == AppTheme.Dark;
            }
        }
        catch
        {
            // Fall through to the WinUI-side oracle below.
        }
        return Microsoft.UI.Xaml.Application.Current?.RequestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Dark;
    }

    private static void ApplyWindowsImmersiveDarkMode(Microsoft.UI.Xaml.Window nativeWindow, bool isDark)
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

            // The Win11 DWM draws the ~1 px outer border around the window. Without DWMWA_USE_IMMERSIVE_DARK_MODE the OS treats the window as a Light-mode app and paints that border in a near-white tone, which is the visible "white outline" around a dark-themed Praxis window. Setting the attribute to TRUE flips DWM to its Dark-mode chrome (dark outer border + dark inactive title-bar simulacrum). Documented since Windows 10 build 18985 (BOOL = int).
            var value = isDark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(ApplyWindowsImmersiveDarkMode), $"Failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }

    private static void ApplyWindowsDwmColors(Microsoft.UI.Xaml.Window nativeWindow, bool isDark)
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
            var color = isDark ? 0x00161616u : 0x00F2F2F2u;
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref color, sizeof(uint));
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref color, sizeof(uint));
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(ApplyWindowsDwmColors), $"Failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }

    private static void ApplyWindowsClassBackgroundBrush(Microsoft.UI.Xaml.Window nativeWindow, bool isDark)
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
            var colorRef = isDark ? 0x00161616u : 0x00F2F2F2u;
            var brush = CreateSolidBrush(colorRef);
            if (brush == IntPtr.Zero)
            {
                CrashFileLogger.WriteWarning(nameof(ApplyWindowsClassBackgroundBrush), "CreateSolidBrush returned NULL");
                return;
            }

            SetClassLongPtrW(hwnd, GCLP_HBRBACKGROUND, brush);
            InvalidateRect(hwnd, IntPtr.Zero, true);
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(ApplyWindowsClassBackgroundBrush), $"Failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }

    private static void CollapseWindowsTitleBarReserveRow(Microsoft.UI.Xaml.Window nativeWindow)
    {
        // The actual phantom-row fix lives one level deeper inside Microsoft.Maui.Platform.WindowRootView (a descendant of WindowRootViewContainer). Recursively walk the visual tree from Window.Content and null-out the title-bar fields on every WindowRootView we find; ForceNavigationViewContentGridMarginZero in TryNullAppTitleBarRecursive handles the actual margin override.
        try
        {
            TryNullAppTitleBarRecursive(nativeWindow.Content, depth: 0);
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(CollapseWindowsTitleBarReserveRow), $"Failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }

    private static void ApplyWindowsRootContentBackground(Microsoft.UI.Xaml.Window nativeWindow, bool isDark)
    {
        try
        {
            // Match the page idle background from Resources/Styles/Colors.xaml (`Light=#F2F2F2 / Dark=#161616`). The effective theme oracle is resolved by ApplyWindowsThemeChrome's caller (UserAppTheme override wins over OS RequestedTheme).
            var bg = isDark
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
