#if WINDOWS
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinRT;
using WinRT.Interop;
using Praxis.Services;

namespace Praxis;

public partial class App
{
    private static int windowsBackdropDiagnosticsLogged;
    private static Microsoft.UI.Xaml.Controls.FontIcon? windowsMaximizeRestoreIcon;
    private static readonly ConditionalWeakTable<Microsoft.UI.Xaml.Window, WindowsDesktopAcrylicState> WindowsDesktopAcrylicStates = new();
    private static readonly WindowSubclassProc WindowsBackdropSubclassProc = WindowsBackdropWindowSubclassProc;
    private static readonly UIntPtr WindowsBackdropSubclassId = new(0x50725853u);
    private const string WindowsCustomChromeRootName = "PraxisWindowsCustomChromeRoot";

    static partial void ApplyPlatformWindowBackdrop(Microsoft.Maui.Controls.Window window)
    {
        ApplyWindowsWindowBackdrop(window);
    }

    private static void ApplyWindowsWindowBackdrop(Microsoft.Maui.Controls.Window window)
    {
        try
        {
            if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
            {
                return;
            }

            var hwnd = WindowNative.GetWindowHandle(nativeWindow);
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            ApplyWindowsContentChrome(nativeWindow);
            ApplyWindowsRootTransparency(nativeWindow);
            ApplyWindowsChromeTint(nativeWindow);
            ApplyWindowsTitleBarTransparency(nativeWindow);
            AttachWindowsBackdropActivationRefresh(nativeWindow);
            AttachWindowsBackdropSizeRefresh(nativeWindow);
            EnsureWindowsResizeEraseSuppression(nativeWindow, hwnd);
            if (!ApplyWindowsDesktopAcrylicController(nativeWindow))
            {
                ApplyWindowsAcrylicBackdrop(hwnd);
            }

            ApplyWindowsRoundedCorners(hwnd);
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to apply Windows glass backdrop: {safeMessage}");
        }
    }

    private static void AttachWindowsBackdropActivationRefresh(Microsoft.UI.Xaml.Window nativeWindow)
    {
        nativeWindow.Activated -= WindowsWindowOnActivated;
        nativeWindow.Activated += WindowsWindowOnActivated;
    }

    private static void AttachWindowsBackdropSizeRefresh(Microsoft.UI.Xaml.Window nativeWindow)
    {
        nativeWindow.SizeChanged -= WindowsWindowOnSizeChanged;
        nativeWindow.SizeChanged += WindowsWindowOnSizeChanged;
    }

    private static void WindowsWindowOnActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs e)
    {
        if (sender is not Microsoft.UI.Xaml.Window nativeWindow)
        {
            return;
        }

        try
        {
            RefreshWindowsBackdropAfterActivation(nativeWindow);
            QueueWindowsBackdropRefresh(nativeWindow);
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to refresh Windows backdrop after activation: {safeMessage}");
        }
    }

    private static void WindowsWindowOnSizeChanged(object sender, Microsoft.UI.Xaml.WindowSizeChangedEventArgs e)
    {
        if (sender is not Microsoft.UI.Xaml.Window nativeWindow)
        {
            return;
        }

        try
        {
            RefreshWindowsBackdropAfterResize(nativeWindow);
            QueueWindowsBackdropRefresh(nativeWindow);
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to refresh Windows backdrop after resize: {safeMessage}");
        }
    }

    private static void QueueWindowsBackdropRefresh(Microsoft.UI.Xaml.Window nativeWindow)
    {
        _ = nativeWindow.DispatcherQueue.TryEnqueue(() =>
        {
            TryRefreshWindowsBackdrop(nativeWindow, "queued activation/resize refresh");
            _ = nativeWindow.DispatcherQueue.TryEnqueue(() =>
                TryRefreshWindowsBackdrop(nativeWindow, "second queued activation/resize refresh"));
        });
    }

    private static void TryRefreshWindowsBackdrop(
        Microsoft.UI.Xaml.Window nativeWindow,
        string context)
    {
        try
        {
            RefreshWindowsBackdropAfterActivation(nativeWindow);
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to refresh Windows backdrop after {context}: {safeMessage}");
        }
    }

    private static void RefreshWindowsBackdropAfterActivation(Microsoft.UI.Xaml.Window nativeWindow)
    {
        UpdateWindowsMaximizeRestoreIcon(nativeWindow);
        ApplyWindowsChromeTint(nativeWindow);
        ApplyWindowsRootTransparency(nativeWindow);
        ApplyWindowsTitleBarTransparency(nativeWindow);
        var hwnd = WindowNative.GetWindowHandle(nativeWindow);
        if (hwnd != IntPtr.Zero)
        {
            EnsureWindowsResizeEraseSuppression(nativeWindow, hwnd);
            if (!ApplyWindowsDesktopAcrylicController(nativeWindow))
            {
                ApplyWindowsAcrylicBackdrop(hwnd);
            }

            EnableWindowsQuickAccessCaptionStyle(hwnd);
        }
    }

    private static void RefreshWindowsBackdropAfterResize(Microsoft.UI.Xaml.Window nativeWindow)
    {
        ApplyWindowsChromeTint(nativeWindow);
        ApplyWindowsRootTransparency(nativeWindow);
        var hwnd = WindowNative.GetWindowHandle(nativeWindow);
        if (hwnd != IntPtr.Zero)
        {
            EnsureWindowsResizeEraseSuppression(nativeWindow, hwnd);
            if (!ApplyWindowsDesktopAcrylicController(nativeWindow))
            {
                ApplyWindowsAcrylicBackdrop(hwnd);
            }

            EnableWindowsQuickAccessCaptionStyle(hwnd);
        }
    }

    private static void ApplyWindowsContentChrome(Microsoft.UI.Xaml.Window nativeWindow)
    {
        try
        {
            EnsureWindowsCustomChrome(nativeWindow);
            nativeWindow.ExtendsContentIntoTitleBar = true;
            nativeWindow.Title = string.Empty;
            nativeWindow.AppWindow.Title = string.Empty;
            if (nativeWindow.Content is Microsoft.UI.Xaml.Controls.Grid { Name: WindowsCustomChromeRootName } chromeRoot &&
                chromeRoot.FindName("PraxisWindowsCustomTitleBar") is UIElement titleBar)
            {
                nativeWindow.SetTitleBar(titleBar);
            }

            if (nativeWindow.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false);
            }

            var hwnd = WindowNative.GetWindowHandle(nativeWindow);
            if (hwnd != IntPtr.Zero)
            {
                EnableWindowsQuickAccessCaptionStyle(hwnd);
            }
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to extend Windows content into title bar: {safeMessage}");
        }
    }

    private static void EnsureWindowsCustomChrome(Microsoft.UI.Xaml.Window nativeWindow)
    {
        if (nativeWindow.Content is Microsoft.UI.Xaml.Controls.Grid { Name: WindowsCustomChromeRootName })
        {
            return;
        }

        if (nativeWindow.Content is not UIElement originalContent)
        {
            return;
        }

        var chromeRoot = new Microsoft.UI.Xaml.Controls.Grid
        {
            Name = WindowsCustomChromeRootName,
            Background = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0))
        };

        chromeRoot.Children.Add(originalContent);

        var titleBar = CreateWindowsCustomTitleBar(nativeWindow);
        chromeRoot.Children.Add(titleBar);
        nativeWindow.Content = chromeRoot;
    }

    private static Microsoft.UI.Xaml.Controls.Grid CreateWindowsCustomTitleBar(Microsoft.UI.Xaml.Window nativeWindow)
    {
        var titleBar = new Microsoft.UI.Xaml.Controls.Grid
        {
            Name = "PraxisWindowsCustomTitleBar",
            Height = 36,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Top,
            Background = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0))
        };

        titleBar.ColumnDefinitions.Add(new Microsoft.UI.Xaml.Controls.ColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star) });
        titleBar.ColumnDefinitions.Add(new Microsoft.UI.Xaml.Controls.ColumnDefinition { Width = Microsoft.UI.Xaml.GridLength.Auto });

        var buttons = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Right,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Top
        };
        Microsoft.UI.Xaml.Controls.Grid.SetColumn(buttons, 1);
        titleBar.Children.Add(buttons);

        buttons.Children.Add(CreateWindowsCaptionButton("\uE921", () => MinimizeWindowsWindow(nativeWindow)));
        var maximizeRestoreButton = CreateWindowsCaptionButton("\uE922", () => ToggleMaximizeRestoreWindowsWindow(nativeWindow));
        if (maximizeRestoreButton.Content is Microsoft.UI.Xaml.Controls.FontIcon maximizeRestoreIcon)
        {
            windowsMaximizeRestoreIcon = maximizeRestoreIcon;
            UpdateWindowsMaximizeRestoreIcon(nativeWindow);
        }

        buttons.Children.Add(maximizeRestoreButton);
        buttons.Children.Add(CreateWindowsCaptionButton("\uE8BB", nativeWindow.Close));

        return titleBar;
    }

    private static Microsoft.UI.Xaml.Controls.Button CreateWindowsCaptionButton(string glyph, Action action)
    {
        var foregroundBrush = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveWindowsCaptionForegroundColor());
        var icon = new Microsoft.UI.Xaml.Controls.FontIcon
        {
            Glyph = glyph,
            FontFamily = new global::Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
            FontSize = 10,
            Foreground = foregroundBrush
        };
        var button = new Microsoft.UI.Xaml.Controls.Button
        {
            Width = 46,
            Height = 36,
            IsTabStop = false,
            UseSystemFocusVisuals = false,
            Padding = new Microsoft.UI.Xaml.Thickness(0),
            BorderThickness = new Microsoft.UI.Xaml.Thickness(0),
            Background = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0)),
            BorderBrush = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0)),
            Foreground = foregroundBrush,
            Content = icon
        };
        ApplyWindowsCaptionButtonResources(button);

        button.Click += (_, _) => action();
        button.PointerEntered += (_, _) => ApplyWindowsCaptionButtonState(button, pressed: false, hovered: true);
        button.PointerExited += (_, _) => ApplyWindowsCaptionButtonState(button, pressed: false, hovered: false);
        button.PointerPressed += (_, _) => ApplyWindowsCaptionButtonState(button, pressed: true, hovered: true);
        button.PointerReleased += (_, _) => ApplyWindowsCaptionButtonState(button, pressed: false, hovered: false);
        button.PointerCanceled += (_, _) => ApplyWindowsCaptionButtonState(button, pressed: false, hovered: false);
        button.PointerCaptureLost += (_, _) => ApplyWindowsCaptionButtonState(button, pressed: false, hovered: false);
        button.LostFocus += (_, _) => ApplyWindowsCaptionButtonState(button, pressed: false, hovered: false);
        return button;
    }

    private static void ApplyWindowsCaptionButtonResources(Microsoft.UI.Xaml.Controls.Button button)
    {
        var normalBackground = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0));
        var hoverBackground = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveWindowsCaptionHoverBackgroundColor());
        var pressedBackground = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveWindowsCaptionPressedBackgroundColor());
        var normalForeground = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveWindowsCaptionForegroundColor());
        var pressedForeground = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveWindowsCaptionPressedForegroundColor());

        button.Resources["ButtonBackground"] = normalBackground;
        button.Resources["ButtonBackgroundPointerOver"] = hoverBackground;
        button.Resources["ButtonBackgroundPressed"] = pressedBackground;
        button.Resources["ButtonBackgroundDisabled"] = normalBackground;
        button.Resources["ButtonForeground"] = normalForeground;
        button.Resources["ButtonForegroundPointerOver"] = normalForeground;
        button.Resources["ButtonForegroundPressed"] = pressedForeground;
        button.Resources["ButtonForegroundDisabled"] = normalForeground;
        button.Resources["ButtonBorderBrush"] = normalBackground;
        button.Resources["ButtonBorderBrushPointerOver"] = hoverBackground;
        button.Resources["ButtonBorderBrushPressed"] = pressedBackground;
        button.Resources["ButtonBorderBrushDisabled"] = normalBackground;
    }

    private static void ApplyWindowsCaptionButtonState(Microsoft.UI.Xaml.Controls.Button button, bool pressed, bool hovered)
    {
        var background = pressed
            ? ResolveWindowsCaptionPressedBackgroundColor()
            : hovered
                ? ResolveWindowsCaptionHoverBackgroundColor()
                : global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
        var foreground = pressed
            ? ResolveWindowsCaptionPressedForegroundColor()
            : ResolveWindowsCaptionForegroundColor();

        var foregroundBrush = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(foreground);
        button.Background = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(background);
        button.Foreground = foregroundBrush;
        if (button.Content is Microsoft.UI.Xaml.Controls.FontIcon icon)
        {
            icon.Foreground = foregroundBrush;
        }
    }

    private static void MinimizeWindowsWindow(Microsoft.UI.Xaml.Window nativeWindow)
    {
        if (nativeWindow.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Minimize();
        }
    }

    private static void ToggleMaximizeRestoreWindowsWindow(Microsoft.UI.Xaml.Window nativeWindow)
    {
        if (nativeWindow.AppWindow.Presenter is not OverlappedPresenter presenter)
        {
            return;
        }

        if (presenter.State == OverlappedPresenterState.Maximized)
        {
            presenter.Restore();
        }
        else
        {
            presenter.Maximize();
        }

        UpdateWindowsMaximizeRestoreIcon(nativeWindow);
    }

    private static void UpdateWindowsMaximizeRestoreIcon(Microsoft.UI.Xaml.Window nativeWindow)
    {
        if (windowsMaximizeRestoreIcon is null ||
            nativeWindow.AppWindow.Presenter is not OverlappedPresenter presenter)
        {
            return;
        }

        windowsMaximizeRestoreIcon.Glyph = presenter.State == OverlappedPresenterState.Maximized
            ? "\uE923"
            : "\uE922";
    }

    private static void ApplyWindowsChromeTint(Microsoft.UI.Xaml.Window nativeWindow)
    {
        var tintBrush = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveWindowsChromeTintColor());
        var transparentBrush = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0));
        if (nativeWindow.Content is Microsoft.UI.Xaml.Controls.Grid { Name: WindowsCustomChromeRootName } chromeRoot)
        {
            chromeRoot.Background = tintBrush;
            if (chromeRoot.FindName("PraxisWindowsCustomTitleBar") is Microsoft.UI.Xaml.Controls.Grid titleBar)
            {
                titleBar.Background = transparentBrush;
                UpdateWindowsCaptionButtonForegrounds(titleBar);
            }
        }
    }

    private static void UpdateWindowsCaptionButtonForegrounds(Microsoft.UI.Xaml.DependencyObject root)
    {
        var foregroundBrush = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(ResolveWindowsCaptionForegroundColor());
        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is Microsoft.UI.Xaml.Controls.Button button)
            {
                ApplyWindowsCaptionButtonResources(button);
                button.Foreground = foregroundBrush;
            }
            else if (child is Microsoft.UI.Xaml.Controls.FontIcon icon)
            {
                icon.Foreground = foregroundBrush;
            }

            UpdateWindowsCaptionButtonForegrounds(child);
        }
    }

    private static global::Windows.UI.Color ResolveWindowsChromeTintColor()
    {
        return IsWindowsDarkThemeActive()
            ? global::Windows.UI.Color.FromArgb(96, 0, 0, 0)
            : global::Windows.UI.Color.FromArgb(96, 255, 255, 255);
    }

    private static int ResolveWindowsAcrylicGradientColor()
    {
        var color = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
        return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
    }

    private static bool IsWindowsDarkThemeActive()
    {
        return Microsoft.Maui.Controls.Application.Current?.UserAppTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark ||
            Microsoft.Maui.Controls.Application.Current?.UserAppTheme == Microsoft.Maui.ApplicationModel.AppTheme.Unspecified &&
            Microsoft.Maui.Controls.Application.Current?.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark;
    }

    private static global::Windows.UI.Color ResolveWindowsCaptionForegroundColor()
    {
        return IsWindowsDarkThemeActive()
            ? global::Windows.UI.Color.FromArgb(238, 245, 247, 250)
            : global::Windows.UI.Color.FromArgb(230, 16, 18, 22);
    }

    private static global::Windows.UI.Color ResolveWindowsCaptionHoverBackgroundColor()
    {
        return IsWindowsDarkThemeActive()
            ? global::Windows.UI.Color.FromArgb(255, 51, 51, 51)
            : global::Windows.UI.Color.FromArgb(30, 0, 0, 0);
    }

    private static global::Windows.UI.Color ResolveWindowsCaptionPressedBackgroundColor()
    {
        return IsWindowsDarkThemeActive()
            ? global::Windows.UI.Color.FromArgb(255, 255, 255, 255)
            : global::Windows.UI.Color.FromArgb(48, 0, 0, 0);
    }

    private static global::Windows.UI.Color ResolveWindowsCaptionPressedForegroundColor()
    {
        return IsWindowsDarkThemeActive()
            ? global::Windows.UI.Color.FromArgb(255, 0, 0, 0)
            : global::Windows.UI.Color.FromArgb(230, 16, 18, 22);
    }

    private static void ApplyWindowsRootTransparency(Microsoft.UI.Xaml.Window nativeWindow)
    {
        var transparentBrush = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0));

        if (nativeWindow.Content is Microsoft.UI.Xaml.Controls.Panel panel &&
            panel is not Microsoft.UI.Xaml.Controls.Grid { Name: WindowsCustomChromeRootName })
        {
            panel.Background = transparentBrush;
        }

        if (nativeWindow.Content is Microsoft.UI.Xaml.Controls.Control control)
        {
            control.Background = transparentBrush;
        }

        if (nativeWindow.Content is Microsoft.UI.Xaml.Controls.Border border)
        {
            border.Background = transparentBrush;
        }

        if (nativeWindow.Content is Microsoft.UI.Xaml.UIElement rootElement)
        {
            RefreshWindowsRootTransparency(rootElement);
            AttachWindowsRootTransparencyRefresh(nativeWindow, rootElement);
        }
    }

    private static void AttachWindowsRootTransparencyRefresh(
        Microsoft.UI.Xaml.Window nativeWindow,
        Microsoft.UI.Xaml.UIElement rootElement)
    {
        _ = nativeWindow.DispatcherQueue.TryEnqueue(() => RefreshWindowsRootTransparency(rootElement));
        _ = nativeWindow.DispatcherQueue.TryEnqueue(() =>
        {
            RefreshWindowsRootTransparency(rootElement);
            _ = nativeWindow.DispatcherQueue.TryEnqueue(() => RefreshWindowsRootTransparency(rootElement));
        });

        if (rootElement is Microsoft.UI.Xaml.FrameworkElement frameworkElement)
        {
            frameworkElement.Loaded -= WindowsRootElementOnLoaded;
            frameworkElement.Loaded += WindowsRootElementOnLoaded;
            frameworkElement.SizeChanged -= WindowsRootElementOnSizeChanged;
            frameworkElement.SizeChanged += WindowsRootElementOnSizeChanged;
        }
    }

    private static void WindowsRootElementOnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.UIElement rootElement)
        {
            RefreshWindowsRootTransparency(rootElement);
        }
    }

    private static void WindowsRootElementOnSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.UIElement rootElement)
        {
            RefreshWindowsRootTransparency(rootElement);
        }
    }

    private static void RefreshWindowsRootTransparency(Microsoft.UI.Xaml.UIElement rootElement)
    {
        LogWindowsBackdropDiagnostics(rootElement);
        ClearWindowsOpaqueWrapperBackgrounds(rootElement);
    }

    private static void ClearWindowsOpaqueWrapperBackgrounds(Microsoft.UI.Xaml.DependencyObject root)
    {
        var transparentBrush = new global::Microsoft.UI.Xaml.Media.SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0));
        var visited = new HashSet<Microsoft.UI.Xaml.DependencyObject>();
        var rootSize = ResolveWindowsElementSize(root);
        ClearWindowsOpaqueWrapperBackgrounds(root, transparentBrush, visited, rootSize.width, rootSize.height);
    }

    private static void ClearWindowsOpaqueWrapperBackgrounds(
        Microsoft.UI.Xaml.DependencyObject element,
        global::Microsoft.UI.Xaml.Media.Brush transparentBrush,
        ISet<Microsoft.UI.Xaml.DependencyObject> visited,
        double rootWidth,
        double rootHeight)
    {
        if (!visited.Add(element))
        {
            return;
        }

        ClearWindowsOpaqueWrapperBackground(element, transparentBrush, rootWidth, rootHeight);

        var childCount = VisualTreeHelper.GetChildrenCount(element);
        for (var index = 0; index < childCount; index++)
        {
            ClearWindowsOpaqueWrapperBackgrounds(VisualTreeHelper.GetChild(element, index), transparentBrush, visited, rootWidth, rootHeight);
        }
    }

    private static void ClearWindowsOpaqueWrapperBackground(
        Microsoft.UI.Xaml.DependencyObject element,
        global::Microsoft.UI.Xaml.Media.Brush transparentBrush,
        double rootWidth,
        double rootHeight)
    {
        if (element is Microsoft.UI.Xaml.Controls.Grid { Name: WindowsCustomChromeRootName })
        {
            return;
        }

        var coversRoot = CoversMostOfWindowsRoot(element, rootWidth, rootHeight);
        if (element is Microsoft.UI.Xaml.Controls.Panel panel &&
            (coversRoot || IsWindowsBackdropBlockingBrush(panel.Background, element, rootWidth, rootHeight)))
        {
            panel.Background = transparentBrush;
        }

        if (element is Microsoft.UI.Xaml.Controls.Control control &&
            (coversRoot || IsWindowsBackdropBlockingBrush(control.Background, element, rootWidth, rootHeight)))
        {
            control.Background = transparentBrush;
        }

        if (element is Microsoft.UI.Xaml.Controls.Border border &&
            (coversRoot || IsWindowsBackdropBlockingBrush(border.Background, element, rootWidth, rootHeight)))
        {
            border.Background = transparentBrush;
        }
    }

    private static bool IsWindowsBackdropBlockingBrush(
        global::Microsoft.UI.Xaml.Media.Brush? brush,
        Microsoft.UI.Xaml.DependencyObject element,
        double rootWidth,
        double rootHeight)
    {
        if (brush is not global::Microsoft.UI.Xaml.Media.SolidColorBrush solid)
        {
            return false;
        }

        var color = solid.Color;
        if (color.A == 0)
        {
            return false;
        }

        var max = Math.Max(color.R, Math.Max(color.G, color.B));
        var min = Math.Min(color.R, Math.Min(color.G, color.B));
        return CoversMostOfWindowsRoot(element, rootWidth, rootHeight) ||
            color.A >= 240 && max - min <= 36 && min >= 218;
    }

    private static bool CoversMostOfWindowsRoot(Microsoft.UI.Xaml.DependencyObject element, double rootWidth, double rootHeight)
    {
        if (rootWidth <= 0d || rootHeight <= 0d || element is not Microsoft.UI.Xaml.FrameworkElement frameworkElement)
        {
            return false;
        }

        return frameworkElement.ActualWidth >= rootWidth * 0.8d &&
            frameworkElement.ActualHeight >= rootHeight * 0.8d;
    }

    private static (double width, double height) ResolveWindowsElementSize(Microsoft.UI.Xaml.DependencyObject element)
    {
        return element is Microsoft.UI.Xaml.FrameworkElement frameworkElement
            ? (frameworkElement.ActualWidth, frameworkElement.ActualHeight)
            : (0d, 0d);
    }

    private static void LogWindowsBackdropDiagnostics(Microsoft.UI.Xaml.DependencyObject root)
    {
        var rootSize = ResolveWindowsElementSize(root);
        if (rootSize.width <= 0d || rootSize.height <= 0d)
        {
            return;
        }

        if (Interlocked.Exchange(ref windowsBackdropDiagnosticsLogged, 1) != 0)
        {
            return;
        }

        try
        {
            var sb = new StringBuilder();
            sb.Append($"WindowsBackdrop visual tree: root={root.GetType().FullName}; rootSize={rootSize.width:0.##}x{rootSize.height:0.##}");
            AppendWindowsBackdropDiagnostics(root, rootSize.width, rootSize.height, sb, depth: 0, maxDepth: 7);
            CrashFileLogger.WriteInfo(nameof(App), sb.ToString());
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to log Windows backdrop diagnostics: {safeMessage}");
        }
    }

    private static void AppendWindowsBackdropDiagnostics(
        Microsoft.UI.Xaml.DependencyObject element,
        double rootWidth,
        double rootHeight,
        StringBuilder sb,
        int depth,
        int maxDepth)
    {
        if (depth > maxDepth)
        {
            return;
        }

        var brush = ResolveWindowsElementBrush(element);
        var size = ResolveWindowsElementSize(element);
        if (brush is not null || CoversMostOfWindowsRoot(element, rootWidth, rootHeight))
        {
            sb.Append(" | ");
            sb.Append(depth);
            sb.Append(':');
            sb.Append(element.GetType().FullName);
            sb.Append('[');
            sb.Append(size.width.ToString("0.##"));
            sb.Append('x');
            sb.Append(size.height.ToString("0.##"));
            sb.Append("] bg=");
            sb.Append(DescribeWindowsBrush(brush));
        }

        var childCount = VisualTreeHelper.GetChildrenCount(element);
        for (var index = 0; index < childCount; index++)
        {
            AppendWindowsBackdropDiagnostics(VisualTreeHelper.GetChild(element, index), rootWidth, rootHeight, sb, depth + 1, maxDepth);
        }
    }

    private static global::Microsoft.UI.Xaml.Media.Brush? ResolveWindowsElementBrush(Microsoft.UI.Xaml.DependencyObject element)
    {
        return element switch
        {
            Microsoft.UI.Xaml.Controls.Panel panel => panel.Background,
            Microsoft.UI.Xaml.Controls.Control control => control.Background,
            Microsoft.UI.Xaml.Controls.Border border => border.Background,
            _ => null
        };
    }

    private static string DescribeWindowsBrush(global::Microsoft.UI.Xaml.Media.Brush? brush)
    {
        return brush switch
        {
            null => "(null)",
            global::Microsoft.UI.Xaml.Media.SolidColorBrush solid => $"#{solid.Color.A:X2}{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}",
            _ => brush.GetType().FullName ?? brush.GetType().Name
        };
    }

    private static void ApplyWindowsTitleBarTransparency(Microsoft.UI.Xaml.Window nativeWindow)
    {
        try
        {
            ApplyWindowsChromeTint(nativeWindow);
            var transparent = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
            var captionForeground = ResolveWindowsCaptionForegroundColor();
            var captionHoverBackground = ResolveWindowsCaptionHoverBackgroundColor();
            var captionPressedBackground = ResolveWindowsCaptionPressedBackgroundColor();
            var captionPressedForeground = ResolveWindowsCaptionPressedForegroundColor();
            var titleBar = nativeWindow.AppWindow.TitleBar;
            titleBar.BackgroundColor = transparent;
            titleBar.InactiveBackgroundColor = transparent;
            titleBar.ForegroundColor = captionForeground;
            titleBar.InactiveForegroundColor = captionForeground;
            titleBar.ButtonBackgroundColor = transparent;
            titleBar.ButtonInactiveBackgroundColor = transparent;
            titleBar.ButtonForegroundColor = captionForeground;
            titleBar.ButtonInactiveForegroundColor = captionForeground;
            titleBar.ButtonHoverBackgroundColor = captionHoverBackground;
            titleBar.ButtonHoverForegroundColor = captionForeground;
            titleBar.ButtonPressedBackgroundColor = captionPressedBackground;
            titleBar.ButtonPressedForegroundColor = captionPressedForeground;
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to make Windows title bar transparent: {safeMessage}");
        }
    }

    private static void EnsureWindowsResizeEraseSuppression(Microsoft.UI.Xaml.Window nativeWindow, IntPtr hwnd)
    {
        var state = WindowsDesktopAcrylicStates.GetValue(nativeWindow, CreateWindowsDesktopAcrylicState);
        if (state.ResizeEraseSuppressionHwnd == hwnd)
        {
            return;
        }

        if (!SetWindowSubclass(hwnd, WindowsBackdropSubclassProc, WindowsBackdropSubclassId, UIntPtr.Zero))
        {
            var error = Marshal.GetLastWin32Error();
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to install Windows resize erase suppression. Win32Error={error}");
            return;
        }

        state.ResizeEraseSuppressionHwnd = hwnd;
    }

    private static IntPtr WindowsBackdropWindowSubclassProc(
        IntPtr hwnd,
        uint message,
        IntPtr wParam,
        IntPtr lParam,
        UIntPtr subclassId,
        UIntPtr refData)
    {
        _ = refData;
        if (message == WM_ERASEBKGND)
        {
            FillWindowsResizeFallbackBackground(hwnd, wParam);
            return new IntPtr(1);
        }

        if (message is WM_SIZE or WM_SIZING or WM_WINDOWPOSCHANGING or WM_WINDOWPOSCHANGED)
        {
            ApplyWindowsAcrylicBackdrop(hwnd);
            InvalidateRect(hwnd, IntPtr.Zero, false);
        }

        if (message == WM_NCDESTROY)
        {
            RemoveWindowSubclass(hwnd, WindowsBackdropSubclassProc, subclassId);
        }

        return DefSubclassProc(hwnd, message, wParam, lParam);
    }

    private static bool ApplyWindowsDesktopAcrylicController(Microsoft.UI.Xaml.Window nativeWindow)
    {
        try
        {
            if (!DesktopAcrylicController.IsSupported())
            {
                return false;
            }

            if (nativeWindow.SystemBackdrop is not null)
            {
                nativeWindow.SystemBackdrop = null;
            }

            var state = WindowsDesktopAcrylicStates.GetValue(nativeWindow, CreateWindowsDesktopAcrylicState);
            UpdateWindowsDesktopAcrylicState(state);
            if (state.Target is null)
            {
                state.Target = nativeWindow.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>();
                state.Controller.AddSystemBackdropTarget(state.Target);
            }

            state.Controller.SetSystemBackdropConfiguration(state.Configuration);
            return true;
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(App), $"Failed to apply Windows desktop acrylic controller: {safeMessage}");
            return false;
        }
    }

    private static WindowsDesktopAcrylicState CreateWindowsDesktopAcrylicState(Microsoft.UI.Xaml.Window nativeWindow)
    {
        _ = nativeWindow;
        return new WindowsDesktopAcrylicState(
            new DesktopAcrylicController(),
            new SystemBackdropConfiguration());
    }

    private static void UpdateWindowsDesktopAcrylicState(WindowsDesktopAcrylicState state)
    {
        var isDark = IsWindowsDarkThemeActive();
        state.Configuration.IsInputActive = true;
        state.Configuration.Theme = isDark
            ? SystemBackdropTheme.Dark
            : SystemBackdropTheme.Light;
        state.Controller.TintColor = isDark
            ? global::Windows.UI.Color.FromArgb(255, 0, 0, 0)
            : global::Windows.UI.Color.FromArgb(255, 255, 255, 255);
        state.Controller.TintOpacity = isDark ? 0.04f : 0.02f;
        state.Controller.LuminosityOpacity = isDark ? 0.18f : 0.12f;
    }

    private sealed class WindowsDesktopAcrylicState(
        DesktopAcrylicController controller,
        SystemBackdropConfiguration configuration)
    {
        public DesktopAcrylicController Controller { get; } = controller;
        public SystemBackdropConfiguration Configuration { get; } = configuration;
        public Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop? Target { get; set; }
        public IntPtr ResizeEraseSuppressionHwnd { get; set; }
    }

    private static void ApplyWindowsAcrylicBackdrop(IntPtr hwnd)
    {
        var gradientColor = ResolveWindowsAcrylicGradientColor();
        var accent = new AccentPolicy
        {
            AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            AccentFlags = 0,
            GradientColor = gradientColor,
            AnimationId = 0
        };

        var accentStructSize = Marshal.SizeOf(accent);
        var accentPtr = Marshal.AllocHGlobal(accentStructSize);
        try
        {
            Marshal.StructureToPtr(accent, accentPtr, false);
            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };
            var result = SetWindowCompositionAttribute(hwnd, ref data);
            if (result == 0)
            {
                CrashFileLogger.WriteWarning(nameof(App), "SetWindowCompositionAttribute did not apply Windows acrylic blur.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(accentPtr);
        }
    }

    private static void FillWindowsResizeFallbackBackground(IntPtr hwnd, IntPtr hdc)
    {
        if (hdc == IntPtr.Zero || !GetClientRect(hwnd, out var rect))
        {
            return;
        }

        var brush = CreateSolidBrush(ToColorRef(ResolveWindowsResizeFallbackColor()));
        if (brush == IntPtr.Zero)
        {
            return;
        }

        try
        {
            FillRect(hdc, ref rect, brush);
        }
        finally
        {
            DeleteObject(brush);
        }
    }

    private static global::Windows.UI.Color ResolveWindowsResizeFallbackColor()
    {
        return IsWindowsDarkThemeActive()
            ? global::Windows.UI.Color.FromArgb(255, 20, 24, 29)
            : global::Windows.UI.Color.FromArgb(255, 229, 233, 238);
    }

    private static uint ToColorRef(global::Windows.UI.Color color)
    {
        return (uint)(color.R | color.G << 8 | color.B << 16);
    }

    private static void EnableWindowsQuickAccessCaptionStyle(IntPtr hwnd)
    {
        var style = GetWindowLongPtr(hwnd, GWL_STYLE);
        var updatedStyle = new IntPtr(style.ToInt64() | WS_CAPTION);
        if (updatedStyle != style)
        {
            SetWindowLongPtr(hwnd, GWL_STYLE, updatedStyle);
        }
    }

    private static void ApplyWindowsRoundedCorners(IntPtr hwnd)
    {
        var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(uint));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    internal enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_ENABLE_HOSTBACKDROP = 5,
        ACCENT_ENABLE_ACRYLIC = 6,
        ACCENT_ENABLE_HOSTBACKDROP_ACRYLIC = 7
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    internal enum WindowCompositionAttribute
    {
        WCA_ACCENT_POLICY = 19
    }

    public enum DWMWINDOWATTRIBUTE
    {
        DWMWA_WINDOW_CORNER_PREFERENCE = 33
    }

    public enum DWM_WINDOW_CORNER_PREFERENCE
    {
        DWMWCP_DEFAULT = 0,
        DWMWCP_DONOTROUND = 1,
        DWMWCP_ROUND = 2,
        DWMWCP_ROUNDSMALL = 3
    }

    private const int GWL_STYLE = -16;
    private const uint WM_SIZE = 0x0005;
    private const uint WM_ERASEBKGND = 0x0014;
    private const uint WM_WINDOWPOSCHANGING = 0x0046;
    private const uint WM_WINDOWPOSCHANGED = 0x0047;
    private const uint WM_NCDESTROY = 0x0082;
    private const uint WM_SIZING = 0x0214;
    private const long WS_BORDER = 0x00800000L;
    private const long WS_DLGFRAME = 0x00400000L;
    private const long WS_CAPTION = WS_BORDER | WS_DLGFRAME;

    private delegate IntPtr WindowSubclassProc(
        IntPtr hwnd,
        uint message,
        IntPtr wParam,
        IntPtr lParam,
        UIntPtr subclassId,
        UIntPtr refData);

    [DllImport("user32.dll")]
    internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    internal static extern void DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute, uint cbAttribute);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    internal static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    internal static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(
        IntPtr hwnd,
        WindowSubclassProc subclassProc,
        UIntPtr subclassId,
        UIntPtr refData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool RemoveWindowSubclass(
        IntPtr hwnd,
        WindowSubclassProc subclassProc,
        UIntPtr subclassId);

    [DllImport("comctl32.dll")]
    private static extern IntPtr DefSubclassProc(
        IntPtr hwnd,
        uint message,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern int FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(uint colorRef);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
}
#endif
