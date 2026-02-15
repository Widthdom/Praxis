using Praxis.ViewModels;
using System.ComponentModel;
using System.Reflection;
using Praxis.Behaviors;

namespace Praxis;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel viewModel;
    private bool initialized;
    private CancellationTokenSource? copyNoticeCts;
    private CancellationTokenSource? statusFlashCts;
    private bool pointerDragging;
    private Point pointerStart;
    private double pointerLastDx;
    private double pointerLastDy;
    private bool selectionDragging;
    private Guid? suppressTapExecuteForItemId;
    private bool suppressNextRootSuggestionClose;
    private Point selectionStartCanvas;
    private Point selectionStartViewport;
    private Point selectionLastCanvas;
    private Point selectionLastViewport;
    private TaskCompletionSource<EditorConflictResolution>? editorConflictTcs;
#if WINDOWS
    private Microsoft.UI.Xaml.UIElement? capturedElement;
    private Microsoft.UI.Xaml.Controls.TextBox? commandTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? searchTextBox;
    private Microsoft.UI.Xaml.UIElement? pageNativeElement;
    private Microsoft.UI.Xaml.Input.KeyEventHandler? pageKeyDownHandler;
#endif

    public MainPage(MainViewModel viewModel)
    {
        App.WriteStartupLog("MainPage ctor begin");
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            App.WriteStartupLog($"MainPage InitializeComponent error: {ex}");
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Children =
                {
                    new Label { Text = "MainPage XAML load failed." },
                    new Label { Text = ex.Message },
                },
            };
        }
        BindingContext = this.viewModel = viewModel;
        this.viewModel.ResolveEditorConflictAsync = ResolveEditorConflictAsync;
        this.viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        App.ThemeShortcutRequested += OnThemeShortcutRequested;
        HandlerChanged += (_, _) =>
        {
            ApplyTabPolicy();
#if WINDOWS
            EnsureWindowsKeyHooks();
#endif
            ApplyNeutralStatusBackground();
        };
        SizeChanged += (_, _) => UpdateCommandSuggestionPopupPlacement();
        TopBarGrid.SizeChanged += (_, _) => UpdateCommandSuggestionPopupPlacement();
        MainCommandEntry.SizeChanged += (_, _) => UpdateCommandSuggestionPopupPlacement();
        MainSearchEntry.SizeChanged += (_, _) => UpdateCommandSuggestionPopupPlacement();
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged += (_, _) => Dispatcher.Dispatch(ApplyNeutralStatusBackground);
        }
        App.WriteStartupLog("MainPage ctor end");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        App.WriteStartupLog("MainPage OnAppearing");

        if (initialized)
        {
            return;
        }

        initialized = true;
        try
        {
            await viewModel.InitializeAsync();
            ApplyTabPolicy();
#if WINDOWS
            EnsureWindowsKeyHooks();
            EnsureWindowsTextBoxHooks();
#endif
            ApplyNeutralStatusBackground();
            SyncViewportToViewModel();
            App.WriteStartupLog("MainPage InitializeAsync success");
        }
        catch (Exception ex)
        {
            App.WriteStartupLog($"MainPage InitializeAsync error: {ex}");
            await DisplayAlertAsync("Initialization Error", ex.Message, "OK");
        }
    }

    private void PlacementArea_SizeChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement view)
        {
            return;
        }

        var size = new Size(view.Width, view.Height);
        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        if (viewModel.SaveAreaSizeCommand.CanExecute(size))
        {
            viewModel.SaveAreaSizeCommand.Execute(size);
        }

        SyncViewportToViewModel();
    }

    private void PlacementScroll_Scrolled(object? sender, ScrolledEventArgs e)
    {
        SyncViewportToViewModel();
    }

    private async void CopyIconButton_Clicked(object? sender, EventArgs e)
    {
        copyNoticeCts?.Cancel();
        copyNoticeCts?.Dispose();
        copyNoticeCts = new CancellationTokenSource();

        var token = copyNoticeCts.Token;
        try
        {
            CopyNoticeOverlay.IsVisible = true;
            CopyNoticeOverlay.Opacity = 0;
            CopyNoticeBox.Scale = 0.88;

            await Task.WhenAll(
                CopyNoticeOverlay.FadeToAsync(1.0, 90, Easing.CubicOut),
                CopyNoticeBox.ScaleToAsync(1.0, 90, Easing.CubicOut));

            await Task.Delay(320, token);
            token.ThrowIfCancellationRequested();

            await Task.WhenAll(
                CopyNoticeOverlay.FadeToAsync(0.0, 160, Easing.CubicIn),
                CopyNoticeBox.ScaleToAsync(0.92, 160, Easing.CubicIn));
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                CopyNoticeOverlay.IsVisible = false;
            }
        }
    }

    private void Draggable_PanUpdated(object? sender, PanUpdatedEventArgs e)
    {
#if !WINDOWS
        if (sender is not BindableObject bindable)
        {
            return;
        }

        ExecuteDragFromItem(bindable.BindingContext, e.StatusType, e.TotalX, e.TotalY);
#endif
    }

    private void Draggable_PointerPressed(object? sender, PointerEventArgs e)
    {
        if (sender is not BindableObject bindable || !IsPrimaryPointerPressed(e))
        {
            return;
        }

        if (bindable.BindingContext is LauncherButtonItemViewModel item && IsSelectionModifierPressed(e))
        {
            viewModel.ToggleSelection(item);
            suppressTapExecuteForItemId = item.Id;
            pointerDragging = false;
            ReleaseCapturedPointer();
            return;
        }

        suppressTapExecuteForItemId = null;

        var p = e.GetPosition(this);
        if (p is null)
        {
            return;
        }

        pointerDragging = true;
        pointerStart = p.Value;
        pointerLastDx = 0;
        pointerLastDy = 0;
#if WINDOWS
        TryCapturePointer(sender, e);
#endif
        ExecuteDragFromItem(bindable.BindingContext, GestureStatus.Started, 0, 0);
    }

    private void Draggable_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!pointerDragging || sender is not BindableObject bindable)
        {
            return;
        }

#if WINDOWS
        // If release happened outside the element, end drag immediately.
        if (!IsPrimaryPointerPressed(e))
        {
            pointerDragging = false;
            ReleaseCapturedPointer();
            ExecuteDragFromItem(bindable.BindingContext, GestureStatus.Completed, pointerLastDx, pointerLastDy);
            return;
        }
#endif

        var p = e.GetPosition(this);
        if (p is null)
        {
            return;
        }

        pointerLastDx = p.Value.X - pointerStart.X;
        pointerLastDy = p.Value.Y - pointerStart.Y;
        ExecuteDragFromItem(bindable.BindingContext, GestureStatus.Running, pointerLastDx, pointerLastDy);
    }

    private void Draggable_PointerReleased(object? sender, PointerEventArgs e)
    {
        if (!pointerDragging || sender is not BindableObject bindable)
        {
            return;
        }

        pointerDragging = false;
        ReleaseCapturedPointer();
        var p = e.GetPosition(this);
        var dx = p?.X - pointerStart.X ?? pointerLastDx;
        var dy = p?.Y - pointerStart.Y ?? pointerLastDy;
        ExecuteDragFromItem(bindable.BindingContext, GestureStatus.Completed, dx, dy);
    }

    private void Selection_PointerPressed(object? sender, PointerEventArgs e)
    {
        CloseCommandSuggestionPopup();

        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen)
        {
            return;
        }

        var point = GetCanvasPointFromPointer(e);
        var viewportPoint = e.GetPosition(PlacementScroll);
        if (point is null || viewportPoint is null)
        {
            return;
        }

        if (IsSecondaryPointerPressed(e))
        {
            if (!IsOnAnyVisibleButton(point.Value))
            {
                _ = OpenCreateEditorFromCanvasPointAsync(point.Value);
            }
            return;
        }

        if (!IsPrimaryPointerPressed(e) || IsOnAnyVisibleButton(point.Value))
        {
            return;
        }

        selectionDragging = true;
        selectionStartCanvas = point.Value;
        selectionStartViewport = viewportPoint.Value;
        selectionLastCanvas = selectionStartCanvas;
        selectionLastViewport = selectionStartViewport;
#if WINDOWS
        TryCapturePointer(sender, e);
#endif
        UpdateSelectionRect(selectionStartViewport, selectionStartViewport);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, selectionStartCanvas.X, selectionStartCanvas.Y, GestureStatus.Started));
    }

    private async void PlacementCanvas_SecondaryTapped(object? sender, TappedEventArgs e)
    {
        CloseCommandSuggestionPopup();

        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen)
        {
            return;
        }

        var viewportPoint = e.GetPosition(PlacementScroll);
        if (viewportPoint is null)
        {
            return;
        }

        var canvasPoint = new Point(
            viewportPoint.Value.X + PlacementScroll.ScrollX,
            viewportPoint.Value.Y + PlacementScroll.ScrollY);

        if (IsOnAnyVisibleButton(canvasPoint))
        {
            return;
        }

        await OpenCreateEditorFromCanvasPointAsync(canvasPoint);
    }

    private void Selection_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!selectionDragging)
        {
            return;
        }

#if WINDOWS
        if (!IsPrimaryPointerPressed(e))
        {
            selectionDragging = false;
            ReleaseCapturedPointer();
            UpdateSelectionRect(selectionStartViewport, selectionLastViewport, hide: true);
            ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, selectionLastCanvas.X, selectionLastCanvas.Y, GestureStatus.Completed));
            return;
        }
#endif

        var point = GetCanvasPointFromPointer(e);
        if (point is null)
        {
            return;
        }

        var viewportPoint = e.GetPosition(PlacementScroll);
        if (viewportPoint is null)
        {
            return;
        }

        selectionLastCanvas = point.Value;
        selectionLastViewport = viewportPoint.Value;
        UpdateSelectionRect(selectionStartViewport, viewportPoint.Value);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, point.Value.X, point.Value.Y, GestureStatus.Running));
    }

    private void Selection_PointerReleased(object? sender, PointerEventArgs e)
    {
        if (!selectionDragging)
        {
            return;
        }

        selectionDragging = false;
        ReleaseCapturedPointer();
        var point = GetCanvasPointFromPointer(e) ?? selectionStartCanvas;
        selectionLastCanvas = point;
        selectionLastViewport = e.GetPosition(PlacementScroll) ?? selectionStartViewport;
        UpdateSelectionRect(selectionStartViewport, e.GetPosition(PlacementScroll) ?? selectionStartViewport, hide: true);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, point.X, point.Y, GestureStatus.Completed));
    }

    private void ExecuteDragFromItem(object? item, GestureStatus status, double totalX, double totalY)
    {
        var payload = new DragPayload(item, status, totalX, totalY);
        if (viewModel.DragCommand.CanExecute(payload))
        {
            viewModel.DragCommand.Execute(payload);
        }
    }

    private static bool IsSelectionModifierPressed(PointerEventArgs e)
    {
        var platformArgs = e.PlatformArgs;
        if (platformArgs is null)
        {
            return false;
        }

        if (HasSelectionModifier(platformArgs))
        {
            return true;
        }

        var routed = TryGetProperty(platformArgs, "PointerRoutedEventArgs");
        if (routed is not null && HasSelectionModifier(routed))
        {
            return true;
        }

        var gestureRecognizer = TryGetProperty(platformArgs, "GestureRecognizer");
        if (gestureRecognizer is not null && HasSelectionModifier(gestureRecognizer))
        {
            return true;
        }

        var nativeEvent = TryGetProperty(platformArgs, "Event");
        if (nativeEvent is not null && HasSelectionModifier(nativeEvent))
        {
            return true;
        }

        return false;
    }

    private static bool HasSelectionModifier(object source)
    {
        var keyModifiers = TryGetProperty(source, "KeyModifiers");
        if (keyModifiers is not null)
        {
            var keyModifierText = keyModifiers.ToString() ?? string.Empty;
            if (keyModifierText.Contains("Control", StringComparison.OrdinalIgnoreCase) ||
                keyModifierText.Contains("Command", StringComparison.OrdinalIgnoreCase) ||
                keyModifierText.Contains("Meta", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var modifierFlags = TryGetProperty(source, "ModifierFlags");
        if (modifierFlags is not null)
        {
            var modifierText = modifierFlags.ToString() ?? string.Empty;
            if (modifierText.Contains("Control", StringComparison.OrdinalIgnoreCase) ||
                modifierText.Contains("Command", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var modifiers = TryGetProperty(source, "Modifiers");
        if (modifiers is not null)
        {
            var modifierText = modifiers.ToString() ?? string.Empty;
            if (modifierText.Contains("Control", StringComparison.OrdinalIgnoreCase) ||
                modifierText.Contains("Command", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static object? TryGetProperty(object source, string propertyName)
    {
        var prop = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(source);
    }

    private static bool IsPrimaryPointerPressed(PointerEventArgs e)
    {
#if WINDOWS
        var platformArgs = e.PlatformArgs;
        var routedProp = platformArgs?.GetType().GetProperty("PointerRoutedEventArgs");
        var routed = routedProp?.GetValue(platformArgs) as Microsoft.UI.Xaml.Input.PointerRoutedEventArgs;
        return routed?.GetCurrentPoint(null).Properties?.IsLeftButtonPressed == true;
#else
        return true;
#endif
    }

    private static bool IsSecondaryPointerPressed(PointerEventArgs e)
    {
#if WINDOWS
        var platformArgs = e.PlatformArgs;
        var routedProp = platformArgs?.GetType().GetProperty("PointerRoutedEventArgs");
        var routed = routedProp?.GetValue(platformArgs) as Microsoft.UI.Xaml.Input.PointerRoutedEventArgs;
        return routed?.GetCurrentPoint(null).Properties?.IsRightButtonPressed == true;
#else
        return false;
#endif
    }

    private Point? GetCanvasPointFromPointer(PointerEventArgs e)
    {
        var p = e.GetPosition(PlacementScroll);
        if (p is null)
        {
            return null;
        }

        return new Point(p.Value.X + PlacementScroll.ScrollX, p.Value.Y + PlacementScroll.ScrollY);
    }

    private bool IsOnAnyVisibleButton(Point point)
    {
        foreach (var b in viewModel.VisibleButtons)
        {
            if (point.X >= b.X && point.X <= b.X + b.Width &&
                point.Y >= b.Y && point.Y <= b.Y + b.Height)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateSelectionRect(Point start, Point current, bool hide = false)
    {
        if (hide)
        {
            SelectionRect.IsVisible = false;
            return;
        }

        var x = Math.Min(start.X, current.X);
        var y = Math.Min(start.Y, current.Y);
        var w = Math.Abs(current.X - start.X);
        var h = Math.Abs(current.Y - start.Y);
        SelectionRect.IsVisible = w > 2 && h > 2;
        SelectionRect.TranslationX = x;
        SelectionRect.TranslationY = y;
        SelectionRect.WidthRequest = w;
        SelectionRect.HeightRequest = h;
    }

    private void ExecuteSelectionPayload(SelectionPayload payload)
    {
        viewModel.ApplySelection(payload);
    }

    private async Task OpenCreateEditorFromCanvasPointAsync(Point canvasPoint)
    {
        await viewModel.OpenCreateEditorAtAsync(canvasPoint.X, canvasPoint.Y, useClipboardForArguments: true);
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(60), () => ModalCommandEntry.Focus());
    }

    private async void Draggable_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not LauncherButtonItemViewModel item)
        {
            return;
        }

        if (suppressTapExecuteForItemId == item.Id)
        {
            suppressTapExecuteForItemId = null;
            return;
        }

        if (viewModel.ExecuteButtonCommand.CanExecute(item))
        {
            await viewModel.ExecuteButtonCommand.ExecuteAsync(item);
        }
    }

#if WINDOWS
    private void TryCapturePointer(object? sender, PointerEventArgs e)
    {
        var element = (sender as VisualElement)?.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
        if (element is null)
        {
            return;
        }

        var platformArgs = e.PlatformArgs;
        var routedProp = platformArgs?.GetType().GetProperty("PointerRoutedEventArgs");
        var routed = routedProp?.GetValue(platformArgs) as Microsoft.UI.Xaml.Input.PointerRoutedEventArgs;
        var pointer = routed?.Pointer;
        if (pointer is null)
        {
            return;
        }

        var captureMethod = element.GetType().GetMethod("CapturePointer");
        if (captureMethod?.Invoke(element, [pointer]) is bool captured && captured)
        {
            capturedElement = element;
        }
    }

    private void ReleaseCapturedPointer()
    {
        if (capturedElement is null)
        {
            return;
        }

        var releaseAllMethod = capturedElement.GetType().GetMethod("ReleasePointerCaptures", Type.EmptyTypes);
        releaseAllMethod?.Invoke(capturedElement, null);
        capturedElement = null;
    }
#else
    private void ReleaseCapturedPointer()
    {
    }
#endif

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedTheme))
        {
            ApplyNeutralStatusBackground();
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.StatusRevision))
        {
            TriggerStatusFlash(viewModel.StatusText);
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.IsCommandSuggestionOpen))
        {
            Dispatcher.Dispatch(UpdateCommandSuggestionPopupPlacement);
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.IsContextMenuOpen))
        {
            ApplyTabPolicy();
            if (viewModel.IsContextMenuOpen)
            {
                Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(40), () => ContextEditButton.Focus());
            }

            return;
        }

        if (e.PropertyName != nameof(MainViewModel.IsEditorOpen) || !viewModel.IsEditorOpen)
        {
            if (e.PropertyName == nameof(MainViewModel.IsEditorOpen))
            {
                ApplyTabPolicy();
            }
            return;
        }

        ApplyTabPolicy();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(60), () =>
        {
            ModalCommandEntry.Focus();
        });
    }

    private async void TriggerStatusFlash(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || string.Equals(message, "Ready", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        statusFlashCts?.Cancel();
        statusFlashCts?.Dispose();
        statusFlashCts = new CancellationTokenSource();
        var token = statusFlashCts.Token;
        var neutral = GetNeutralStatusBackgroundColor();
        var flash = IsErrorStatus(message)
            ? Color.FromArgb("#D94A4A")
            : Color.FromArgb("#4AAE6A");

        try
        {
            await AnimateStatusBackgroundAsync(neutral, flash, 120, Easing.CubicOut, token);
            token.ThrowIfCancellationRequested();
            await AnimateStatusBackgroundAsync(flash, neutral, 220, Easing.CubicIn, token);
            token.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            ResetStatusBarBackgroundToThemeBinding();
        }
    }

    private static bool IsErrorStatus(string message)
        => message.StartsWith("Failed", StringComparison.OrdinalIgnoreCase)
           || message.Contains("error", StringComparison.OrdinalIgnoreCase)
           || message.Contains("exception", StringComparison.OrdinalIgnoreCase);

    private async Task AnimateStatusBackgroundAsync(Color from, Color to, uint durationMs, Easing easing, CancellationToken token)
    {
        const int steps = 10;
        for (var i = 1; i <= steps; i++)
        {
            token.ThrowIfCancellationRequested();
            var t = (double)i / steps;
            var eased = easing.Ease(t);
            StatusBarBorder.BackgroundColor = LerpColor(from, to, eased);
            await Task.Delay((int)Math.Max(1, durationMs / steps), token);
        }
    }

    private static Color LerpColor(Color from, Color to, double t)
    {
        static float Lerp(float a, float b, double ratio) => (float)(a + (b - a) * ratio);
        return new Color(
            Lerp(from.Red, to.Red, t),
            Lerp(from.Green, to.Green, t),
            Lerp(from.Blue, to.Blue, t),
            Lerp(from.Alpha, to.Alpha, t));
    }

    private Color GetNeutralStatusBackgroundColor()
    {
#if WINDOWS
        if (pageNativeElement is Microsoft.UI.Xaml.FrameworkElement fe)
        {
            var actual = fe.ActualTheme;
            if (actual == Microsoft.UI.Xaml.ElementTheme.Dark)
            {
                return Color.FromArgb("#1E1E1E");
            }

            if (actual == Microsoft.UI.Xaml.ElementTheme.Light)
            {
                return Color.FromArgb("#F2F2F2");
            }
        }
#endif
        var theme = viewModel.SelectedTheme switch
        {
            Praxis.Core.Models.ThemeMode.Light => AppTheme.Light,
            Praxis.Core.Models.ThemeMode.Dark => AppTheme.Dark,
            _ => Application.Current?.RequestedTheme ?? AppTheme.Unspecified,
        };

        return theme == AppTheme.Dark
            ? Color.FromArgb("#1E1E1E")
            : Color.FromArgb("#F2F2F2");
    }

    private void ApplyNeutralStatusBackground()
    {
        ResetStatusBarBackgroundToThemeBinding();
    }

    private void ResetStatusBarBackgroundToThemeBinding()
    {
        StatusBarBorder.ClearValue(VisualElement.BackgroundColorProperty);
    }

    private void ApplyTabPolicy()
    {
#if WINDOWS
        var editorOpen = viewModel.IsEditorOpen;
        var contextOpen = viewModel.IsContextMenuOpen;
        var mainEnabled = !editorOpen && !contextOpen;

        // Main area: only Command and Search are tabbable when modal is closed.
        SetTabStop(MainCommandEntry, mainEnabled);
        SetTabStop(MainSearchEntry, mainEnabled);
        SetTabStop(CreateButton, false);

        // Context menu area: loop only Edit/Delete.
        SetTabStop(ContextEditButton, contextOpen);
        SetTabStop(ContextDeleteButton, contextOpen);

        // Modal editor area: keep tab navigation inside modal when open.
        SetTabStop(ModalGuidEntry, editorOpen);
        SetTabStop(ModalCommandEntry, editorOpen);
        SetTabStop(ModalButtonTextEntry, editorOpen);
        SetTabStop(ModalToolEntry, editorOpen);
        SetTabStop(ModalArgumentsEntry, editorOpen);
        SetTabStop(ModalClipWordEntry, editorOpen);
        SetTabStop(ModalNoteEditor, editorOpen);
        SetTabStop(ModalCancelButton, editorOpen);
        SetTabStop(ModalSaveButton, editorOpen);

        // Copy buttons are clickable, but excluded from tab traversal.
        SetTabStop(CopyGuidButton, false);
        SetTabStop(CopyCommandButton, false);
        SetTabStop(CopyButtonTextButton, false);
        SetTabStop(CopyToolButton, false);
        SetTabStop(CopyArgumentsButton, false);
        SetTabStop(CopyClipWordButton, false);
        SetTabStop(CopyNoteButton, false);
#endif
    }

    private void MainCommandEntry_HandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        EnsureWindowsKeyHooks();
        EnsureWindowsTextBoxHooks();
#endif
        UpdateCommandSuggestionPopupPlacement();
    }

    private void MainCommandEntry_Focused(object? sender, FocusEventArgs e)
    {
        ReopenCommandSuggestionsFromEntry();
    }

    private void MainCommandEntry_PointerPressed(object? sender, PointerEventArgs e)
    {
        ReopenCommandSuggestionsFromEntry();
    }

    private void MainCommandEntry_Tapped(object? sender, TappedEventArgs e)
    {
        ReopenCommandSuggestionsFromEntry();
    }

    private void MainSearchEntry_Focused(object? sender, FocusEventArgs e)
    {
        CloseCommandSuggestionPopup();
    }

    private void MainSearchEntry_HandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        EnsureWindowsKeyHooks();
        EnsureWindowsTextBoxHooks();
#endif
    }

#if WINDOWS
    private void CommandTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (TryHandleThemeShortcutFromKey(e))
        {
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Windows.System.VirtualKey.Up:
                if (viewModel.MoveSuggestionUpCommand.CanExecute(null))
                {
                    viewModel.MoveSuggestionUpCommand.Execute(null);
                    e.Handled = true;
                }
                break;
            case Windows.System.VirtualKey.Down:
                if (viewModel.MoveSuggestionDownCommand.CanExecute(null))
                {
                    viewModel.MoveSuggestionDownCommand.Execute(null);
                    e.Handled = true;
                }
                break;
            case Windows.System.VirtualKey.Escape:
                if (viewModel.CloseCommandSuggestionsCommand.CanExecute(null))
                {
                    viewModel.CloseCommandSuggestionsCommand.Execute(null);
                    e.Handled = true;
                }
                break;
        }
    }

    private void SearchTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (TryHandleThemeShortcutFromKey(e))
        {
            e.Handled = true;
        }
    }

    private void CommandTextBox_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (viewModel.ReopenCommandSuggestionsCommand.CanExecute(null))
        {
            viewModel.ReopenCommandSuggestionsCommand.Execute(null);
        }
    }

    private void PageNativeElement_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (TryHandleThemeShortcutFromKey(e))
        {
            e.Handled = true;
            return;
        }

        var ctrlDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (!viewModel.IsEditorOpen)
        {
            return;
        }

        if (ctrlDown && e.Key == Windows.System.VirtualKey.S)
        {
            if (viewModel.SaveEditorCommand.CanExecute(null))
            {
                viewModel.SaveEditorCommand.Execute(null);
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            if (viewModel.CancelEditorCommand.CanExecute(null))
            {
                viewModel.CancelEditorCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private bool TryHandleThemeShortcutFromKey(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        var ctrlDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var shiftDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        if (!ctrlDown || !shiftDown)
        {
            return false;
        }

        if (e.Key == Windows.System.VirtualKey.L)
        {
            ApplyThemeShortcut("Light");
            return true;
        }

        if (e.Key == Windows.System.VirtualKey.D)
        {
            ApplyThemeShortcut("Dark");
            return true;
        }

        if (e.Key == Windows.System.VirtualKey.H)
        {
            ApplyThemeShortcut("System");
            return true;
        }

        return false;
    }

    private void EnsureWindowsKeyHooks()
    {
        var current = Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
        if (current is null)
        {
            return;
        }

        if (!ReferenceEquals(pageNativeElement, current))
        {
            if (pageNativeElement is not null && pageKeyDownHandler is not null)
            {
                pageNativeElement.RemoveHandler(Microsoft.UI.Xaml.UIElement.KeyDownEvent, pageKeyDownHandler);
            }

            pageNativeElement = current;
            pageKeyDownHandler ??= new Microsoft.UI.Xaml.Input.KeyEventHandler(PageNativeElement_KeyDown);
            pageNativeElement.AddHandler(Microsoft.UI.Xaml.UIElement.KeyDownEvent, pageKeyDownHandler, true);
        }
    }

    private void EnsureWindowsTextBoxHooks()
    {
        var currentCommand = MainCommandEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox;
        if (!ReferenceEquals(commandTextBox, currentCommand))
        {
            if (commandTextBox is not null)
            {
                commandTextBox.KeyDown -= CommandTextBox_KeyDown;
                commandTextBox.PointerPressed -= CommandTextBox_PointerPressed;
            }

            commandTextBox = currentCommand;
            if (commandTextBox is not null)
            {
                commandTextBox.KeyDown += CommandTextBox_KeyDown;
                commandTextBox.PointerPressed += CommandTextBox_PointerPressed;
            }
        }

        var currentSearch = MainSearchEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox;
        if (!ReferenceEquals(searchTextBox, currentSearch))
        {
            if (searchTextBox is not null)
            {
                searchTextBox.KeyDown -= SearchTextBox_KeyDown;
            }

            searchTextBox = currentSearch;
            if (searchTextBox is not null)
            {
                searchTextBox.KeyDown += SearchTextBox_KeyDown;
            }
        }
    }

#endif

    private void OnThemeShortcutRequested(string mode)
    {
        Dispatcher.Dispatch(() => ApplyThemeShortcut(mode));
    }

    private void ApplyThemeShortcut(string mode)
    {
        if (viewModel.SetThemeCommand.CanExecute(mode))
        {
            viewModel.SetThemeCommand.Execute(mode);
        }
    }

    private async Task<EditorConflictResolution> ResolveEditorConflictAsync(EditorConflictContext context)
    {
        var title = context.ConflictType == EditorConflictType.DeletedByOtherWindow
            ? "Conflict detected (deleted)"
            : "Conflict detected (updated)";
        var message = context.ConflictType == EditorConflictType.DeletedByOtherWindow
            ? "This button was deleted in another window."
            : "This button was updated in another window.";

        return await ShowEditorConflictDialogAsync(title, message);
    }

    private Task<EditorConflictResolution> ShowEditorConflictDialogAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<EditorConflictResolution>(TaskCreationOptions.RunContinuationsAsynchronously);
        editorConflictTcs = tcs;

        Dispatcher.Dispatch(() =>
        {
            ConflictTitleLabel.Text = title;
            ConflictMessageLabel.Text = message;
            ConflictOverlay.IsVisible = true;
            ConflictReloadButton.Focus();
        });

        return tcs.Task;
    }

    private void CloseEditorConflictDialog(EditorConflictResolution resolution)
    {
        if (editorConflictTcs is null)
        {
            return;
        }

        var tcs = editorConflictTcs;
        editorConflictTcs = null;
        ConflictOverlay.IsVisible = false;
        tcs.TrySetResult(resolution);
    }

    private void ConflictReloadButton_Clicked(object? sender, EventArgs e)
    {
        CloseEditorConflictDialog(EditorConflictResolution.Reload);
    }

    private void ConflictOverwriteButton_Clicked(object? sender, EventArgs e)
    {
        CloseEditorConflictDialog(EditorConflictResolution.Overwrite);
    }

    private void ConflictCancelButton_Clicked(object? sender, EventArgs e)
    {
        CloseEditorConflictDialog(EditorConflictResolution.Cancel);
    }

    private void RootGrid_PointerPressed(object? sender, PointerEventArgs e)
    {
        if (suppressNextRootSuggestionClose)
        {
            suppressNextRootSuggestionClose = false;
            return;
        }

        if (!viewModel.IsCommandSuggestionOpen)
        {
            return;
        }

        var p = e.GetPosition(RootGrid);
        if (p is null)
        {
            return;
        }

        if (IsPointInsideElement(p.Value, MainCommandEntry) || IsPointInsideElement(p.Value, CommandSuggestionPopup))
        {
            return;
        }

        CloseCommandSuggestionPopup();
    }

    private void UpdateCommandSuggestionPopupPlacement()
    {
        if (MainCommandEntry.Width <= 0 || MainCommandEntry.Height <= 0 || MainSearchEntry.Width <= 0)
        {
            return;
        }

        var commandPos = GetPositionRelativeToAncestor(MainCommandEntry, RootGrid);
        var searchPos = GetPositionRelativeToAncestor(MainSearchEntry, RootGrid);
        var left = commandPos.X - RootGrid.Padding.Left;
        var right = searchPos.X + MainSearchEntry.Width - RootGrid.Padding.Left;
        var top = commandPos.Y - RootGrid.Padding.Top;

        CommandSuggestionPopup.WidthRequest = Math.Max(80, right - left);
        CommandSuggestionPopup.TranslationX = left;
        CommandSuggestionPopup.TranslationY = top + MainCommandEntry.Height + 10;
    }

    private void SyncViewportToViewModel()
    {
        if (PlacementScroll.Width <= 0 || PlacementScroll.Height <= 0)
        {
            return;
        }

        viewModel.UpdateViewport(
            PlacementScroll.ScrollX,
            PlacementScroll.ScrollY,
            PlacementScroll.Width,
            PlacementScroll.Height);
    }

    private void CloseCommandSuggestionPopup()
    {
        if (viewModel.CloseCommandSuggestionsCommand.CanExecute(null))
        {
            viewModel.CloseCommandSuggestionsCommand.Execute(null);
        }
    }

    private void ReopenCommandSuggestionsFromEntry()
    {
        suppressNextRootSuggestionClose = true;
        if (viewModel.ReopenCommandSuggestionsCommand.CanExecute(null))
        {
            viewModel.ReopenCommandSuggestionsCommand.Execute(null);
        }
    }

    private static Point GetPositionRelativeToAncestor(VisualElement element, VisualElement ancestor)
    {
        double x = 0;
        double y = 0;

        Element? current = element;
        while (current is VisualElement ve && !ReferenceEquals(ve, ancestor))
        {
            x += ve.X;
            y += ve.Y;
            current = ve.Parent;
        }

        return new Point(x, y);
    }

    private bool IsPointInsideElement(Point p, VisualElement element)
    {
        if (element.Width <= 0 || element.Height <= 0)
        {
            return false;
        }

        var pos = GetPositionRelativeToAncestor(element, RootGrid);
        return p.X >= pos.X &&
               p.X <= pos.X + element.Width &&
               p.Y >= pos.Y &&
               p.Y <= pos.Y + element.Height;
    }

#if WINDOWS
    private static void SetTabStop(VisualElement element, bool isTabStop)
    {
        var platformView = element.Handler?.PlatformView;
        if (platformView is null)
        {
            return;
        }

        var prop = platformView.GetType().GetProperty("IsTabStop", BindingFlags.Public | BindingFlags.Instance);
        if (prop is null || !prop.CanWrite)
        {
            return;
        }

        prop.SetValue(platformView, isTabStop);
    }
#endif
}
