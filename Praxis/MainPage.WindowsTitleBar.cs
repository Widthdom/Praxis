using Praxis.Services;

namespace Praxis;

public partial class MainPage
{
#if WINDOWS
    // Segoe Fluent Icons code points (kept as `\u` escape sequences so the file survives any tooling round-trip that strips Private Use Area glyphs).
    private const string WindowsMinimizeGlyph = ""; // ChromeMinimize
    private const string WindowsMaximizeGlyph = ""; // ChromeMaximize
    private const string WindowsRestoreGlyph = "";  // ChromeRestore
    private const string WindowsCloseGlyph = "";    // ChromeClose

    private void ConfigureWindowsTitleBar()
    {
        if (windowsTitleBarConfigured)
        {
            return;
        }

        if (Handler?.MauiContext is null || Window is null)
        {
            return;
        }

        if (Window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
        {
            return;
        }

        windowsNativeWindow = nativeWindow;
        try
        {
            windowsAppWindow = nativeWindow.AppWindow;
            windowsOverlappedPresenter = windowsAppWindow?.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;

            // App.xaml.cs::CreateWindow already configured ExtendsContentIntoTitleBar, SystemBackdrop=null, and OverlappedPresenter.SetBorderAndTitleBar(true, false). No system caption buttons remain to recolour, so this partial only needs to keep the AppWindow / Presenter references for max/restore tracking and drag-region updates.
            if (windowsAppWindow is not null)
            {
                windowsAppWindow.Title = string.Empty;
            }
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(MainPage), $"ExtendsContentIntoTitleBar setup failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }

        EnsureCaptionButtonGlyphs();
        AttachWindowsTitleBarGestures();

        // Re-declare drag region whenever the title bar grid lays out at a new size; otherwise the drag rectangle stays at the initial measurement and dragging fails after a resize.
        WindowTitleBar.SizeChanged += (_, _) => UpdateWindowsTitleBarDragRegion();
        WindowTitleBarDragRegion.SizeChanged += (_, _) => UpdateWindowsTitleBarDragRegion();
        WindowCaptionButtonsStack.SizeChanged += (_, _) => UpdateWindowsTitleBarDragRegion();
        UpdateWindowsTitleBarDragRegion();

        if (windowsAppWindow is not null)
        {
            windowsAppWindow.Changed += WindowsAppWindow_Changed;
        }
        UpdateWindowsTitleBarMaximizedState();
        windowsTitleBarConfigured = true;
    }

    private void EnsureCaptionButtonGlyphs()
    {
        // XAML already supplies the glyphs as `&#xE921;`-style entity references. Only override here if our runtime constants actually carry the PUA character, so that a tooling round-trip that strips the constants does not blank out the XAML-rendered glyph.
        if (WindowMinimizeButton.Content is Label minLabel && !string.IsNullOrEmpty(WindowsMinimizeGlyph))
        {
            minLabel.Text = WindowsMinimizeGlyph;
        }

        var maxGlyph = windowsTitleBarMaximized ? WindowsRestoreGlyph : WindowsMaximizeGlyph;
        if (!string.IsNullOrEmpty(maxGlyph))
        {
            WindowMaximizeButtonGlyph.Text = maxGlyph;
        }

        if (WindowCloseButton.Content is Label closeLabel && !string.IsNullOrEmpty(WindowsCloseGlyph))
        {
            closeLabel.Text = WindowsCloseGlyph;
        }
    }

    private void AttachWindowsTitleBarGestures()
    {
        AttachWindowsCaptionButtonGesture(WindowMinimizeButton, MinimizeWindowsWindow);
        AttachWindowsCaptionButtonGesture(WindowMaximizeButton, ToggleWindowsMaximize);
        AttachWindowsCaptionButtonGesture(WindowCloseButton, CloseWindowsWindow);
    }

    private void AttachWindowsCaptionButtonGesture(Border button, Action onClick)
    {
        if (button.GestureRecognizers.OfType<TapGestureRecognizer>().Any())
        {
            return;
        }

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => onClick();
        button.GestureRecognizers.Add(tap);

        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => SetWindowsCaptionButtonHover(button, true);
        pointer.PointerExited += (_, _) => SetWindowsCaptionButtonHover(button, false);
        pointer.PointerPressed += (_, _) => SetWindowsCaptionButtonPressed(button, true);
        pointer.PointerReleased += (_, _) => SetWindowsCaptionButtonPressed(button, false);
        button.GestureRecognizers.Add(pointer);
    }

    private void SetWindowsCaptionButtonHover(Border button, bool hovered)
    {
        var dark = IsDarkThemeActive();
        button.BackgroundColor = hovered
            ? Color.FromArgb(dark ? "#3A3A3A" : "#E0E0E0")
            : Colors.Transparent;
    }

    private void SetWindowsCaptionButtonPressed(Border button, bool pressed)
    {
        if (!pressed)
        {
            // Mouse release returns the button to its hover/idle visual; PointerExited will reset to transparent if the pointer also left.
            return;
        }

        var dark = IsDarkThemeActive();
        button.BackgroundColor = Color.FromArgb(dark ? "#4A4A4A" : "#D0D0D0");
    }

    private void MinimizeWindowsWindow()
    {
        try
        {
            windowsOverlappedPresenter?.Minimize();
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(MainPage), $"Minimize failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }

    private void ToggleWindowsMaximize()
    {
        try
        {
            if (windowsOverlappedPresenter is null)
            {
                return;
            }

            if (windowsOverlappedPresenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized)
            {
                windowsOverlappedPresenter.Restore();
            }
            else
            {
                windowsOverlappedPresenter.Maximize();
            }
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(MainPage), $"MaximizeOrRestore failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }

    private void CloseWindowsWindow()
    {
        try
        {
            // Closing through the WinUI Window keeps MAUI's lifecycle hooks (DetachWindowActivationHook etc.) running so app teardown matches the standard system-button close path.
            if (windowsNativeWindow is not null)
            {
                windowsNativeWindow.Close();
            }
            else
            {
                windowsAppWindow?.Hide();
            }
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(MainPage), $"Close failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }

    private void WindowsAppWindow_Changed(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
    {
        if (args.DidPresenterChange || args.DidSizeChange)
        {
            Dispatcher.Dispatch(() =>
            {
                UpdateWindowsTitleBarMaximizedState();
                UpdateWindowsTitleBarDragRegion();
            });
        }
    }

    private void UpdateWindowsTitleBarMaximizedState()
    {
        if (windowsOverlappedPresenter is null)
        {
            return;
        }

        var maximized = windowsOverlappedPresenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized;
        if (maximized == windowsTitleBarMaximized)
        {
            return;
        }

        windowsTitleBarMaximized = maximized;
        var glyph = maximized ? WindowsRestoreGlyph : WindowsMaximizeGlyph;
        // Only overwrite when the runtime constant has content; otherwise leave the XAML-rendered glyph alone.
        if (!string.IsNullOrEmpty(glyph))
        {
            WindowMaximizeButtonGlyph.Text = glyph;
        }
    }

    private void UpdateWindowsTitleBarDragRegion()
    {
        try
        {
            if (windowsNativeWindow is null || windowsAppWindow is null)
            {
                return;
            }

            if (WindowTitleBarDragRegion.Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement dragElement)
            {
                return;
            }

            if (dragElement.XamlRoot is not { } xamlRoot)
            {
                return;
            }

            // MAUI's WindowRootViewContainer reserves a ~30px row at the top of the window for an OS title bar even when HasTitleBar=false. If we declare the Caption region at (0,0,...) it lands inside that phantom row instead of on our visually-rendered WindowTitleBarDragRegion, so dragging "works" above the caption buttons row instead of on it. Compute the drag rect from the element's actual position via TransformToVisual.
            var topLeft = dragElement.TransformToVisual(xamlRoot.Content)
                .TransformPoint(new global::Windows.Foundation.Point(0, 0));
            var scale = xamlRoot.RasterizationScale;
            var width = WindowTitleBarDragRegion.Width;
            var height = WindowTitleBarDragRegion.Height;
            if (height <= 0)
            {
                height = WindowTitleBar.Height;
            }
            if (height <= 0)
            {
                height = 30;
            }

            var dragWidthPx = (int)Math.Round(width * scale);
            var dragHeightPx = (int)Math.Round(height * scale);
            CrashFileLogger.WriteInfo(nameof(MainPage), $"[DragRegion] scale={scale:0.##} elementOrigin=({topLeft.X:0},{topLeft.Y:0}) size=({width:0}x{height:0}) → rectPx=({(int)Math.Round(topLeft.X * scale)},{(int)Math.Round(topLeft.Y * scale)},{dragWidthPx},{dragHeightPx})");

            var ncSource = Microsoft.UI.Input.InputNonClientPointerSource.GetForWindowId(windowsAppWindow.Id);
            if (dragWidthPx <= 0 || dragHeightPx <= 0)
            {
                ncSource.SetRegionRects(Microsoft.UI.Input.NonClientRegionKind.Caption, Array.Empty<global::Windows.Graphics.RectInt32>());
                return;
            }

            var dragRect = new global::Windows.Graphics.RectInt32(
                (int)Math.Round(topLeft.X * scale),
                (int)Math.Round(topLeft.Y * scale),
                dragWidthPx,
                dragHeightPx);
            ncSource.SetRegionRects(Microsoft.UI.Input.NonClientRegionKind.Caption, new[] { dragRect });
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning(nameof(MainPage), $"UpdateWindowsTitleBarDragRegion failed: {CrashFileLogger.SafeExceptionMessage(ex)}");
        }
    }
#endif
}
