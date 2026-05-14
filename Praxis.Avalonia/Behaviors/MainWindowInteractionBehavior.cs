using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Praxis.Avalonia.ViewModels;
using Praxis.Core.Models;

namespace Praxis.Avalonia.Behaviors;

public sealed class MainWindowInteractionBehavior
{
    private const string CoreGraphicsLibrary = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string DwmApiLibrary = "dwmapi.dll";
    private const string Shell32Library = "shell32.dll";
    private const string User32Library = "user32.dll";
    private static readonly Guid PropertyStoreInterfaceId = new("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
    private static readonly PROPERTYKEY AppUserModelRelaunchIconResourceKey = new()
    {
        FormatId = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
        PropertyId = 3,
    };
    private static readonly PROPERTYKEY AppUserModelIdKey = new()
    {
        FormatId = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
        PropertyId = 5,
    };
    private const string WindowsAppUserModelId = "Widthdom.Praxis.Avalonia";
    private const int DwmWindowCornerPreference = 33;
    private const int DwmWindowCornerPreferenceDefault = 0;
    private const int DwmWindowCornerPreferenceRound = 2;
    private const int IconSmall = 0;
    private const int IconBig = 1;
    private const int IconSmall2 = 2;
    private const int ImageIcon = 1;
    private const int LoadFromFile = 0x00000010;
    private const int ShcneAssocChanged = 0x08000000;
    private const int ShcnfIdList = 0x0000;
    private const int WmSetIcon = 0x0080;
    private const int WmNcHitTest = 0x0084;
    private const int HtCaption = 2;
    private const int GclpHIcon = -14;
    private const int GclpHIconSmall = -34;
    private const int GwlStyle = -16;
    private const int GwlpWndProc = -4;
    private const int WsThickFrame = 0x00040000;
    private const int WsMinimizeBox = 0x00020000;
    private const int WsMaximizeBox = 0x00010000;
    private const double WindowsCornerRadiusDip = 10;
    private const double WindowsCaptionHitHeightDip = 32;
    private const double WindowsCaptionButtonWidthDip = 108;
    private const double DragThreshold = 3;
    private const int MacEdgeSnapThresholdPixels = 16;
    private const double MacCornerSnapZoneRatio = 0.30;
    private const int MacEdgeSnapRetryLimit = 50;
    private const int MacNormalMaximizeTolerancePixels = 12;

    private readonly Window window;
    private Canvas? placementSurface;
    private ScrollViewer? placementScroll;
    private Border? selectionRect;
    private Border? statusBar;
    private Border? copyToast;
    private Grid? shellContent;
    private Grid? topInputRow;
    private Border? commandSuggestionPanel;
    private Border? placementFrame;
    private ScrollViewer? dockScroll;
    private TextBox? modalGuidEntry;
    private TextBox? modalButtonTextEntry;
    private TextBox? modalCommandEntry;
    private TextBox? modalToolEntry;
    private TextBox? modalArgumentsEntry;
    private TextBox? modalClipWordEditor;
    private TextBox? modalNoteEditor;
    private Button? modalCancelButton;
    private Button? modalSaveButton;
    private Grid? contextMenuOverlay;
    private Grid? editorOverlay;
    private Button? macMinimizeButton;
    private Button? windowsMaximizeButton;
    private TextBlock? windowsMaximizeGlyph;
    private TextBlock? windowsMaximizeTooltip;
    private Button? contextEditButton;
    private Button? contextDeleteButton;
    private Button? conflictReloadButton;
    private Button? conflictOverwriteButton;
    private Button? conflictCancelButton;
    private Point launcherDragStart;
    private Point selectionStart;
    private LauncherButtonModel? draggedButton;
    private bool isLauncherDragActive;
    private bool launcherDragMoved;
    private bool isSelectionActive;
    private bool isEnteringMacFullScreen;
    private bool isMoveDragArmedForSnap;
    private bool isMoveDragInProgress;
    private bool isApplyingEdgeSnap;
    private int edgeSnapRetryCount;
    private Point? moveDragCompletedMousePosition;
    private DispatcherTimer? edgeSnapTimer;
    private DispatcherTimer? statusDismissTimer;
    private DispatcherTimer? copyToastDismissTimer;
    private DispatcherTimer? copyToastHideTimer;
    private StatusModel? observedStatus;
    private MainWindowViewModel? observedModel;
    private Border? activeResizeGrip;
    private WindowEdge activeResizeEdge;
    private Point resizeStartPointer;
    private PixelPoint resizeStartPosition;
    private double resizeStartWidth;
    private double resizeStartHeight;
    private double resizeScaling = 1;
    private PixelPoint? normalPositionBeforeFullScreen;
    private double? normalWidthBeforeFullScreen;
    private double? normalHeightBeforeFullScreen;
    private PixelPoint? normalPositionBeforeMaximize;
    private double? normalWidthBeforeMaximize;
    private double? normalHeightBeforeMaximize;
    private WindowBounds? macNormalZoomRestoreBounds;
    private double? defaultMinWidth;
    private double? defaultMinHeight;
    private IntPtr windowsSmallIcon;
    private IntPtr windowsBigIcon;
    private IntPtr originalWindowsWndProc;
    private WndProcDelegate? windowsWndProc;

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<MainWindowInteractionBehavior, Window, bool>("IsEnabled");

    public static bool GetIsEnabled(Window window)
        => window.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Window window, bool value)
        => window.SetValue(IsEnabledProperty, value);

    public static void ToggleMacFullScreen(Window window)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        if (window.WindowState == WindowState.FullScreen)
        {
            window.WindowState = WindowState.Normal;
            return;
        }

        if (window.GetValue(BehaviorInstanceProperty) is not { } behavior)
        {
            window.WindowState = WindowState.FullScreen;
            return;
        }

        behavior.EnterMacFullScreenAsync();
    }

    public static void NotifyMoveDragStarted(Window window)
    {
        if (window.GetValue(BehaviorInstanceProperty) is { } behavior)
        {
            behavior.ArmMoveDragSnap();
        }
    }

    public static void NotifyMoveDragCompleted(Window window, Point globalMousePosition)
    {
        if (window.GetValue(BehaviorInstanceProperty) is { } behavior)
        {
            behavior.CompleteMoveDragSnap(globalMousePosition);
        }
    }

    public static void CaptureNormalBoundsBeforeMaximize(Window window)
    {
        if (window.WindowState != WindowState.Normal
            || window.GetValue(BehaviorInstanceProperty) is not { } behavior)
        {
            return;
        }

        behavior.CaptureNormalBoundsBeforeMaximize();
    }

    public static void ToggleMacNormalMaximize(Window window)
    {
        if (!OperatingSystem.IsMacOS())
        {
            if (window.WindowState == WindowState.Normal)
            {
                CaptureNormalBoundsBeforeMaximize(window);
            }

            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            return;
        }

        if (window.GetValue(BehaviorInstanceProperty) is { } behavior)
        {
            behavior.ToggleMacNormalMaximize();
        }
    }

    static MainWindowInteractionBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<Window>(OnIsEnabledChanged);
    }

    private MainWindowInteractionBehavior(Window window)
    {
        this.window = window;
    }

    private MainWindowViewModel? ViewModel => window.DataContext as MainWindowViewModel;

    private static void OnIsEnabledChanged(Window window, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.GetNewValue<bool>())
        {
            var behavior = new MainWindowInteractionBehavior(window);
            window.SetValue(BehaviorInstanceProperty, behavior);
            behavior.Attach();
            return;
        }

        if (window.GetValue(BehaviorInstanceProperty) is { } existing)
        {
            existing.Detach();
            window.ClearValue(BehaviorInstanceProperty);
        }
    }

    private static readonly AttachedProperty<MainWindowInteractionBehavior?> BehaviorInstanceProperty =
        AvaloniaProperty.RegisterAttached<MainWindowInteractionBehavior, Window, MainWindowInteractionBehavior?>("BehaviorInstance");

    private void Attach()
    {
        ApplyWindowsWindowChromeHints();
        window.Opened += WindowOnOpened;
        window.Closed += WindowOnClosed;
        window.SizeChanged += WindowOnSizeChanged;
        window.PositionChanged += WindowOnPositionChanged;
        window.DataContextChanged += WindowOnDataContextChanged;
        window.PropertyChanged += WindowOnPropertyChanged;
        if (Application.Current is not null)
        {
            Application.Current.ActualThemeVariantChanged += ApplicationOnActualThemeVariantChanged;
        }

        window.AddHandler(InputElement.KeyDownEvent, Window_KeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        window.AddHandler(InputElement.PointerPressedEvent, Window_PointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        window.AddHandler(InputElement.PointerMovedEvent, Window_PointerMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        window.AddHandler(InputElement.PointerReleasedEvent, Window_PointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        window.AddHandler(Button.ClickEvent, CopyButton_Click, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void Detach()
    {
        UnobserveModel();
        window.Opened -= WindowOnOpened;
        window.Closed -= WindowOnClosed;
        window.SizeChanged -= WindowOnSizeChanged;
        window.PositionChanged -= WindowOnPositionChanged;
        window.DataContextChanged -= WindowOnDataContextChanged;
        window.PropertyChanged -= WindowOnPropertyChanged;
        if (Application.Current is not null)
        {
            Application.Current.ActualThemeVariantChanged -= ApplicationOnActualThemeVariantChanged;
        }

        window.RemoveHandler(InputElement.KeyDownEvent, Window_KeyDown);
        window.RemoveHandler(InputElement.PointerPressedEvent, Window_PointerPressed);
        window.RemoveHandler(InputElement.PointerMovedEvent, Window_PointerMoved);
        window.RemoveHandler(InputElement.PointerReleasedEvent, Window_PointerReleased);
        window.RemoveHandler(Button.ClickEvent, CopyButton_Click);
    }

    private void WindowOnOpened(object? sender, EventArgs e)
    {
        ConfigurePlatformCaptionButtons();
        placementSurface = window.FindControl<Canvas>("PlacementSurface");
        placementScroll = window.FindControl<ScrollViewer>("PlacementScroll");
        selectionRect = window.FindControl<Border>("SelectionRect");
        statusBar = window.FindControl<Border>("StatusBar");
        copyToast = window.FindControl<Border>("CopyToast");
        shellContent = window.FindControl<Grid>("ShellContent");
        topInputRow = window.FindControl<Grid>("TopInputRow");
        commandSuggestionPanel = window.FindControl<Border>("CommandSuggestionPanel");
        placementFrame = window.FindControl<Border>("PlacementFrame");
        dockScroll = window.FindControl<ScrollViewer>("DockScroll");
        modalGuidEntry = window.FindControl<TextBox>("ModalGuidEntry");
        modalButtonTextEntry = window.FindControl<TextBox>("ModalButtonTextEntry");
        modalCommandEntry = window.FindControl<TextBox>("ModalCommandEntry");
        modalToolEntry = window.FindControl<TextBox>("ModalToolEntry");
        modalArgumentsEntry = window.FindControl<TextBox>("ModalArgumentsEntry");
        modalClipWordEditor = window.FindControl<TextBox>("ModalClipWordEditor");
        modalNoteEditor = window.FindControl<TextBox>("ModalNoteEditor");
        modalCancelButton = window.FindControl<Button>("ModalCancelButton");
        modalSaveButton = window.FindControl<Button>("ModalSaveButton");
        contextMenuOverlay = window.FindControl<Grid>("ContextMenuOverlay");
        editorOverlay = window.FindControl<Grid>("EditorOverlay");
        macMinimizeButton = window.FindControl<Button>("MacMinimizeButton");
        windowsMaximizeButton = window.FindControl<Button>("WindowsMaximizeButton");
        windowsMaximizeGlyph = window.FindControl<TextBlock>("WindowsMaximizeGlyph");
        windowsMaximizeTooltip = window.FindControl<TextBlock>("WindowsMaximizeTooltip");
        contextEditButton = window.FindControl<Button>("ContextEditButton");
        contextDeleteButton = window.FindControl<Button>("ContextDeleteButton");
        conflictReloadButton = window.FindControl<Button>("ConflictReloadButton");
        conflictOverwriteButton = window.FindControl<Button>("ConflictOverwriteButton");
        conflictCancelButton = window.FindControl<Button>("ConflictCancelButton");
        defaultMinWidth = window.MinWidth;
        defaultMinHeight = window.MinHeight;

        if (placementSurface is not null)
        {
            placementSurface.AddHandler(InputElement.PointerPressedEvent, LauncherButton_PointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
            placementSurface.AddHandler(InputElement.PointerMovedEvent, LauncherButton_PointerMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
            placementSurface.AddHandler(InputElement.PointerReleasedEvent, LauncherButton_PointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
            placementSurface.PointerPressed += PlacementSurface_PointerPressed;
            placementSurface.PointerMoved += PlacementSurface_PointerMoved;
            placementSurface.PointerReleased += PlacementSurface_PointerReleased;
        }

        if (placementScroll is not null)
        {
            placementScroll.ScrollChanged += PlacementScroll_ScrollChanged;
            placementScroll.SizeChanged += PlacementScroll_SizeChanged;
            Dispatcher.UIThread.Post(SyncPlacementViewport);
        }

        if (contextMenuOverlay is not null)
        {
            contextMenuOverlay.AddHandler(InputElement.KeyDownEvent, ContextMenu_KeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            contextMenuOverlay.PointerPressed += ContextOverlay_PointerPressed;
        }

        window.FindControl<TextBox>("CommandBox")?.AddHandler(InputElement.KeyDownEvent, CommandBox_KeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        foreach (var textBox in new[] { modalClipWordEditor, modalNoteEditor })
        {
            if (textBox is not null)
            {
                textBox.TextChanged += GrowingEditorTextBox_TextChanged;
            }
        }

        foreach (var textBox in window.GetVisualDescendants().OfType<TextBox>())
        {
            ConfigureTextBoxContextMenu(textBox);
        }

        foreach (var grip in window.GetVisualDescendants().OfType<Border>().Where(static border => border.Classes.Contains("praxis-resize-edge") || border.Classes.Contains("praxis-resize-corner")))
        {
            grip.PointerEntered += ResizeGrip_PointerEntered;
            grip.PointerExited += ResizeGrip_PointerExited;
            grip.PointerPressed += ResizeGrip_PointerPressed;
        }

        ObserveModel();
        ApplyWindowsAppUserModelId();
        ApplyWindowsWindowChromeHints();
        ApplyWindowsWindowIcons();
        ApplyWindowsSnapWindowStyles();
        InstallWindowsCaptionHitTest();
        UpdateMacMinimizeState();
        SyncWindowThemeClasses();
        ApplyWindowsRoundedCorners();
        Dispatcher.UIThread.Post(ApplyWindowsWindowIcons, DispatcherPriority.Loaded);
        Dispatcher.UIThread.Post(ApplyWindowsSnapWindowStyles, DispatcherPriority.Loaded);
        Dispatcher.UIThread.Post(InstallWindowsCaptionHitTest, DispatcherPriority.Loaded);
        Dispatcher.UIThread.Post(ApplyWindowsRoundedCorners, DispatcherPriority.Loaded);
    }

    private void ArmMoveDragSnap()
    {
        if (!OperatingSystem.IsMacOS() || window.WindowState != WindowState.Normal)
        {
            return;
        }

        isMoveDragArmedForSnap = true;
        isMoveDragInProgress = true;
        moveDragCompletedMousePosition = null;
        edgeSnapRetryCount = 0;
    }

    private void CompleteMoveDragSnap(Point globalMousePosition)
    {
        if (!isMoveDragArmedForSnap)
        {
            return;
        }

        isMoveDragInProgress = false;
        moveDragCompletedMousePosition = globalMousePosition;
        TryApplyMacEdgeSnap();
        if (isMoveDragArmedForSnap)
        {
            StopMacEdgeSnapMonitor(restoreMinSize: true);
        }
    }

    private void WindowOnClosed(object? sender, EventArgs e)
    {
        StopMacEdgeSnapMonitor(restoreMinSize: false);
        UninstallWindowsCaptionHitTest();
        ReleaseWindowsWindowIcons();

        if (placementScroll is not null)
        {
            placementScroll.ScrollChanged -= PlacementScroll_ScrollChanged;
            placementScroll.SizeChanged -= PlacementScroll_SizeChanged;
            placementScroll = null;
        }

        Detach();
    }

    private void WindowOnDataContextChanged(object? sender, EventArgs e)
    {
        ObserveModel();
        Dispatcher.UIThread.Post(SyncPlacementViewport);
    }

    private void WindowOnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (!isMoveDragArmedForSnap || isMoveDragInProgress || isApplyingEdgeSnap || activeResizeGrip is not null)
        {
            return;
        }

        StartMacEdgeSnapMonitor();
    }

    private void WindowOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        ApplyWindowsRoundedCorners();
    }

    private void StartMacEdgeSnapMonitor()
    {
        if (edgeSnapTimer is null)
        {
            edgeSnapTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            edgeSnapTimer.Tick += (_, _) =>
            {
                edgeSnapRetryCount++;
                if (edgeSnapRetryCount > MacEdgeSnapRetryLimit)
                {
                    StopMacEdgeSnapMonitor(restoreMinSize: true);
                    return;
                }

                TryApplyMacEdgeSnap();
            };
        }

        if (!edgeSnapTimer.IsEnabled)
        {
            edgeSnapTimer.Start();
        }
    }

    private void StopMacEdgeSnapMonitor(bool restoreMinSize)
    {
        edgeSnapTimer?.Stop();
        edgeSnapTimer = null;
        edgeSnapRetryCount = 0;
        isMoveDragArmedForSnap = false;
        moveDragCompletedMousePosition = null;
        if (restoreMinSize)
        {
            RestoreDefaultMinSizeIfPossible();
        }
    }

    private void TryApplyMacEdgeSnap()
    {
        if (!OperatingSystem.IsMacOS()
            || !isMoveDragArmedForSnap
            || activeResizeGrip is not null
            || window.WindowState != WindowState.Normal)
        {
            StopMacEdgeSnapMonitor(restoreMinSize: true);
            return;
        }

        var screen = window.Screens.ScreenFromWindow(window);
        if (screen is null)
        {
            StopMacEdgeSnapMonitor(restoreMinSize: true);
            return;
        }

        var target = ResolveMacEdgeSnapTarget(screen.WorkingArea, screen.Scaling);
        if (target is null)
        {
            RestoreDefaultMinSizeIfPossible();
            return;
        }

        isApplyingEdgeSnap = true;
        try
        {
            var snapTarget = target.Value;
            if (snapTarget.IsMaximized)
            {
                RestoreDefaultMinSizeIfPossible();
                if (OperatingSystem.IsMacOS())
                {
                    macNormalZoomRestoreBounds = null;
                    CaptureNormalBoundsBeforeMaximize();
                    window.WindowState = WindowState.Maximized;
                }
                else
                {
                    CaptureNormalBoundsBeforeMaximize();
                    window.WindowState = WindowState.Maximized;
                }

                return;
            }

            window.MinWidth = Math.Min(defaultMinWidth ?? window.MinWidth, snapTarget.Width);
            window.MinHeight = Math.Min(defaultMinHeight ?? window.MinHeight, snapTarget.Height);
            window.Position = snapTarget.Position;
            window.Width = snapTarget.Width;
            window.Height = snapTarget.Height;
        }
        finally
        {
            isApplyingEdgeSnap = false;
        }
    }

    private MacEdgeSnapTarget? ResolveMacEdgeSnapTarget(PixelRect workingArea, double scaling)
    {
        var left = workingArea.X;
        var top = workingArea.Y;
        var right = workingArea.X + workingArea.Width;
        var bottom = workingArea.Y + workingArea.Height;
        bool atLeft;
        bool atRight;
        bool atTop;
        bool atBottom;
        bool atTopEdge;
        if (moveDragCompletedMousePosition is { } mouse)
        {
            var windowRight = window.Position.X + (window.Width * scaling);
            var windowBottom = window.Position.Y + (window.Height * scaling);
            atLeft = IsMouseNear(mouse.X, left, scaling) || IsNear(window.Position.X, left);
            atRight = IsMouseNear(mouse.X, right, scaling) || IsNear(windowRight, right);
            var mouseY = ResolveMousePixelCoordinate(mouse.Y, top, bottom, scaling);
            var cornerZoneHeight = workingArea.Height * MacCornerSnapZoneRatio;
            atTop = mouseY <= top + cornerZoneHeight;
            atBottom = mouseY >= bottom - cornerZoneHeight;
            atTopEdge = mouseY <= top + MacEdgeSnapThresholdPixels;
        }
        else
        {
            var windowRight = window.Position.X + (window.Width * scaling);
            var windowBottom = window.Position.Y + (window.Height * scaling);
            atLeft = IsNear(window.Position.X, left);
            atRight = IsNear(windowRight, right);
            atTop = IsNear(window.Position.Y, top);
            atBottom = IsNear(windowBottom, bottom);
            atTopEdge = atTop;
        }

        if (atTopEdge && !atLeft && !atRight)
        {
            return new MacEdgeSnapTarget(default, 0, 0, IsMaximized: true);
        }

        if (!atLeft && !atRight)
        {
            return null;
        }

        var fullWidth = workingArea.Width / scaling;
        var fullHeight = workingArea.Height / scaling;
        var width = fullWidth / 2;
        var height = (atTop || atBottom) ? fullHeight / 2 : fullHeight;
        var positionX = atRight ? workingArea.X + (int)Math.Round(width * scaling) : workingArea.X;
        var positionY = atBottom ? workingArea.Y + (int)Math.Round(height * scaling) : workingArea.Y;
        return new MacEdgeSnapTarget(new PixelPoint(positionX, positionY), width, height);
    }

    private static bool IsNear(double value, double target)
        => IsNear(value, target, MacEdgeSnapThresholdPixels);

    private static bool IsNear(double value, double target, int threshold)
        => Math.Abs(value - target) <= threshold;

    private static bool IsMouseNear(double value, double target, double scaling, int threshold = MacEdgeSnapThresholdPixels)
        => IsNear(value, target, threshold) || IsNear(value * scaling, target, threshold);

    private readonly record struct WindowBounds(PixelPoint Position, double Width, double Height);

    private static double ResolveMousePixelCoordinate(double value, double min, double max, double scaling)
    {
        if (value >= min && value <= max)
        {
            return value;
        }

        var scaled = value * scaling;
        if (scaled >= min && scaled <= max)
        {
            return scaled;
        }

        return scaled;
    }

    private readonly record struct MacEdgeSnapTarget(PixelPoint Position, double Width, double Height, bool IsMaximized = false);

    private void RestoreDefaultMinSizeIfPossible()
    {
        if (defaultMinWidth is { } minWidth && window.Width >= minWidth)
        {
            window.MinWidth = minWidth;
        }

        if (defaultMinHeight is { } minHeight && window.Height >= minHeight)
        {
            window.MinHeight = minHeight;
        }
    }

    private void PlacementScroll_ScrollChanged(object? sender, ScrollChangedEventArgs e)
        => SyncPlacementViewport();

    private void PlacementScroll_SizeChanged(object? sender, SizeChangedEventArgs e)
        => SyncPlacementViewport();

    private void SyncPlacementViewport()
    {
        if (placementScroll is null)
        {
            return;
        }

        var viewport = placementScroll.Viewport;
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            viewport = placementScroll.Bounds.Size;
        }

        ViewModel?.UpdatePlacementViewport(
            placementScroll.Offset.X,
            placementScroll.Offset.Y,
            viewport.Width,
            viewport.Height);
    }

    private void WindowOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty)
        {
            var oldState = e.GetOldValue<WindowState>();
            var newState = e.GetNewValue<WindowState>();
            if (oldState == WindowState.Normal && newState == WindowState.Maximized)
            {
                normalPositionBeforeMaximize = window.Position;
                normalWidthBeforeMaximize = window.Width;
                normalHeightBeforeMaximize = window.Height;
            }

            if (oldState == WindowState.Normal
                && newState == WindowState.FullScreen
                && normalPositionBeforeFullScreen is null)
            {
                normalPositionBeforeFullScreen = window.Position;
                normalWidthBeforeFullScreen = window.Width;
                normalHeightBeforeFullScreen = window.Height;
            }

            UpdateMacMinimizeState();
        }
    }

    private void CaptureNormalBoundsBeforeMaximize()
    {
        if (IsAtMacNormalMaximizedBounds())
        {
            return;
        }

        normalPositionBeforeMaximize = window.Position;
        normalWidthBeforeMaximize = window.Width;
        normalHeightBeforeMaximize = window.Height;
    }

    private void ToggleMacNormalMaximize()
    {
        StopMacEdgeSnapMonitor(restoreMinSize: true);
        if (window.WindowState == WindowState.Maximized)
        {
            window.WindowState = WindowState.Normal;
            RestoreCapturedNormalMaximizeBounds();
            return;
        }

        if (macNormalZoomRestoreBounds is not null || IsAtMacNormalMaximizedBounds())
        {
            RestoreMacNormalMaximizeBounds();
            return;
        }

        macNormalZoomRestoreBounds = null;
        CaptureNormalBoundsBeforeMaximize();
        window.WindowState = WindowState.Maximized;
    }

    private void MaximizeMacNormal()
    {
        var screen = window.Screens.ScreenFromWindow(window);
        if (screen is null)
        {
            return;
        }

        window.WindowState = WindowState.Normal;
        window.Position = screen.WorkingArea.Position;
        window.Width = screen.WorkingArea.Width / screen.Scaling;
        window.Height = screen.WorkingArea.Height / screen.Scaling;
    }

    private void RestoreMacNormalMaximizeBounds()
    {
        if (macNormalZoomRestoreBounds is { } bounds)
        {
            RestoreWindowBoundsWithinCurrentScreen(bounds.Position, bounds.Width, bounds.Height);
        }
        else
        {
            RestoreFallbackMacNormalBounds();
        }

        macNormalZoomRestoreBounds = null;
    }

    private void RestoreCapturedNormalMaximizeBounds()
    {
        if (normalPositionBeforeMaximize is { } position
            && normalWidthBeforeMaximize is { } width
            && normalHeightBeforeMaximize is { } height)
        {
            RestoreWindowBoundsWithinCurrentScreen(position, width, height);
        }
        else
        {
            RestoreFallbackMacNormalBounds();
        }

        ClearMaximizeRestoreBounds();
        macNormalZoomRestoreBounds = null;
    }

    private void RestoreFallbackMacNormalBounds()
    {
        var screen = window.Screens.ScreenFromWindow(window);
        if (screen is null)
        {
            window.Width = Math.Max(defaultMinWidth ?? window.MinWidth, window.Width * 0.72);
            window.Height = Math.Max(defaultMinHeight ?? window.MinHeight, window.Height * 0.72);
            return;
        }

        var width = Math.Max(defaultMinWidth ?? window.MinWidth, (screen.WorkingArea.Width / screen.Scaling) * 0.72);
        var height = Math.Max(defaultMinHeight ?? window.MinHeight, (screen.WorkingArea.Height / screen.Scaling) * 0.72);
        var x = screen.WorkingArea.X + (int)Math.Round((screen.WorkingArea.Width - (width * screen.Scaling)) / 2);
        var y = screen.WorkingArea.Y + (int)Math.Round((screen.WorkingArea.Height - (height * screen.Scaling)) / 2);
        RestoreWindowBoundsWithinCurrentScreen(new PixelPoint(x, y), width, height);
    }

    private bool IsAtMacNormalMaximizedBounds()
    {
        if (!OperatingSystem.IsMacOS() || window.WindowState != WindowState.Normal)
        {
            return false;
        }

        var screen = window.Screens.ScreenFromWindow(window);
        if (screen is null)
        {
            return false;
        }

        var width = screen.WorkingArea.Width / screen.Scaling;
        var height = screen.WorkingArea.Height / screen.Scaling;
        return Math.Abs(window.Position.X - screen.WorkingArea.X) <= MacNormalMaximizeTolerancePixels
            && Math.Abs(window.Position.Y - screen.WorkingArea.Y) <= MacNormalMaximizeTolerancePixels
            && Math.Abs(window.Width - width) <= MacNormalMaximizeTolerancePixels
            && Math.Abs(window.Height - height) <= MacNormalMaximizeTolerancePixels;
    }

    private void RestoreWindowBoundsWithinCurrentScreen(PixelPoint position, double width, double height)
    {
        var screen = window.Screens.ScreenFromWindow(window);
        if (screen is null)
        {
            window.Position = position;
            window.Width = width;
            window.Height = height;
            return;
        }

        var restoredWidth = Math.Min(width, screen.WorkingArea.Width / screen.Scaling);
        var restoredHeight = Math.Min(height, screen.WorkingArea.Height / screen.Scaling);
        var minX = screen.WorkingArea.X;
        var minY = screen.WorkingArea.Y;
        var maxX = screen.WorkingArea.X + screen.WorkingArea.Width - (int)Math.Round(restoredWidth * screen.Scaling);
        var maxY = screen.WorkingArea.Y + screen.WorkingArea.Height - (int)Math.Round(restoredHeight * screen.Scaling);
        window.Position = new PixelPoint(
            Math.Clamp(position.X, minX, Math.Max(minX, maxX)),
            Math.Clamp(position.Y, minY, Math.Max(minY, maxY)));
        window.Width = restoredWidth;
        window.Height = restoredHeight;
    }

    private void UpdateMacMinimizeState()
    {
        window.Classes.Set("fullscreen", window.WindowState == WindowState.FullScreen);
        window.Classes.Set("maximized", window.WindowState == WindowState.Maximized);
        ApplyWindowsRoundedCorners();
        if (shellContent is not null)
        {
            shellContent.Margin = ResolveShellContentMargin();
        }

        ApplyWindowsContentMargins();
        UpdateWindowsMaximizeCaption();

        if (OperatingSystem.IsMacOS())
        {
            if (window.WindowState == WindowState.FullScreen)
            {
                return;
            }
            else if (normalPositionBeforeFullScreen is { } position
                     && normalWidthBeforeFullScreen is { } width
                     && normalHeightBeforeFullScreen is { } height)
            {
                RestoreWindowBoundsWithinCurrentScreen(position, width, height);
                normalPositionBeforeFullScreen = null;
                normalWidthBeforeFullScreen = null;
                normalHeightBeforeFullScreen = null;
            }
        }

        if (macMinimizeButton is not null && OperatingSystem.IsMacOS())
        {
            macMinimizeButton.IsEnabled = window.WindowState != WindowState.FullScreen;
        }
    }

    private void UpdateWindowsMaximizeCaption()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var isMaximized = window.WindowState == WindowState.Maximized;
        if (windowsMaximizeGlyph is not null)
        {
            windowsMaximizeGlyph.Text = isMaximized ? "\uE923" : "\uE922";
        }

        if (windowsMaximizeTooltip is not null)
        {
            windowsMaximizeTooltip.Text = isMaximized ? "Restore" : "Maximize";
        }

        if (windowsMaximizeButton is not null)
        {
            ToolTip.SetTip(windowsMaximizeButton, windowsMaximizeTooltip ?? (object)(isMaximized ? "Restore" : "Maximize"));
        }
    }

    private async void EnterMacFullScreenAsync()
    {
        if (isEnteringMacFullScreen)
        {
            return;
        }

        try
        {
            isEnteringMacFullScreen = true;
            if (IsAtMacNormalMaximizedBounds())
            {
                normalPositionBeforeFullScreen = macNormalZoomRestoreBounds?.Position ?? window.Position;
                normalWidthBeforeFullScreen = macNormalZoomRestoreBounds?.Width ?? window.Width;
                normalHeightBeforeFullScreen = macNormalZoomRestoreBounds?.Height ?? window.Height;
                macNormalZoomRestoreBounds = null;
            }
            else if (window.WindowState == WindowState.Maximized)
            {
                normalPositionBeforeFullScreen = normalPositionBeforeMaximize ?? window.Position;
                normalWidthBeforeFullScreen = normalWidthBeforeMaximize ?? window.Width;
                normalHeightBeforeFullScreen = normalHeightBeforeMaximize ?? window.Height;
            }
            else
            {
                normalPositionBeforeFullScreen = window.Position;
                normalWidthBeforeFullScreen = window.Width;
                normalHeightBeforeFullScreen = window.Height;
            }

            await AnimateToCurrentScreenAsync(TimeSpan.FromMilliseconds(220));
            window.WindowState = WindowState.FullScreen;
        }
        finally
        {
            isEnteringMacFullScreen = false;
        }
    }

    private async Task AnimateToCurrentScreenAsync(TimeSpan duration)
    {
        var screen = window.Screens.ScreenFromWindow(window);
        if (screen is null)
        {
            return;
        }

        var startPosition = window.Position;
        var startWidth = window.Width;
        var startHeight = window.Height;
        var targetPosition = screen.Bounds.Position;
        var targetWidth = screen.Bounds.Width / screen.Scaling;
        var targetHeight = screen.Bounds.Height / screen.Scaling;
        const int frames = 14;
        var frameDelay = TimeSpan.FromMilliseconds(duration.TotalMilliseconds / frames);

        for (var frame = 1; frame <= frames; frame++)
        {
            var progress = frame / (double)frames;
            var eased = 1 - Math.Pow(1 - progress, 3);
            window.Position = new PixelPoint(
                Lerp(startPosition.X, targetPosition.X, eased),
                Lerp(startPosition.Y, targetPosition.Y, eased));
            window.Width = Lerp(startWidth, targetWidth, eased);
            window.Height = Lerp(startHeight, targetHeight, eased);
            await Task.Delay(frameDelay);
        }
    }

    private static int Lerp(int from, int to, double progress)
        => (int)Math.Round(from + ((to - from) * progress));

    private static double Lerp(double from, double to, double progress)
        => from + ((to - from) * progress);

    private void ObserveModel()
    {
        UnobserveModel();
        observedStatus = ViewModel?.Status;
        observedModel = ViewModel;
        if (observedStatus is not null)
        {
            observedStatus.PropertyChanged += StatusOnPropertyChanged;
        }

        if (observedModel is not null)
        {
            observedModel.PropertyChanged += ModelOnPropertyChanged;
        }
    }

    private void UnobserveModel()
    {
        if (observedStatus is not null)
        {
            observedStatus.PropertyChanged -= StatusOnPropertyChanged;
            observedStatus = null;
        }

        if (observedModel is not null)
        {
            observedModel.PropertyChanged -= ModelOnPropertyChanged;
            observedModel = null;
        }
    }

    private void ConfigurePlatformCaptionButtons()
    {
        var isMac = OperatingSystem.IsMacOS();
        var isWindows = OperatingSystem.IsWindows();
        window.Classes.Set("macos", isMac);
        window.Classes.Set("windows", isWindows);
        if (isWindows)
        {
            window.Title = string.Empty;
        }

        window.FindControl<StackPanel>("MacCaptionButtons")!.IsVisible = isMac;
        window.FindControl<StackPanel>("WindowsCaptionButtons")!.IsVisible = !isMac && !isWindows;
    }

    private Thickness ResolveShellContentMargin()
    {
        if (window.WindowState == WindowState.FullScreen)
        {
            return new Thickness(10);
        }

        return OperatingSystem.IsWindows()
            ? new Thickness(0, 0, 0, 18)
            : new Thickness(18);
    }

    private void ApplyWindowsContentMargins()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var contentMargin = new Thickness(18, 0, 18, 0);
        if (topInputRow is not null)
        {
            topInputRow.Margin = contentMargin;
        }

        if (commandSuggestionPanel is not null)
        {
            commandSuggestionPanel.Margin = new Thickness(18, 50, 18, 0);
        }

        if (placementFrame is not null)
        {
            placementFrame.Margin = new Thickness(18, 8, 18, 0);
        }

        if (dockScroll is not null)
        {
            dockScroll.Margin = new Thickness(18, 8, 18, 0);
        }

        if (statusBar is not null)
        {
            statusBar.Margin = new Thickness(18, 8, 18, 0);
        }
    }

    private void ApplyWindowsWindowChromeHints()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        window.WindowDecorations = WindowDecorations.Full;
        window.ExtendClientAreaToDecorationsHint = true;
        window.TransparencyLevelHint =
        [
            WindowTransparencyLevel.AcrylicBlur,
            WindowTransparencyLevel.Blur,
            WindowTransparencyLevel.Transparent,
        ];
        window.ExtendClientAreaTitleBarHeightHint = WindowsCaptionHitHeightDip;
    }

    private void ApplyWindowsRoundedCorners()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var handle = window.TryGetPlatformHandle();
        if (handle is null || handle.Handle == IntPtr.Zero)
        {
            return;
        }

        var preference = window.WindowState == WindowState.Normal
            ? DwmWindowCornerPreferenceRound
            : DwmWindowCornerPreferenceDefault;
        _ = DwmSetWindowAttribute(
            handle.Handle,
            DwmWindowCornerPreference,
            ref preference,
            sizeof(int));

        if (window.WindowState != WindowState.Normal)
        {
            _ = SetWindowRgn(handle.Handle, IntPtr.Zero, true);
            return;
        }

        _ = SetWindowRgn(handle.Handle, IntPtr.Zero, true);
    }

    private void ApplyWindowsWindowIcons()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var handle = window.TryGetPlatformHandle();
        if (handle is null || handle.Handle == IntPtr.Zero)
        {
            return;
        }

        var bigIconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "praxis-icon.ico");
        var smallIconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "praxis-window-small.ico");
        if (!File.Exists(bigIconPath))
        {
            return;
        }

        if (windowsSmallIcon == IntPtr.Zero)
        {
            var path = File.Exists(smallIconPath) ? smallIconPath : bigIconPath;
            windowsSmallIcon = LoadImage(IntPtr.Zero, path, ImageIcon, 16, 16, LoadFromFile);
        }

        if (windowsBigIcon == IntPtr.Zero)
        {
            windowsBigIcon = LoadImage(IntPtr.Zero, bigIconPath, ImageIcon, 32, 32, LoadFromFile);
        }

        if (windowsSmallIcon != IntPtr.Zero)
        {
            _ = SendMessage(handle.Handle, WmSetIcon, (IntPtr)IconSmall, windowsSmallIcon);
            _ = SendMessage(handle.Handle, WmSetIcon, (IntPtr)IconSmall2, windowsSmallIcon);
            _ = SetClassLongPtr(handle.Handle, GclpHIconSmall, windowsSmallIcon);
        }

        if (windowsBigIcon != IntPtr.Zero)
        {
            _ = SendMessage(handle.Handle, WmSetIcon, (IntPtr)IconBig, windowsBigIcon);
            _ = SetClassLongPtr(handle.Handle, GclpHIcon, windowsBigIcon);
        }

        ApplyWindowsRelaunchIconResource(handle.Handle, smallIconPath);
        SHChangeNotify(ShcneAssocChanged, ShcnfIdList, IntPtr.Zero, IntPtr.Zero);
    }

    private static void ApplyWindowsRelaunchIconResource(IntPtr hwnd, string smallIconPath)
    {
        var propertyStoreId = PropertyStoreInterfaceId;
        if (!File.Exists(smallIconPath)
            || SHGetPropertyStoreForWindow(hwnd, ref propertyStoreId, out var propertyStore) != 0
            || propertyStore is null)
        {
            return;
        }

        var appUserModelIdKey = AppUserModelIdKey;
        using var appUserModelId = new PropVariant(WindowsAppUserModelId);
        _ = propertyStore.SetValue(ref appUserModelIdKey, appUserModelId);

        var iconResourceKey = AppUserModelRelaunchIconResourceKey;
        using var iconResource = new PropVariant($"{smallIconPath},0");
        _ = propertyStore.SetValue(ref iconResourceKey, iconResource);
        _ = propertyStore.Commit();
    }

    private static void ApplyWindowsAppUserModelId()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        _ = SetCurrentProcessExplicitAppUserModelID(WindowsAppUserModelId);
    }

    private void InstallWindowsCaptionHitTest()
    {
        if (!OperatingSystem.IsWindows() || originalWindowsWndProc != IntPtr.Zero)
        {
            return;
        }

        var handle = window.TryGetPlatformHandle();
        if (handle is null || handle.Handle == IntPtr.Zero)
        {
            return;
        }

        windowsWndProc = WindowsWndProc;
        originalWindowsWndProc = SetWindowLongPtr(handle.Handle, GwlpWndProc, Marshal.GetFunctionPointerForDelegate(windowsWndProc));
    }

    private void ApplyWindowsSnapWindowStyles()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var handle = window.TryGetPlatformHandle();
        if (handle is null || handle.Handle == IntPtr.Zero)
        {
            return;
        }

        var style = GetWindowLongPtr(handle.Handle, GwlStyle);
        var nextStyle = (IntPtr)(style.ToInt64() | WsThickFrame | WsMinimizeBox | WsMaximizeBox);
        if (nextStyle != style)
        {
            _ = SetWindowLongPtr(handle.Handle, GwlStyle, nextStyle);
        }
    }

    private void UninstallWindowsCaptionHitTest()
    {
        if (!OperatingSystem.IsWindows() || originalWindowsWndProc == IntPtr.Zero)
        {
            return;
        }

        var handle = window.TryGetPlatformHandle();
        if (handle is not null && handle.Handle != IntPtr.Zero)
        {
            _ = SetWindowLongPtr(handle.Handle, GwlpWndProc, originalWindowsWndProc);
        }

        originalWindowsWndProc = IntPtr.Zero;
        windowsWndProc = null;
    }

    private IntPtr WindowsWndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam)
    {
        if (message == WmNcHitTest && IsWindowsCaptionHit(hwnd, lParam))
        {
            return (IntPtr)HtCaption;
        }

        return CallWindowProc(originalWindowsWndProc, hwnd, message, wParam, lParam);
    }

    private bool IsWindowsCaptionHit(IntPtr hwnd, IntPtr lParam)
    {
        if (!GetWindowRect(hwnd, out var rect))
        {
            return false;
        }

        var x = unchecked((short)((long)lParam & 0xFFFF));
        var y = unchecked((short)(((long)lParam >> 16) & 0xFFFF));
        var scaling = window.Screens.ScreenFromWindow(window)?.Scaling ?? 1;
        var localX = x - rect.Left;
        var localY = y - rect.Top;
        var width = rect.Right - rect.Left;
        var captionHeight = (int)Math.Round(WindowsCaptionHitHeightDip * scaling);
        var captionButtonsWidth = (int)Math.Round(WindowsCaptionButtonWidthDip * scaling);

        return localY >= 0
            && localY < captionHeight
            && localX >= 0
            && localX < width - captionButtonsWidth;
    }

    private void ReleaseWindowsWindowIcons()
    {
        if (windowsSmallIcon != IntPtr.Zero)
        {
            _ = DestroyIcon(windowsSmallIcon);
            windowsSmallIcon = IntPtr.Zero;
        }

        if (windowsBigIcon != IntPtr.Zero)
        {
            _ = DestroyIcon(windowsBigIcon);
            windowsBigIcon = IntPtr.Zero;
        }
    }

    private void LauncherButton_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var control = FindButtonFromEvent(sender, e.Source, "praxis-launcher");
        if (control?.DataContext is not LauncherButtonModel button)
        {
            return;
        }

        var point = e.GetCurrentPoint(control);
        if (point.Properties.IsRightButtonPressed)
        {
            ViewModel?.OpenContextMenuCommand.Execute(button);
            e.Handled = true;
            return;
        }

        if (point.Properties.IsMiddleButtonPressed)
        {
            ViewModel?.OpenEditorCommand.Execute(button);
            e.Handled = true;
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        if ((e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0)
        {
            ViewModel?.ToggleSelectionCommand.Execute(button);
            launcherDragMoved = false;
            isLauncherDragActive = false;
            e.Handled = true;
            return;
        }

        draggedButton = button;
        launcherDragStart = e.GetPosition(window);
        isLauncherDragActive = true;
        launcherDragMoved = false;
        e.Pointer.Capture(control);
        ViewModel?.DragButtonCommand.Execute(new ButtonDragPayload { Button = button, Status = InteractionStatus.Started });
        e.Handled = true;
    }

    private void LauncherButton_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!isLauncherDragActive || draggedButton is null)
        {
            return;
        }

        var delta = e.GetPosition(window) - launcherDragStart;
        launcherDragMoved = launcherDragMoved || Math.Abs(delta.X) > DragThreshold || Math.Abs(delta.Y) > DragThreshold;
        if (!launcherDragMoved)
        {
            return;
        }

        ViewModel?.DragButtonCommand.Execute(new ButtonDragPayload
        {
            Button = draggedButton,
            Status = InteractionStatus.Running,
            TotalX = delta.X,
            TotalY = delta.Y,
        });
        e.Handled = true;
    }

    private void LauncherButton_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!isLauncherDragActive || draggedButton is null)
        {
            return;
        }

        var button = draggedButton;
        var delta = e.GetPosition(window) - launcherDragStart;
        e.Pointer.Capture(null);
        ViewModel?.DragButtonCommand.Execute(new ButtonDragPayload
        {
            Button = button,
            Status = InteractionStatus.Completed,
            TotalX = launcherDragMoved ? delta.X : 0,
            TotalY = launcherDragMoved ? delta.Y : 0,
        });

        if (!launcherDragMoved && e.InitialPressMouseButton == MouseButton.Left)
        {
            ViewModel?.ExecuteButtonCommand.Execute(button);
        }

        draggedButton = null;
        isLauncherDragActive = false;
        launcherDragMoved = false;
        e.Handled = true;
    }

    private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (SuggestionButton_PointerPressed(sender, e))
        {
            return;
        }

        DockButton_PointerPressed(sender, e);
    }

    private bool SuggestionButton_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var control = FindButtonFromEvent(sender, e.Source, "praxis-suggestion");
        if (control?.DataContext is not CommandSuggestionModel suggestion)
        {
            return false;
        }

        ViewModel?.SelectSuggestionCommand.Execute(suggestion);
        var point = e.GetCurrentPoint(control);
        if (point.Properties.IsRightButtonPressed)
        {
            ViewModel?.OpenContextMenuCommand.Execute(suggestion.Source);
            e.Handled = true;
            return true;
        }

        if (point.Properties.IsMiddleButtonPressed)
        {
            ViewModel?.OpenEditorCommand.Execute(suggestion.Source);
            e.Handled = true;
            return true;
        }

        return false;
    }

    private void DockButton_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var control = FindButtonFromEvent(sender, e.Source, "praxis-dock");
        if (control?.DataContext is not LauncherButtonModel button)
        {
            return;
        }

        var point = e.GetCurrentPoint(control);
        if (point.Properties.IsRightButtonPressed)
        {
            ViewModel?.OpenContextMenuCommand.Execute(button);
            e.Handled = true;
            return;
        }

        if (point.Properties.IsMiddleButtonPressed)
        {
            ViewModel?.OpenEditorCommand.Execute(button);
            e.Handled = true;
            return;
        }

        if (point.Properties.IsLeftButtonPressed)
        {
            ViewModel?.ExecuteButtonCommand.Execute(button);
            e.Handled = true;
        }
    }

    private void PlacementSurface_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (placementSurface is null || FindButtonFromEvent(sender, e.Source, "praxis-launcher") is not null)
        {
            return;
        }

        var point = e.GetCurrentPoint(placementSurface);
        if (point.Properties.IsRightButtonPressed)
        {
            var position = e.GetPosition(placementSurface);
            ViewModel?.OpenNewButtonEditorCommand.Execute(new NewButtonPayload { X = position.X, Y = position.Y, HasPosition = true });
            e.Handled = true;
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        selectionStart = e.GetPosition(placementSurface);
        isSelectionActive = true;
        e.Pointer.Capture(placementSurface);
        UpdateSelectionRect(selectionStart, selectionStart);
        ViewModel?.ApplySelectionCommand.Execute(new SelectionPayload
        {
            StartX = selectionStart.X,
            StartY = selectionStart.Y,
            CurrentX = selectionStart.X,
            CurrentY = selectionStart.Y,
            Status = InteractionStatus.Started,
        });
        e.Handled = true;
    }

    private void PlacementSurface_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!isSelectionActive || placementSurface is null)
        {
            return;
        }

        var current = e.GetPosition(placementSurface);
        UpdateSelectionRect(selectionStart, current);
        ViewModel?.ApplySelectionCommand.Execute(new SelectionPayload
        {
            StartX = selectionStart.X,
            StartY = selectionStart.Y,
            CurrentX = current.X,
            CurrentY = current.Y,
            Status = InteractionStatus.Running,
        });
        e.Handled = true;
    }

    private void PlacementSurface_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!isSelectionActive || placementSurface is null)
        {
            return;
        }

        var current = e.GetPosition(placementSurface);
        ViewModel?.ApplySelectionCommand.Execute(new SelectionPayload
        {
            StartX = selectionStart.X,
            StartY = selectionStart.Y,
            CurrentX = current.X,
            CurrentY = current.Y,
            Status = InteractionStatus.Completed,
        });

        isSelectionActive = false;
        HideSelectionRect();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void ContextOverlay_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ReferenceEquals(sender, e.Source))
        {
            ViewModel?.CloseContextMenuCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void Window_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (FindButtonFromEvent(sender, e.Source, "praxis-suggestion") is { DataContext: CommandSuggestionModel suggestion })
        {
            ViewModel?.SelectSuggestionCommand.Execute(suggestion);
        }

        if (activeResizeGrip is not null)
        {
            ApplyManualResize(GetResizePointer(e));
            e.Handled = true;
        }
    }

    private void Window_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (activeResizeGrip is not null)
        {
            EndManualResize(e.Pointer);
            e.Handled = true;
        }
        else
        {
            window.Cursor = null;
        }
    }

    private async void CopyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (FindButtonFromEvent(sender, e.Source, "praxis-copy-button") is not { } button)
        {
            return;
        }

        var text = button.Tag?.ToString();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(text);
            ShowCopyToast();
        }

        e.Handled = true;
    }

    private void ShowCopyToast()
    {
        if (copyToast is null)
        {
            return;
        }

        copyToastDismissTimer?.Stop();
        copyToastHideTimer?.Stop();
        copyToast.IsVisible = true;
        copyToast.Opacity = 1;

        copyToastDismissTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(650) };
        copyToastDismissTimer.Tick += (_, _) =>
        {
            copyToastDismissTimer?.Stop();
            if (copyToast is null)
            {
                return;
            }

            copyToast.Opacity = 0;
            copyToastHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
            copyToastHideTimer.Tick += (_, _) =>
            {
                copyToastHideTimer?.Stop();
                if (copyToast is not null)
                {
                    copyToast.IsVisible = false;
                }
            };
            copyToastHideTimer.Start();
        };
        copyToastDismissTimer.Start();
    }

    private static void ConfigureTextBoxContextMenu(TextBox textBox)
    {
        var flyout = new MenuFlyout();
        flyout.Items.Add(CreateTextBoxMenuItem("Cut", textBox, static target => target.Cut(), () => !textBox.IsReadOnly && textBox.CanCut));
        flyout.Items.Add(CreateTextBoxMenuItem("Copy", textBox, static target => target.Copy(), () => textBox.CanCopy));
        flyout.Items.Add(CreateTextBoxMenuItem("Paste", textBox, static target => target.Paste(), () => !textBox.IsReadOnly && textBox.CanPaste));
        flyout.Items.Add(CreateTextBoxMenuItem("Select All", textBox, static target => target.SelectAll(), () => !string.IsNullOrEmpty(textBox.Text)));
        flyout.Opening += (_, _) =>
        {
            foreach (var item in flyout.Items.OfType<MenuItem>())
            {
                if (item.Tag is Func<bool> canExecute)
                {
                    item.IsEnabled = canExecute();
                }
            }
        };

        textBox.ContextFlyout = flyout;
    }

    private static MenuItem CreateTextBoxMenuItem(string header, TextBox target, Action<TextBox> action, Func<bool> canExecute)
    {
        var item = new MenuItem
        {
            Header = header,
            Tag = canExecute,
        };
        item.Click += (_, e) =>
        {
            action(target);
            e.Handled = true;
        };
        return item;
    }

    private void GrowingEditorTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var lines = Math.Max(1, (textBox.Text?.Count(static c => c == '\n') ?? 0) + 1);
            textBox.Height = Math.Min(126, 38 + ((lines - 1) * 22));
        }
    }

    private void ResizeGrip_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not string edgeName || !Enum.TryParse(edgeName, out WindowEdge edge))
        {
            return;
        }

        StopMacEdgeSnapMonitor(restoreMinSize: true);
        macNormalZoomRestoreBounds = null;
        ClearMaximizeRestoreBounds();

        if (window.WindowState == WindowState.Maximized)
        {
            var screen = window.Screens.ScreenFromWindow(window);
            var maximizedPosition = screen?.WorkingArea.Position ?? window.Position;
            var maximizedWidth = screen is null ? window.Width : screen.WorkingArea.Width / screen.Scaling;
            var maximizedHeight = screen is null ? window.Height : screen.WorkingArea.Height / screen.Scaling;
            window.WindowState = WindowState.Normal;
            window.Position = maximizedPosition;
            window.Width = maximizedWidth;
            window.Height = maximizedHeight;
        }

        var cursor = CursorForEdge(edge);
        border.Cursor = cursor;
        window.Cursor = cursor;
        activeResizeGrip = border;
        activeResizeEdge = edge;
        resizeStartPointer = GetResizePointer(e);
        resizeStartPosition = window.Position;
        resizeStartWidth = window.Width;
        resizeStartHeight = window.Height;
        resizeScaling = window.Screens.ScreenFromWindow(window)?.Scaling ?? 1;
        e.Pointer.Capture(border);
        border.PointerReleased += ResizeGrip_PointerReleased;
        border.PointerMoved += ResizeGrip_PointerMoved;
        e.Handled = true;
    }

    private static void ResizeGrip_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not Border border || border.Tag is not string edgeName || !Enum.TryParse(edgeName, out WindowEdge edge))
        {
            return;
        }

        border.Cursor = CursorForEdge(edge);
    }

    private void ResizeGrip_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Border border && !ReferenceEquals(activeResizeGrip, border))
        {
            border.Cursor = null;
            window.Cursor = null;
        }
    }

    private static Cursor? CursorForEdge(WindowEdge edge)
        => edge switch
        {
            WindowEdge.North or WindowEdge.South => new Cursor(StandardCursorType.SizeNorthSouth),
            WindowEdge.West or WindowEdge.East => new Cursor(StandardCursorType.SizeWestEast),
            WindowEdge.NorthWest or WindowEdge.SouthEast => new Cursor(StandardCursorType.TopLeftCorner),
            WindowEdge.NorthEast or WindowEdge.SouthWest => new Cursor(StandardCursorType.TopRightCorner),
            _ => null,
        };

    private void ResizeGrip_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (activeResizeGrip is null)
        {
            return;
        }

        ApplyManualResize(GetResizePointer(e));
        e.Handled = true;
    }

    private void ResizeGrip_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (activeResizeGrip is not null)
        {
            EndManualResize(e.Pointer);
            e.Handled = true;
        }
    }

    private void ApplyManualResize(Point current)
    {
        var rawDelta = current - resizeStartPointer;
        var delta = OperatingSystem.IsMacOS()
            ? new Point(rawDelta.X / resizeScaling, rawDelta.Y / resizeScaling)
            : rawDelta;
        var x = resizeStartPosition.X;
        var y = resizeStartPosition.Y;
        var width = resizeStartWidth;
        var height = resizeStartHeight;

        if (activeResizeEdge is WindowEdge.East or WindowEdge.NorthEast or WindowEdge.SouthEast)
        {
            width = Math.Max(window.MinWidth, resizeStartWidth + delta.X);
        }
        if (activeResizeEdge is WindowEdge.West or WindowEdge.NorthWest or WindowEdge.SouthWest)
        {
            width = Math.Max(window.MinWidth, resizeStartWidth - delta.X);
            x = resizeStartPosition.X + (int)Math.Round((resizeStartWidth - width) * resizeScaling);
        }
        if (activeResizeEdge is WindowEdge.South or WindowEdge.SouthEast or WindowEdge.SouthWest)
        {
            height = Math.Max(window.MinHeight, resizeStartHeight + delta.Y);
        }
        if (activeResizeEdge is WindowEdge.North or WindowEdge.NorthEast or WindowEdge.NorthWest)
        {
            height = Math.Max(window.MinHeight, resizeStartHeight - delta.Y);
            y = resizeStartPosition.Y + (int)Math.Round((resizeStartHeight - height) * resizeScaling);
        }

        window.Position = new PixelPoint(x, y);
        window.Width = width;
        window.Height = height;
    }

    private void EndManualResize(IPointer pointer)
    {
        if (activeResizeGrip is not null)
        {
            activeResizeGrip.PointerReleased -= ResizeGrip_PointerReleased;
            activeResizeGrip.PointerMoved -= ResizeGrip_PointerMoved;
            activeResizeGrip.Cursor = null;
        }

        pointer.Capture(null);
        activeResizeGrip = null;
        window.Cursor = null;
        macNormalZoomRestoreBounds = null;
        StopMacEdgeSnapMonitor(restoreMinSize: true);
        ClearMaximizeRestoreBounds();
    }

    private void ClearMaximizeRestoreBounds()
    {
        normalPositionBeforeMaximize = null;
        normalWidthBeforeMaximize = null;
        normalHeightBeforeMaximize = null;
    }

    private Point GetResizePointer(PointerEventArgs e)
        => OperatingSystem.IsMacOS() ? GetGlobalMousePosition() : e.GetPosition(window);

    private static Point GetGlobalMousePosition()
    {
        var currentEvent = CGEventCreate(IntPtr.Zero);
        if (currentEvent == IntPtr.Zero)
        {
            return default;
        }

        try
        {
            var point = CGEventGetLocation(currentEvent);
            return new Point(point.X, point.Y);
        }
        finally
        {
            CFRelease(currentEvent);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct CGPoint
    {
        public readonly double X;
        public readonly double Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct PROPERTYKEY
    {
        public Guid FormatId { get; init; }

        public int PropertyId { get; init; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct RECT
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;
    }

    private delegate IntPtr WndProcDelegate(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        int GetCount(out uint count);

        int GetAt(uint index, out PROPERTYKEY key);

        int GetValue(ref PROPERTYKEY key, IntPtr value);

        int SetValue(ref PROPERTYKEY key, PropVariant value);

        int Commit();
    }

    [StructLayout(LayoutKind.Sequential)]
    private sealed class PropVariant : IDisposable
    {
        private const ushort VtLpwstr = 31;

        private readonly ushort valueType = VtLpwstr;
        private readonly ushort reserved1;
        private readonly ushort reserved2;
        private readonly ushort reserved3;
        private IntPtr value;

        public PropVariant(string text)
        {
            value = Marshal.StringToCoTaskMemUni(text);
        }

        ~PropVariant()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (value == IntPtr.Zero)
            {
                return;
            }

            Marshal.FreeCoTaskMem(value);
            value = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }
    }

    [DllImport(CoreGraphicsLibrary)]
    private static extern IntPtr CGEventCreate(IntPtr source);

    [DllImport(CoreGraphicsLibrary)]
    private static extern CGPoint CGEventGetLocation(IntPtr eventRef);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);

    [DllImport(DwmApiLibrary)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);

    [DllImport(User32Library)]
    private static extern int SetWindowRgn(IntPtr hwnd, IntPtr region, bool redraw);

    [DllImport(User32Library, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr instance, string name, int type, int desiredWidth, int desiredHeight, int load);

    [DllImport(User32Library)]
    private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

    [DllImport(User32Library, EntryPoint = "SetClassLongPtrW", SetLastError = true)]
    private static extern IntPtr SetClassLongPtr(IntPtr hwnd, int index, IntPtr newLong);

    [DllImport(User32Library, EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr newLong);

    [DllImport(User32Library, EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport(User32Library)]
    private static extern IntPtr CallWindowProc(IntPtr previousWndProc, IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

    [DllImport(User32Library)]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

    [DllImport(User32Library, SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr icon);

    [DllImport(Shell32Library)]
    private static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

    [DllImport(Shell32Library, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

    [DllImport(Shell32Library)]
    private static extern int SHGetPropertyStoreForWindow(
        IntPtr hwnd,
        ref Guid iid,
        [MarshalAs(UnmanagedType.Interface)] out IPropertyStore? propertyStore);

    private void UpdateSelectionRect(Point start, Point current)
    {
        if (selectionRect is null)
        {
            return;
        }

        Canvas.SetLeft(selectionRect, Math.Min(start.X, current.X));
        Canvas.SetTop(selectionRect, Math.Min(start.Y, current.Y));
        selectionRect.Width = Math.Abs(current.X - start.X);
        selectionRect.Height = Math.Abs(current.Y - start.Y);
        selectionRect.IsVisible = true;
    }

    private void HideSelectionRect()
    {
        if (selectionRect is null)
        {
            return;
        }

        selectionRect.IsVisible = false;
        selectionRect.Width = 0;
        selectionRect.Height = 0;
    }

    private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsContextMenuOpen) && ViewModel?.IsContextMenuOpen == true)
        {
            Dispatcher.UIThread.Post(() => contextEditButton?.Focus(), DispatcherPriority.Loaded);
            return;
        }

        if (e.PropertyName != nameof(MainWindowViewModel.IsEditorOpen))
        {
            return;
        }

        if (ViewModel?.IsEditorOpen != true)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (modalButtonTextEntry is null)
            {
                return;
            }

            modalButtonTextEntry.Focus();
            if (ViewModel?.IsEditorCreatingNewButton == true)
            {
                modalButtonTextEntry.SelectAll();
                return;
            }

            modalButtonTextEntry.CaretIndex = modalButtonTextEntry.Text?.Length ?? 0;
        }, DispatcherPriority.Loaded);
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        var commandModifier = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
        if (ViewModel?.IsEditorOpen == true)
        {
            if (e.Key == Key.Tab)
            {
                FocusNextEditorControl(reverse: (e.KeyModifiers & KeyModifiers.Shift) != 0);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                ViewModel.CancelEditorCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.S && (e.KeyModifiers & commandModifier) != 0)
            {
                ViewModel.SaveEditorCommand.Execute(null);
                e.Handled = true;
                return;
            }
        }

        if (ViewModel?.IsConflictDialogOpen == true)
        {
            if (e.Key == Key.Tab)
            {
                FocusNextConflictControl(reverse: (e.KeyModifiers & KeyModifiers.Shift) != 0);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                ViewModel.CancelConflictCommand.Execute(null);
                e.Handled = true;
                return;
            }
        }

        var hasCommandModifier = (e.KeyModifiers & commandModifier) != 0;
        var hasShiftModifier = (e.KeyModifiers & KeyModifiers.Shift) != 0;
        if (!hasCommandModifier)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Z when !hasShiftModifier:
                ViewModel?.UndoCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Z:
                ViewModel?.RedoCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.L when hasShiftModifier:
                ApplyTheme(ThemeMode.Light);
                e.Handled = true;
                break;
            case Key.D when hasShiftModifier:
                ApplyTheme(ThemeMode.Dark);
                e.Handled = true;
                break;
            case Key.H when hasShiftModifier:
                ApplyTheme(ThemeMode.System);
                e.Handled = true;
                break;
        }
    }

    private void FocusNextEditorControl(bool reverse)
    {
        var controls = GetEditorFocusLoop();
        FocusNextControl(controls, reverse);
    }

    private Control[] GetEditorFocusLoop()
    {
        return new Control?[]
            {
                modalButtonTextEntry,
                modalCommandEntry,
                modalToolEntry,
                modalArgumentsEntry,
                modalClipWordEditor,
                modalNoteEditor,
                modalCancelButton,
                modalSaveButton,
                modalGuidEntry,
            }
            .Where(static control => control?.IsEnabled == true && control.IsVisible)
            .Cast<Control>()
            .ToArray();
    }

    private void FocusNextConflictControl(bool reverse)
    {
        var controls = new Control?[]
            {
                conflictReloadButton,
                conflictOverwriteButton,
                conflictCancelButton,
            }
            .Where(static control => control?.IsEnabled == true && control.IsVisible)
            .Cast<Control>()
            .ToArray();
        FocusNextControl(controls, reverse);
    }

    private static void FocusNextControl(IReadOnlyList<Control> controls, bool reverse)
    {
        if (controls.Count == 0)
        {
            return;
        }

        var currentIndex = -1;
        for (var index = 0; index < controls.Count; index++)
        {
            if (controls[index].IsKeyboardFocusWithin)
            {
                currentIndex = index;
                break;
            }
        }

        var nextIndex = currentIndex < 0
            ? 0
            : reverse
                ? (currentIndex - 1 + controls.Count) % controls.Count
                : (currentIndex + 1) % controls.Count;
        controls[nextIndex].Focus();
    }

    private void ContextMenu_KeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel?.IsContextMenuOpen != true)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            ViewModel.CloseContextMenuCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Down or Key.Tab)
        {
            (contextEditButton?.IsKeyboardFocusWithin == true ? contextDeleteButton : contextEditButton)?.Focus();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up)
        {
            (contextDeleteButton?.IsKeyboardFocusWithin == true ? contextEditButton : contextDeleteButton)?.Focus();
            e.Handled = true;
        }
    }

    private void CommandBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            ViewModel?.MoveSuggestionDownFromInputCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up)
        {
            ViewModel?.MoveSuggestionUpCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ApplyTheme(ThemeMode mode)
    {
        ViewModel?.ApplyThemeCommand.Execute(mode);
        if (Application.Current is null)
        {
            SyncWindowThemeClasses(mode);
            return;
        }

        Application.Current.RequestedThemeVariant = mode switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
        SyncWindowThemeClasses(mode);
        Dispatcher.UIThread.Post(() => SyncWindowThemeClasses(mode), DispatcherPriority.Background);
    }

    private void ApplicationOnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        SyncWindowThemeClasses();
    }

    private void SyncWindowThemeClasses()
    {
        SyncWindowThemeClasses(ViewModel?.SelectedTheme ?? ThemeMode.System);
    }

    private void SyncWindowThemeClasses(ThemeMode mode)
    {
        var isLight = mode switch
        {
            ThemeMode.Light => true,
            ThemeMode.Dark => false,
            _ => Application.Current?.ActualThemeVariant == ThemeVariant.Light,
        };
        window.Classes.Set("light", isLight);
        window.Classes.Set("dark", !isLight);
        ViewModel?.RefreshThemeBindings();
    }

    private static Button? FindButtonFromEvent(object? sender, object? source, string requiredClass)
    {
        var button = sender as Button;
        if (button is null && source is Visual visual)
        {
            button = visual.FindAncestorOfType<Button>(includeSelf: true);
        }

        return button?.Classes.Contains(requiredClass) == true ? button : null;
    }

    private void StatusOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(StatusModel.IsVisible) && e.PropertyName != nameof(StatusModel.Kind))
        {
            return;
        }

        var status = observedStatus;
        if (status is null || !status.IsVisible)
        {
            return;
        }

        if (statusBar is not null)
        {
            statusBar.Opacity = 1;
        }

        if (status.Kind == LauncherStatusKind.Busy)
        {
            return;
        }

        statusDismissTimer?.Stop();
        statusDismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(status.Kind == LauncherStatusKind.Error ? 5 : 2) };
        statusDismissTimer.Tick += (_, _) =>
        {
            statusDismissTimer?.Stop();
            if (statusBar is not null)
            {
                statusBar.Opacity = 0;
            }

            var dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(275) };
            dismissTimer.Tick += (_, _) =>
            {
                dismissTimer.Stop();
                status.Dismiss();
            };
            dismissTimer.Start();
        };
        statusDismissTimer.Start();
    }
}
