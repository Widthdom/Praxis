using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Praxis.Avalonia.ViewModels;
using Praxis.Core.Models;

namespace Praxis.Avalonia.Behaviors;

public sealed class MainWindowInteractionBehavior
{
    private const double DragThreshold = 3;

    private readonly Window window;
    private Canvas? placementSurface;
    private Border? selectionRect;
    private Border? statusBar;
    private Border? copyToast;
    private Grid? shellContent;
    private TextBox? modalGuidEntry;
    private TextBox? modalButtonTextEntry;
    private Grid? contextMenuOverlay;
    private Button? macMinimizeButton;
    private Button? contextEditButton;
    private Button? contextDeleteButton;
    private Point launcherDragStart;
    private Point selectionStart;
    private LauncherButtonModel? draggedButton;
    private bool isLauncherDragActive;
    private bool launcherDragMoved;
    private bool isSelectionActive;
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
    private PixelPoint? normalPositionBeforeFullScreen;
    private double? normalWidthBeforeFullScreen;
    private double? normalHeightBeforeFullScreen;
    private IReadOnlyList<WindowTransparencyLevel>? normalTransparencyLevelHint;
    private bool? normalExtendClientAreaToDecorationsHint;

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<MainWindowInteractionBehavior, Window, bool>("IsEnabled");

    public static bool GetIsEnabled(Window window)
        => window.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Window window, bool value)
        => window.SetValue(IsEnabledProperty, value);

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
        window.Opened += WindowOnOpened;
        window.Closed += WindowOnClosed;
        window.DataContextChanged += WindowOnDataContextChanged;
        window.PropertyChanged += WindowOnPropertyChanged;
        window.AddHandler(InputElement.KeyDownEvent, Window_KeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        window.AddHandler(InputElement.PointerPressedEvent, Window_PointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        window.AddHandler(InputElement.PointerMovedEvent, Window_PointerMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        window.AddHandler(Button.ClickEvent, CopyButton_Click, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void Detach()
    {
        UnobserveModel();
        window.Opened -= WindowOnOpened;
        window.Closed -= WindowOnClosed;
        window.DataContextChanged -= WindowOnDataContextChanged;
        window.PropertyChanged -= WindowOnPropertyChanged;
        window.RemoveHandler(InputElement.KeyDownEvent, Window_KeyDown);
        window.RemoveHandler(InputElement.PointerPressedEvent, Window_PointerPressed);
        window.RemoveHandler(InputElement.PointerMovedEvent, Window_PointerMoved);
        window.RemoveHandler(Button.ClickEvent, CopyButton_Click);
    }

    private void WindowOnOpened(object? sender, EventArgs e)
    {
        ConfigurePlatformCaptionButtons();
        placementSurface = window.FindControl<Canvas>("PlacementSurface");
        selectionRect = window.FindControl<Border>("SelectionRect");
        statusBar = window.FindControl<Border>("StatusBar");
        copyToast = window.FindControl<Border>("CopyToast");
        shellContent = window.FindControl<Grid>("ShellContent");
        modalGuidEntry = window.FindControl<TextBox>("ModalGuidEntry");
        modalButtonTextEntry = window.FindControl<TextBox>("ModalButtonTextEntry");
        contextMenuOverlay = window.FindControl<Grid>("ContextMenuOverlay");
        macMinimizeButton = window.FindControl<Button>("MacMinimizeButton");
        contextEditButton = window.FindControl<Button>("ContextEditButton");
        contextDeleteButton = window.FindControl<Button>("ContextDeleteButton");

        if (placementSurface is not null)
        {
            placementSurface.AddHandler(InputElement.PointerPressedEvent, LauncherButton_PointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
            placementSurface.AddHandler(InputElement.PointerMovedEvent, LauncherButton_PointerMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
            placementSurface.AddHandler(InputElement.PointerReleasedEvent, LauncherButton_PointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
            placementSurface.PointerPressed += PlacementSurface_PointerPressed;
            placementSurface.PointerMoved += PlacementSurface_PointerMoved;
            placementSurface.PointerReleased += PlacementSurface_PointerReleased;
        }

        if (contextMenuOverlay is not null)
        {
            contextMenuOverlay.AddHandler(InputElement.KeyDownEvent, ContextMenu_KeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            contextMenuOverlay.PointerPressed += ContextOverlay_PointerPressed;
        }

        window.FindControl<TextBox>("CommandBox")?.AddHandler(InputElement.KeyDownEvent, CommandBox_KeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        foreach (var textBox in new[] { window.FindControl<TextBox>("ModalClipWordEditor"), window.FindControl<TextBox>("ModalNoteEditor") })
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
        UpdateMacMinimizeState();
    }

    private void WindowOnClosed(object? sender, EventArgs e)
    {
        Detach();
    }

    private void WindowOnDataContextChanged(object? sender, EventArgs e)
    {
        ObserveModel();
    }

    private void WindowOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty)
        {
            var oldState = e.GetOldValue<WindowState>();
            var newState = e.GetNewValue<WindowState>();
            if (oldState != WindowState.FullScreen && newState == WindowState.FullScreen)
            {
                normalPositionBeforeFullScreen = window.Position;
                normalWidthBeforeFullScreen = window.Width;
                normalHeightBeforeFullScreen = window.Height;
            }

            UpdateMacMinimizeState();
        }
    }

    private void UpdateMacMinimizeState()
    {
        window.Classes.Set("fullscreen", window.WindowState == WindowState.FullScreen);
        ApplyFullScreenWindowSurface();
        if (shellContent is not null)
        {
            shellContent.Margin = window.WindowState == WindowState.FullScreen ? new Thickness(10) : new Thickness(18);
        }

        if (OperatingSystem.IsMacOS())
        {
            if (window.WindowState == WindowState.FullScreen)
            {
                Dispatcher.UIThread.Post(StretchToCurrentScreen, DispatcherPriority.Loaded);
            }
            else if (normalPositionBeforeFullScreen is { } position
                     && normalWidthBeforeFullScreen is { } width
                     && normalHeightBeforeFullScreen is { } height)
            {
                window.Position = position;
                window.Width = width;
                window.Height = height;
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

    private void StretchToCurrentScreen()
    {
        var screen = window.Screens.ScreenFromWindow(window);
        if (screen is null || window.WindowState != WindowState.FullScreen)
        {
            return;
        }

        window.Position = screen.Bounds.Position;
        window.Width = screen.Bounds.Width / screen.Scaling;
        window.Height = screen.Bounds.Height / screen.Scaling;
    }

    private void ApplyFullScreenWindowSurface()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        if (window.WindowState == WindowState.FullScreen)
        {
            normalTransparencyLevelHint ??= window.TransparencyLevelHint;
            normalExtendClientAreaToDecorationsHint ??= window.ExtendClientAreaToDecorationsHint;
            window.TransparencyLevelHint = [WindowTransparencyLevel.None];
            window.ExtendClientAreaToDecorationsHint = false;
            return;
        }

        if (normalTransparencyLevelHint is not null)
        {
            window.TransparencyLevelHint = normalTransparencyLevelHint;
            normalTransparencyLevelHint = null;
        }

        if (normalExtendClientAreaToDecorationsHint is { } extend)
        {
            window.ExtendClientAreaToDecorationsHint = extend;
            normalExtendClientAreaToDecorationsHint = null;
        }
    }

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
        window.FindControl<StackPanel>("MacCaptionButtons")!.IsVisible = OperatingSystem.IsMacOS();
        window.FindControl<StackPanel>("WindowsCaptionButtons")!.IsVisible = !OperatingSystem.IsMacOS();
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
        control.Cursor = new Cursor(StandardCursorType.SizeAll);
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
        if (FindButtonFromEvent(sender, e.Source, "praxis-launcher") is { } control)
        {
            control.Cursor = null;
        }

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
            ApplyManualResize(e.GetPosition(window));
            e.Handled = true;
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

        activeResizeGrip = border;
        activeResizeEdge = edge;
        resizeStartPointer = e.GetPosition(window);
        resizeStartPosition = window.Position;
        resizeStartWidth = window.Width;
        resizeStartHeight = window.Height;
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
        }
    }

    private static Cursor CursorForEdge(WindowEdge edge)
        => edge switch
        {
            WindowEdge.North or WindowEdge.South => new Cursor(StandardCursorType.SizeNorthSouth),
            WindowEdge.West or WindowEdge.East => new Cursor(StandardCursorType.SizeWestEast),
            WindowEdge.NorthWest or WindowEdge.SouthEast => new Cursor(StandardCursorType.TopLeftCorner),
            WindowEdge.NorthEast or WindowEdge.SouthWest => new Cursor(StandardCursorType.TopRightCorner),
            _ => new Cursor(StandardCursorType.Arrow),
        };

    private void ResizeGrip_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (activeResizeGrip is null)
        {
            return;
        }

        ApplyManualResize(e.GetPosition(window));
        e.Handled = true;
    }

    private void ResizeGrip_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (activeResizeGrip is not null)
        {
            activeResizeGrip.PointerReleased -= ResizeGrip_PointerReleased;
            activeResizeGrip.PointerMoved -= ResizeGrip_PointerMoved;
            e.Pointer.Capture(null);
            activeResizeGrip = null;
            e.Handled = true;
        }
    }

    private void ApplyManualResize(Point current)
    {
        var delta = current - resizeStartPointer;
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
            x = resizeStartPosition.X + (int)Math.Round(resizeStartWidth - width);
        }
        if (activeResizeEdge is WindowEdge.South or WindowEdge.SouthEast or WindowEdge.SouthWest)
        {
            height = Math.Max(window.MinHeight, resizeStartHeight + delta.Y);
        }
        if (activeResizeEdge is WindowEdge.North or WindowEdge.NorthEast or WindowEdge.NorthWest)
        {
            height = Math.Max(window.MinHeight, resizeStartHeight - delta.Y);
            y = resizeStartPosition.Y + (int)Math.Round(resizeStartHeight - height);
        }

        window.Width = width;
        window.Height = height;
        window.Position = new PixelPoint(x, y);
    }

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

        if (e.PropertyName != nameof(MainWindowViewModel.IsEditorOpen) || ViewModel?.IsEditorOpen != true)
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
            if (e.Key == Key.Tab && modalGuidEntry?.IsKeyboardFocusWithin == true)
            {
                modalButtonTextEntry?.Focus();
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

        if ((e.KeyModifiers & commandModifier) == 0)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Z when (e.KeyModifiers & KeyModifiers.Shift) == 0:
                ViewModel?.UndoCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Z:
                ViewModel?.RedoCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.L:
                ApplyTheme(ThemeMode.Light);
                e.Handled = true;
                break;
            case Key.D:
                ApplyTheme(ThemeMode.Dark);
                e.Handled = true;
                break;
            case Key.H:
                ApplyTheme(ThemeMode.System);
                e.Handled = true;
                break;
        }
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
        window.Classes.Set("light", mode == ThemeMode.Light);
        window.Classes.Set("dark", mode == ThemeMode.Dark);
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = mode switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
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
