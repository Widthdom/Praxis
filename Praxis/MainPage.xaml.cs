using Praxis.Core.Logic;
using Praxis.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using Praxis.Behaviors;
#if MACCATALYST
using Foundation;
using ObjCRuntime;
using CoreGraphics;
using UIKit;
#endif

namespace Praxis;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel viewModel;
    private bool xamlLoaded;
    private bool initialized;
    private CancellationTokenSource? copyNoticeCts;
    private CancellationTokenSource? statusFlashCts;
    private bool pointerDragging;
    private Point pointerStart = Point.Zero;
    private double pointerLastDx;
    private double pointerLastDy;
#if !WINDOWS
    private double panDragLastDx;
    private double panDragLastDy;
    private object? panDragItem;
#endif
    private bool selectionDragging;
#if MACCATALYST
    private bool selectionPanPrimed;
#endif
    private Guid? suppressTapExecuteForItemId;
    private bool suppressNextRootSuggestionClose;
    private Point selectionStartCanvas;
    private Point selectionStartViewport;
    private Point selectionLastCanvas;
    private Point selectionLastViewport;
    private Point? lastPointerOnRoot;
    private TaskCompletionSource<EditorConflictResolution>? editorConflictTcs;
#if WINDOWS
    private Microsoft.UI.Xaml.UIElement? capturedElement;
    private Microsoft.UI.Xaml.Controls.TextBox? commandTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? searchTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? modalGuidTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? modalCommandTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? modalButtonTextTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? modalToolTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? modalArgumentsTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? modalClipWordTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? modalNoteTextBox;
    private bool windowsSelectAllOnTabNavigationPending;
    private Microsoft.UI.Xaml.UIElement? pageNativeElement;
    private Microsoft.UI.Xaml.Input.KeyEventHandler? pageKeyDownHandler;
#endif
    private enum ConflictDialogFocusTarget
    {
        Reload,
        Overwrite,
        Cancel,
    }

    private ConflictDialogFocusTarget? conflictDialogPseudoFocusedTarget;
#if MACCATALYST
    private enum ModalFocusTarget
    {
        Guid,
        Command,
        ButtonText,
        Tool,
        Arguments,
        ClipWord,
        Note,
        CancelButton,
        SaveButton,
    }

    private enum ContextMenuFocusTarget
    {
        Edit,
        Delete,
    }

    private static readonly ModalFocusTarget[] ModalFocusOrder =
    [
        ModalFocusTarget.Guid,
        ModalFocusTarget.Command,
        ModalFocusTarget.ButtonText,
        ModalFocusTarget.Tool,
        ModalFocusTarget.Arguments,
        ModalFocusTarget.ClipWord,
        ModalFocusTarget.Note,
        ModalFocusTarget.CancelButton,
        ModalFocusTarget.SaveButton,
    ];

    private UIKeyCommand? modalEscapeKeyCommand;
    private UIKeyCommand? modalSaveKeyCommand;
    private UIKeyCommand? modalTabNextKeyCommand;
    private UIKeyCommand? modalTabPreviousKeyCommand;
    private UIKeyCommand? modalPrimaryActionKeyCommand;
    private UIKeyCommand? modalPrimaryActionAlternateKeyCommand;
    private static readonly string macEscapeKeyInput = ResolveMacKeyInput("InputEscape", "\u001B");
    private static readonly string macTabKeyInput = ResolveMacKeyInput("InputTab", "\t");
    private static readonly string macReturnKeyInput = ResolveMacKeyInput("InputReturn", "\r");
    private static readonly string? macEnterKeyInput = TryResolveMacKeyInput("InputEnter");
    private static readonly string macUpArrowKeyInput = ResolveMacKeyInput("InputUpArrow", "\uF700");
    private static readonly string macDownArrowKeyInput = ResolveMacKeyInput("InputDownArrow", "\uF701");
    private UIKeyCommand? commandSuggestionUpKeyCommand;
    private UIKeyCommand? commandSuggestionDownKeyCommand;
    private Microsoft.Maui.Dispatching.IDispatcherTimer? macMiddleButtonPollTimer;
    private bool macMiddleButtonWasDown;
    private bool macInitialCommandFocusApplied;
    private ModalFocusTarget? macPseudoFocusedModalTarget;
    private ContextMenuFocusTarget? macPseudoFocusedContextMenuTarget;
    private static readonly bool macDynamicKeyCommandRegistrationEnabled = false;
    private readonly UITextFieldDelegate macGuidReadOnlyDelegate = new MacGuidReadOnlyTextFieldDelegate();
    private UITextField? macGuidNativeTextField;
    private string macGuidLockedText = string.Empty;
    private bool macApplyingGuidTextLock;
    private bool macSuppressEditorTabFallback;
#endif

    public MainPage(MainViewModel viewModel)
    {
        try
        {
            InitializeComponent();
            xamlLoaded = true;
        }
        catch (Exception ex)
        {
            xamlLoaded = false;
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
        if (!xamlLoaded)
        {
            return;
        }

        this.viewModel.ResolveEditorConflictAsync = ResolveEditorConflictAsync;
        App.SetEditorOpenState(this.viewModel.IsEditorOpen);
        this.viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        this.viewModel.CommandSuggestions.CollectionChanged += CommandSuggestionsOnCollectionChanged;
        App.ThemeShortcutRequested += OnThemeShortcutRequested;
        App.EditorShortcutRequested += OnEditorShortcutRequested;
        App.CommandInputShortcutRequested += OnCommandInputShortcutRequested;
        App.MiddleMouseClickRequested += OnMiddleMouseClickRequested;
        HandlerChanged += (_, _) =>
        {
            ApplyTabPolicy();
#if WINDOWS
            EnsureWindowsKeyHooks();
#endif
            ApplyNeutralStatusBackground();
#if MACCATALYST
            ApplyMacVisualTuning();
#endif
        };
        SizeChanged += (_, _) =>
        {
            UpdateCommandSuggestionPopupPlacement();
#if MACCATALYST
            ApplyMacContentScale();
#endif
        };
        TopBarGrid.SizeChanged += (_, _) => UpdateCommandSuggestionPopupPlacement();
        MainCommandEntry.SizeChanged += (_, _) => UpdateCommandSuggestionPopupPlacement();
        MainSearchEntry.SizeChanged += (_, _) => UpdateCommandSuggestionPopupPlacement();
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged += (_, _) => Dispatcher.Dispatch(() =>
            {
                ApplyNeutralStatusBackground();
                ApplyContextActionButtonFocusVisuals();
#if MACCATALYST
                ApplyMacVisualTuning();
                ApplyMacNoteEditorVisualState();
#endif
            });
        }
        RebuildCommandSuggestionStack();
#if MACCATALYST
        ModalGuidEntry.Focused += ModalEditorField_Focused;
        ModalCommandEntry.Focused += ModalEditorField_Focused;
        ModalButtonTextEntry.Focused += ModalEditorField_Focused;
        ModalToolEntry.Focused += ModalEditorField_Focused;
        ModalArgumentsEntry.Focused += ModalEditorField_Focused;
        ModalClipWordEditor.Focused += ModalEditorField_Focused;
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!xamlLoaded)
        {
            return;
        }

        if (initialized)
        {
            return;
        }

        initialized = true;
        try
        {
            await viewModel.InitializeAsync();
            ApplyTabPolicy();
            App.SetContextMenuOpenState(viewModel.IsContextMenuOpen);
#if WINDOWS
            EnsureWindowsKeyHooks();
            EnsureWindowsTextBoxHooks();
#endif
            ApplyNeutralStatusBackground();
            SyncViewportToViewModel();
#if MACCATALYST
            ApplyMacVisualTuning();
            ApplyMacInitialCommandFocus();
            StartMacMiddleButtonPolling();
#endif
            UpdateModalEditorHeights();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Initialization Error", ex.Message, "OK");
        }
    }

    protected override bool OnBackButtonPressed()
    {
        if (viewModel.IsEditorOpen && viewModel.CancelEditorCommand.CanExecute(null))
        {
            viewModel.CancelEditorCommand.Execute(null);
            return true;
        }

        return base.OnBackButtonPressed();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        App.SetEditorOpenState(false);
        App.SetContextMenuOpenState(false);
        App.SetConflictDialogOpenState(false);
        UpdateConflictDialogModalState(isOpen: false);
#if MACCATALYST
        DetachMacGuidEntryReadOnlyBehavior();
        macGuidLockedText = string.Empty;
        StopMacMiddleButtonPolling();
#endif
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

    private void ModalNoteEditor_HandlerChanged(object? sender, EventArgs e)
    {
#if MACCATALYST
        if (ModalNoteEditor.Handler?.PlatformView is UITextView textView)
        {
            textView.BackgroundColor = UIColor.Clear;
            textView.Layer.BorderWidth = 0;
            textView.Layer.CornerRadius = 0;
        }
        ApplyMacNoteEditorVisualState();
        ModalNoteFocusUnderline.IsVisible = ModalNoteEditor.IsFocused;
        ApplyMacEditorKeyCommands();
#endif
#if WINDOWS
        EnsureWindowsTextBoxHooks();
#endif
        UpdateModalEditorHeights();
    }

    private void ModalClipWordEditor_HandlerChanged(object? sender, EventArgs e)
    {
#if MACCATALYST
        if (ModalClipWordEditor.Handler?.PlatformView is UITextView textView)
        {
            textView.BackgroundColor = UIColor.Clear;
            textView.Layer.BorderWidth = 0;
            textView.Layer.CornerRadius = 0;
        }
        ApplyMacClipWordEditorVisualState();
        ModalClipWordFocusUnderline.IsVisible = ModalClipWordEditor.IsFocused;
        ApplyMacEditorKeyCommands();
#endif
#if WINDOWS
        EnsureWindowsTextBoxHooks();
#endif
        UpdateModalEditorHeights();
    }

    private void ModalClipWordEditor_Focused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        ClearMacModalPseudoFocus();
        ModalClipWordFocusUnderline.IsVisible = true;
#endif
    }

    private void ModalClipWordEditor_Unfocused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        ModalClipWordFocusUnderline.IsVisible = false;
#endif
    }

    private void ModalNoteEditor_Focused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        ClearMacModalPseudoFocus();
        ModalNoteFocusUnderline.IsVisible = true;
#endif
    }

    private void ModalNoteEditor_Unfocused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        ModalNoteFocusUnderline.IsVisible = false;
#endif
    }

    private void ModalGuidEntry_HandlerChanged(object? sender, EventArgs e)
    {
#if MACCATALYST
        EnsureMacGuidEntryReadOnlyBehavior();
#endif
#if WINDOWS
        EnsureWindowsTextBoxHooks();
#endif
    }

    private void ModalTextInput_HandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        EnsureWindowsTextBoxHooks();
#endif
    }

    private void ModalEditorField_Focused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        if (ReferenceEquals(sender, ModalGuidEntry))
        {
            EnsureMacGuidEntryReadOnlyBehavior();
        }

        ClearMacModalPseudoFocus();
#endif
    }

    private void ModalNoteEditor_TextChanged(object? sender, TextChangedEventArgs e)
    {
#if MACCATALYST
        TryHandleMacEditorTabTextInsertion(sender, e);
#endif
        UpdateModalEditorHeights();
    }

    private void ModalClipWordEditor_TextChanged(object? sender, TextChangedEventArgs e)
    {
#if MACCATALYST
        TryHandleMacEditorTabTextInsertion(sender, e);
#endif
        UpdateModalEditorHeights();
    }

    private void UpdateModalEditorHeights()
    {
        if (!xamlLoaded)
        {
            return;
        }

        UpdateEditorHeight(ModalClipWordEditor, ModalClipWordContainer);
        UpdateEditorHeight(ModalNoteEditor, ModalNoteContainer);
    }

    private static void UpdateEditorHeight(Editor editor, Border container)
    {
        const double singleLineHeight = 40;
        const double maxHeight = 220;
        const double perLineHeight = 24;
        const double basePadding = 16;
        var text = editor.Text ?? string.Empty;
        var lineCount = Math.Max(1, text.Count(c => c == '\n') + 1);
        var targetHeight = lineCount <= 1
            ? singleLineHeight
            : Math.Min(maxHeight, basePadding + (lineCount * perLineHeight));

        editor.HeightRequest = targetHeight;
        container.HeightRequest = targetHeight;
    }

    private void Draggable_PanUpdated(object? sender, PanUpdatedEventArgs e)
    {
#if !WINDOWS
        if (sender is not BindableObject bindable)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                panDragItem = bindable.BindingContext;
                panDragLastDx = 0;
                panDragLastDy = 0;
                ExecuteDragFromItem(panDragItem, GestureStatus.Started, 0, 0);
                break;
            case GestureStatus.Running:
                panDragLastDx = e.TotalX;
                panDragLastDy = e.TotalY;
                ExecuteDragFromItem(panDragItem ?? bindable.BindingContext, GestureStatus.Running, panDragLastDx, panDragLastDy);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
            {
                var dx = e.TotalX;
                var dy = e.TotalY;
                if (Math.Abs(dx) < 0.5 && Math.Abs(panDragLastDx) >= 0.5)
                {
                    dx = panDragLastDx;
                }

                if (Math.Abs(dy) < 0.5 && Math.Abs(panDragLastDy) >= 0.5)
                {
                    dy = panDragLastDy;
                }

                ExecuteDragFromItem(panDragItem ?? bindable.BindingContext, GestureStatus.Completed, dx, dy);
                panDragItem = null;
                panDragLastDx = 0;
                panDragLastDy = 0;
                break;
            }
            default:
                break;
        }
#endif
    }

    private void Draggable_PointerPressed(object? sender, PointerEventArgs e)
    {
        if (sender is not BindableObject bindable)
        {
            return;
        }

#if MACCATALYST
        if (IsOtherMouseFromPlatformArgs(e.PlatformArgs) && bindable.BindingContext is LauncherButtonItemViewModel otherMouseItem)
        {
            suppressTapExecuteForItemId = otherMouseItem.Id;
            pointerDragging = false;
            ReleaseCapturedPointer();
            if (viewModel.OpenEditorCommand.CanExecute(otherMouseItem))
            {
                viewModel.OpenEditorCommand.Execute(otherMouseItem);
            }
            return;
        }
#endif

        if (IsMiddlePointerPressed(e) && bindable.BindingContext is LauncherButtonItemViewModel middleItem)
        {
            suppressTapExecuteForItemId = middleItem.Id;
            pointerDragging = false;
            ReleaseCapturedPointer();
            if (viewModel.OpenEditorCommand.CanExecute(middleItem))
            {
                viewModel.OpenEditorCommand.Execute(middleItem);
            }
            return;
        }

        if (!IsPrimaryPointerPressed(e))
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

 #if MACCATALYST
        if (IsSecondaryPointerPressed(e) && bindable.BindingContext is LauncherButtonItemViewModel secondaryItem)
        {
            suppressTapExecuteForItemId = secondaryItem.Id;
            pointerDragging = false;
            ReleaseCapturedPointer();
            if (viewModel.OpenContextMenuCommand.CanExecute(secondaryItem))
            {
                viewModel.OpenContextMenuCommand.Execute(secondaryItem);
            }
            return;
        }
 #endif

#if !MACCATALYST
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
#endif
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
        if (sender is BindableObject bindableForMiddle &&
            IsMiddlePointerPressed(e) &&
            bindableForMiddle.BindingContext is LauncherButtonItemViewModel middleItem)
        {
            suppressTapExecuteForItemId = middleItem.Id;
            pointerDragging = false;
            ReleaseCapturedPointer();
            if (viewModel.OpenEditorCommand.CanExecute(middleItem))
            {
                viewModel.OpenEditorCommand.Execute(middleItem);
            }
            return;
        }

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

    private void DockButton_PointerPressed(object? sender, PointerEventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not LauncherButtonItemViewModel item)
        {
            return;
        }

#if MACCATALYST
        if (IsOtherMouseFromPlatformArgs(e.PlatformArgs))
        {
            if (viewModel.OpenEditorCommand.CanExecute(item))
            {
                viewModel.OpenEditorCommand.Execute(item);
            }
            return;
        }
#endif

        if (IsMiddlePointerPressed(e))
        {
            if (viewModel.OpenEditorCommand.CanExecute(item))
            {
                viewModel.OpenEditorCommand.Execute(item);
            }

            return;
        }

#if MACCATALYST
        if (!IsSecondaryPointerPressed(e))
        {
            return;
        }

        if (viewModel.OpenContextMenuCommand.CanExecute(item))
        {
            viewModel.OpenContextMenuCommand.Execute(item);
        }
#endif
    }

    private void Draggable_SecondaryTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not LauncherButtonItemViewModel item)
        {
            return;
        }

        if (viewModel.OpenContextMenuCommand.CanExecute(item))
        {
            viewModel.OpenContextMenuCommand.Execute(item);
        }
    }

    private void DockButton_SecondaryTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not LauncherButtonItemViewModel item)
        {
            return;
        }

        if (viewModel.OpenContextMenuCommand.CanExecute(item))
        {
            viewModel.OpenContextMenuCommand.Execute(item);
        }
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

#if MACCATALYST
        if (!IsPrimaryPointerPressed(e) || IsOnAnyVisibleButton(point.Value))
        {
            selectionPanPrimed = false;
            return;
        }

        selectionDragging = true;
        selectionPanPrimed = true;
        selectionStartCanvas = point.Value;
        selectionStartViewport = ClampToPlacementViewport(viewportPoint.Value);
        selectionLastCanvas = selectionStartCanvas;
        selectionLastViewport = selectionStartViewport;
        UpdateSelectionRect(selectionStartViewport, selectionStartViewport);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, selectionStartCanvas.X, selectionStartCanvas.Y, GestureStatus.Started));
#else
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
#endif
    }

    private void Selection_PanUpdated(object? sender, PanUpdatedEventArgs e)
    {
#if WINDOWS
        return;
#else
        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                if (!selectionPanPrimed || !selectionDragging)
                {
                    return;
                }

                selectionLastViewport = selectionStartViewport;
                selectionLastCanvas = selectionStartCanvas;
                UpdateSelectionRect(selectionStartViewport, selectionStartViewport);
                return;
            case GestureStatus.Running:
                if (!selectionDragging)
                {
                    return;
                }

                var viewportPoint = ClampToPlacementViewport(new Point(
                    selectionStartViewport.X + e.TotalX,
                    selectionStartViewport.Y + e.TotalY));
                var canvasPoint = new Point(
                    viewportPoint.X + PlacementScroll.ScrollX,
                    viewportPoint.Y + PlacementScroll.ScrollY);

                selectionLastViewport = viewportPoint;
                selectionLastCanvas = canvasPoint;
                UpdateSelectionRect(selectionStartViewport, viewportPoint);
                ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, canvasPoint.X, canvasPoint.Y, GestureStatus.Running));
                return;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (!selectionDragging)
                {
                    return;
                }

                selectionDragging = false;
                selectionPanPrimed = false;
                UpdateSelectionRect(selectionStartViewport, selectionLastViewport, hide: true);
                ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, selectionLastCanvas.X, selectionLastCanvas.Y, GestureStatus.Completed));
                return;
            default:
                return;
        }
#endif
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
#if !MACCATALYST
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
        selectionLastViewport = ClampToPlacementViewport(viewportPoint.Value);
        UpdateSelectionRect(selectionStartViewport, selectionLastViewport);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, point.Value.X, point.Value.Y, GestureStatus.Running));
#endif
    }

    private void Selection_PointerReleased(object? sender, PointerEventArgs e)
    {
#if MACCATALYST
        selectionPanPrimed = false;
#else
        if (!selectionDragging)
        {
            return;
        }

        selectionDragging = false;
        ReleaseCapturedPointer();
        var point = GetCanvasPointFromPointer(e) ?? selectionStartCanvas;
        selectionLastCanvas = point;
        selectionLastViewport = ClampToPlacementViewport(e.GetPosition(PlacementScroll) ?? selectionStartViewport);
        UpdateSelectionRect(selectionStartViewport, selectionLastViewport, hide: true);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, point.X, point.Y, GestureStatus.Completed));
#endif
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
#elif MACCATALYST
        return IsPrimaryFromPlatformArgs(e.PlatformArgs);
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
#elif MACCATALYST
        return IsSecondaryFromPlatformArgs(e.PlatformArgs);
#else
        return false;
#endif
    }

    private static bool IsMiddlePointerPressed(PointerEventArgs e)
    {
#if WINDOWS
        var platformArgs = e.PlatformArgs;
        var routedProp = platformArgs?.GetType().GetProperty("PointerRoutedEventArgs");
        var routed = routedProp?.GetValue(platformArgs) as Microsoft.UI.Xaml.Input.PointerRoutedEventArgs;
        return routed?.GetCurrentPoint(null).Properties?.IsMiddleButtonPressed == true;
#elif MACCATALYST
        return IsMiddleFromPlatformArgs(e.PlatformArgs);
#else
        return false;
#endif
    }

    private static bool IsMiddleFromPlatformArgs(object? platformArgs)
    {
        if (platformArgs is null)
        {
            return false;
        }

        if (IsMiddleFromObject(platformArgs))
        {
            return true;
        }

        var gestureRecognizer = TryGetProperty(platformArgs, "GestureRecognizer");
        if (IsMiddleFromObject(gestureRecognizer))
        {
            return true;
        }

        var nativeEvent = TryGetProperty(platformArgs, "Event");
        if (IsMiddleFromObject(nativeEvent))
        {
            return true;
        }

        return false;
    }

    private static bool IsOtherMouseFromPlatformArgs(object? platformArgs)
    {
        if (platformArgs is null)
        {
            return false;
        }

#if MACCATALYST
        var snapshot = BuildPointerDebugSnapshot(platformArgs);
        if (snapshot.Contains("OtherMouse", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
#endif

        var text = platformArgs.ToString() ?? string.Empty;
        return text.Contains("OtherMouse", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPrimaryFromPlatformArgs(object? platformArgs)
    {
        if (platformArgs is null)
        {
            return true;
        }

        if (IsSecondaryFromPlatformArgs(platformArgs) || IsMiddleFromPlatformArgs(platformArgs))
        {
            return false;
        }

        if (IsPrimaryFromObject(platformArgs))
        {
            return true;
        }

        var gestureRecognizer = TryGetProperty(platformArgs, "GestureRecognizer");
        if (IsPrimaryFromObject(gestureRecognizer))
        {
            return true;
        }

        var nativeEvent = TryGetProperty(platformArgs, "Event");
        if (IsPrimaryFromObject(nativeEvent))
        {
            return true;
        }

        return true;
    }

    private static bool IsSecondaryFromPlatformArgs(object? platformArgs)
    {
        if (platformArgs is null)
        {
            return false;
        }

        if (IsSecondaryFromObject(platformArgs))
        {
            return true;
        }

        var gestureRecognizer = TryGetProperty(platformArgs, "GestureRecognizer");
        if (IsSecondaryFromObject(gestureRecognizer))
        {
            return true;
        }

        var nativeEvent = TryGetProperty(platformArgs, "Event");
        if (IsSecondaryFromObject(nativeEvent))
        {
            return true;
        }

        return false;
    }

    private static bool IsPrimaryFromObject(object? source)
    {
        if (source is null)
        {
            return false;
        }

        var leftPressed = TryGetProperty(source, "IsLeftButtonPressed");
        if (leftPressed is bool pressed && pressed)
        {
            return true;
        }

        var pressedButton = TryGetProperty(source, "PressedButton");
        if (IsPrimaryButtonValue(pressedButton))
        {
            return true;
        }

        var button = TryGetProperty(source, "Button");
        if (IsPrimaryButtonValue(button))
        {
            return true;
        }

        var buttons = TryGetProperty(source, "Buttons");
        if (IsPrimaryButtonValue(buttons))
        {
            return true;
        }

        var buttonMask = TryGetProperty(source, "ButtonMask");
        if (TryConvertToUInt64(buttonMask, out var mask) && mask != 0 && (mask & 0x1) != 0 && (mask & ~0x1UL) == 0)
        {
            return true;
        }

        var buttonNumber = TryGetProperty(source, "ButtonNumber");
        if (TryConvertToInt32(buttonNumber, out var number) && number == 0)
        {
            return true;
        }

        var currentEvent = TryGetProperty(source, "CurrentEvent");
        if (currentEvent is not null && !ReferenceEquals(currentEvent, source))
        {
            return IsPrimaryFromObject(currentEvent);
        }

        return false;
    }

    private static bool IsMiddleFromObject(object? source)
    {
        if (source is null)
        {
            return false;
        }

        var eventTypeText = TryGetProperty(source, "Type")?.ToString() ?? string.Empty;
        if (eventTypeText.Contains("OtherMouse", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var middlePressed = TryGetProperty(source, "IsMiddleButtonPressed");
        if (middlePressed is bool pressed && pressed)
        {
            return true;
        }

        var pressedButton = TryGetProperty(source, "PressedButton");
        if (IsMiddleButtonValue(pressedButton))
        {
            return true;
        }

        var button = TryGetProperty(source, "Button");
        if (IsMiddleButtonValue(button))
        {
            return true;
        }

        var buttons = TryGetProperty(source, "Buttons");
        if (IsMiddleButtonValue(buttons))
        {
            return true;
        }

        var buttonNumber = TryGetProperty(source, "ButtonNumber");
        if (TryConvertToInt32(buttonNumber, out var number) && number >= 2)
        {
            return true;
        }
        var looksLikeOtherMouse = eventTypeText.Contains("OtherMouse", StringComparison.OrdinalIgnoreCase);

        var buttonMask = TryGetProperty(source, "ButtonMask");
        if (TryConvertToUInt64(buttonMask, out var mask))
        {
            if ((mask & 0x4) != 0 || (mask & 0x8) != 0 || (mask & 0x10) != 0)
            {
                return true;
            }

            if ((mask & 0x2) != 0 && looksLikeOtherMouse)
            {
                return true;
            }
        }

        var currentEvent = TryGetProperty(source, "CurrentEvent");
        if (currentEvent is not null && !ReferenceEquals(currentEvent, source))
        {
            return IsMiddleFromObject(currentEvent);
        }

        return false;
    }

    private static bool IsSecondaryFromObject(object? source)
    {
        if (source is null)
        {
            return false;
        }

        var rightPressed = TryGetProperty(source, "IsRightButtonPressed");
        if (rightPressed is bool pressed && pressed)
        {
            return true;
        }

        var pressedButton = TryGetProperty(source, "PressedButton");
        if (IsSecondaryButtonValue(pressedButton))
        {
            return true;
        }

        var button = TryGetProperty(source, "Button");
        if (IsSecondaryButtonValue(button))
        {
            return true;
        }

        var buttons = TryGetProperty(source, "Buttons");
        if (IsSecondaryButtonValue(buttons))
        {
            return true;
        }

        var buttonMask = TryGetProperty(source, "ButtonMask");
        if (TryConvertToUInt64(buttonMask, out var mask) && (mask & 0x2) != 0)
        {
            return true;
        }

        var buttonNumber = TryGetProperty(source, "ButtonNumber");
        if (TryConvertToInt32(buttonNumber, out var number) && number == 1)
        {
            return true;
        }

        var currentEvent = TryGetProperty(source, "CurrentEvent");
        if (currentEvent is not null && !ReferenceEquals(currentEvent, source))
        {
            return IsSecondaryFromObject(currentEvent);
        }

        return false;
    }

    private static bool IsMiddleButtonValue(object? value)
    {
        if (value is null)
        {
            return false;
        }

        var text = value.ToString() ?? string.Empty;
        if (text.Contains("Middle", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Auxiliary", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Center", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Tertiary", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Other", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Button2", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Button3", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(text, out var number) && (number == 2 || number == 3);
    }

    private static bool IsSecondaryButtonValue(object? value)
    {
        if (value is null)
        {
            return false;
        }

        if (IsMiddleButtonValue(value))
        {
            return false;
        }

        var text = value.ToString() ?? string.Empty;
        if (text.Contains("Secondary", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Right", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Button1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(text, out var number) && number == 1;
    }

    private static bool IsPrimaryButtonValue(object? value)
    {
        if (value is null)
        {
            return false;
        }

        var text = value.ToString() ?? string.Empty;
        if (text.Contains("Primary", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Left", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Button0", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(text, out var number) && number == 0;
    }

#if MACCATALYST
    private static string BuildPointerDebugSnapshot(object? platformArgs)
    {
        if (platformArgs is null)
        {
            return string.Empty;
        }

        var segments = new List<string>();
        AppendPointerDebugSnapshot(segments, "args", platformArgs);
        var gestureRecognizer = TryGetProperty(platformArgs, "GestureRecognizer");
        if (gestureRecognizer is not null)
        {
            AppendPointerDebugSnapshot(segments, "gesture", gestureRecognizer);
        }

        var nativeEvent = TryGetProperty(platformArgs, "Event");
        if (nativeEvent is not null)
        {
            AppendPointerDebugSnapshot(segments, "event", nativeEvent);
        }

        return string.Join(" | ", segments);
    }

    private static void AppendPointerDebugSnapshot(List<string> segments, string prefix, object source)
    {
        var type = TryGetProperty(source, "Type");
        var pressedButton = TryGetProperty(source, "PressedButton");
        var button = TryGetProperty(source, "Button");
        var buttons = TryGetProperty(source, "Buttons");
        var buttonMask = TryGetProperty(source, "ButtonMask");
        var buttonNumber = TryGetProperty(source, "ButtonNumber");

        var segment = $"{prefix}.src={source.GetType().Name}";
        if (type is not null) segment += $",type={type}";
        if (pressedButton is not null) segment += $",pressed={pressedButton}";
        if (button is not null) segment += $",button={button}";
        if (buttons is not null) segment += $",buttons={buttons}";
        if (buttonMask is not null) segment += $",mask={buttonMask}";
        if (buttonNumber is not null) segment += $",number={buttonNumber}";
        segments.Add(segment);
    }
#endif

    private static bool TryConvertToUInt64(object? value, out ulong number)
    {
        switch (value)
        {
            case null:
                number = 0;
                return false;
            case ulong unsignedLong:
                number = unsignedLong;
                return true;
            case Enum enumValue:
                number = Convert.ToUInt64(enumValue);
                return true;
            default:
                return ulong.TryParse(value.ToString(), out number);
        }
    }

    private static bool TryConvertToInt32(object? value, out int number)
    {
        switch (value)
        {
            case null:
                number = 0;
                return false;
            case int signed:
                number = signed;
                return true;
            case Enum enumValue:
                number = Convert.ToInt32(enumValue);
                return true;
            default:
                return int.TryParse(value.ToString(), out number);
        }
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

    private Point ClampToPlacementViewport(Point point)
    {
        var maxX = Math.Max(0, PlacementScroll.Width);
        var maxY = Math.Max(0, PlacementScroll.Height);
        return new Point(
            Math.Clamp(point.X, 0, maxX),
            Math.Clamp(point.Y, 0, maxY));
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
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(60), FocusModalCommandEntryForOpen);
    }

    private void FocusModalCommandEntryForOpen()
    {
        ModalCommandEntry.Focus();
#if MACCATALYST
        PlaceMacEntryCaretAtEnd(ModalCommandEntry);
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(40), () =>
        {
            if (!viewModel.IsEditorOpen)
            {
                return;
            }

            PlaceMacEntryCaretAtEnd(ModalCommandEntry);
        });
#endif
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
            App.SetContextMenuOpenState(viewModel.IsContextMenuOpen);
            if (viewModel.IsContextMenuOpen)
            {
                CloseCommandSuggestionPopup();
#if MACCATALYST
                ResignMainInputFirstResponder();
#endif
            }
            ApplyTabPolicy();
            if (viewModel.IsContextMenuOpen)
            {
#if MACCATALYST
                SetMacContextMenuPseudoFocus(ContextMenuFocusTarget.Edit);
#endif
                Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(40), () =>
                {
                    FocusContextActionButton(ContextEditButton);
#if MACCATALYST
                    EnsureMacFirstResponder();
#endif
                    ApplyContextActionButtonFocusVisuals();
                });
                Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(140), () =>
                {
                    if (!viewModel.IsContextMenuOpen || IsButtonFocused(ContextEditButton))
                    {
                        return;
                    }

                    FocusContextActionButton(ContextEditButton);
                    ApplyContextActionButtonFocusVisuals();
                });
            }
            else
            {
#if MACCATALYST
                ClearMacContextMenuPseudoFocus();
#endif
            }

            ApplyContextActionButtonFocusVisuals();

            return;
        }

        if (e.PropertyName != nameof(MainViewModel.IsEditorOpen) || !viewModel.IsEditorOpen)
        {
            if (e.PropertyName == nameof(MainViewModel.IsEditorOpen))
            {
                App.SetEditorOpenState(viewModel.IsEditorOpen);
                ApplyTabPolicy();
                UpdateModalEditorHeights();
#if MACCATALYST
                if (!viewModel.IsEditorOpen)
                {
                    ClearMacModalPseudoFocus();
                    macGuidLockedText = string.Empty;
                }
#endif
            }
            return;
        }

        App.SetEditorOpenState(true);
        ApplyTabPolicy();
        UpdateModalEditorHeights();
#if MACCATALYST
        macGuidLockedText = viewModel.Editor.GuidText ?? string.Empty;
#endif
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(60), () =>
        {
            FocusModalCommandEntryForOpen();
#if MACCATALYST
            EnsureMacFirstResponder();
            ApplyMacEditorKeyCommands();
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(220), ApplyMacEditorKeyCommands);
#endif
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

    private void ContextActionButton_Focused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        if (sender is Button focusedButton)
        {
            SyncMacContextMenuPseudoFocusFromButton(focusedButton);
        }
#endif
        ApplyContextActionButtonFocusVisuals();
    }

    private void ContextActionButton_HandlerChanged(object? sender, EventArgs e)
    {
        ApplyContextActionButtonPlatformFocusSettings();
        ApplyContextActionButtonFocusVisuals();
    }

    private void ContextActionButton_Unfocused(object? sender, FocusEventArgs e)
    {
#if MACCATALYST
        if (sender is Button unfocusedButton)
        {
            var target = GetContextMenuTarget(unfocusedButton);
            if (target is not null && macPseudoFocusedContextMenuTarget == target)
            {
                ClearMacContextMenuPseudoFocus();
            }
        }
#endif
        ApplyContextActionButtonFocusVisuals();
    }

    private void FocusContextActionButton(Button button)
    {
#if MACCATALYST
        ResignMainInputFirstResponder();
#endif
        button.Focus();
#if MACCATALYST
        if (button.Handler?.PlatformView is UIResponder responder &&
            responder.CanBecomeFirstResponder &&
            !responder.IsFirstResponder)
        {
            responder.BecomeFirstResponder();
        }
        SyncMacContextMenuPseudoFocusFromButton(button);
#endif
    }

    private void MoveContextMenuFocus(bool forward)
    {
        if (!viewModel.IsContextMenuOpen)
        {
            return;
        }

        var order = new[] { ContextEditButton, ContextDeleteButton };
        var currentIndex = GetCurrentContextMenuFocusIndex(order);
        if (currentIndex < 0)
        {
            FocusContextActionButton(order[0]);
            ApplyContextActionButtonFocusVisuals();
            return;
        }

        var nextIndex = FocusRingNavigator.GetNextIndex(currentIndex, order.Length, forward);
        if (nextIndex < 0)
        {
            return;
        }

        FocusContextActionButton(order[nextIndex]);
        ApplyContextActionButtonFocusVisuals();
    }

    private int GetCurrentContextMenuFocusIndex(Button[] buttons)
    {
#if MACCATALYST
        if (macPseudoFocusedContextMenuTarget is ContextMenuFocusTarget pseudoTarget)
        {
            return pseudoTarget == ContextMenuFocusTarget.Edit ? 0 : 1;
        }
#endif

        for (var i = 0; i < buttons.Length; i++)
        {
            if (IsButtonFocused(buttons[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private void ApplyContextActionButtonFocusVisuals()
    {
        if (!viewModel.IsContextMenuOpen)
        {
            ApplyButtonFocusVisual(ContextEditButton, false);
            ApplyButtonFocusVisual(ContextDeleteButton, false);
            return;
        }

#if MACCATALYST
        if (macPseudoFocusedContextMenuTarget is ContextMenuFocusTarget pseudoTarget)
        {
            ApplyButtonFocusVisual(ContextEditButton, pseudoTarget == ContextMenuFocusTarget.Edit);
            ApplyButtonFocusVisual(ContextDeleteButton, pseudoTarget == ContextMenuFocusTarget.Delete);
            return;
        }
#endif

        ApplyButtonFocusVisual(ContextEditButton, IsButtonFocused(ContextEditButton));
        ApplyButtonFocusVisual(ContextDeleteButton, IsButtonFocused(ContextDeleteButton));
    }

#if MACCATALYST
    private void SyncMacContextMenuPseudoFocusFromButton(Button button)
    {
        var target = GetContextMenuTarget(button);
        if (target is null)
        {
            return;
        }

        SetMacContextMenuPseudoFocus(target.Value);
    }

    private ContextMenuFocusTarget? GetContextMenuTarget(Button button)
    {
        if (ReferenceEquals(button, ContextEditButton))
        {
            return ContextMenuFocusTarget.Edit;
        }

        if (ReferenceEquals(button, ContextDeleteButton))
        {
            return ContextMenuFocusTarget.Delete;
        }

        return null;
    }

    private void SetMacContextMenuPseudoFocus(ContextMenuFocusTarget target)
    {
        macPseudoFocusedContextMenuTarget = target;
    }

    private void ClearMacContextMenuPseudoFocus()
    {
        macPseudoFocusedContextMenuTarget = null;
    }
#endif

    private void ApplyContextActionButtonPlatformFocusSettings()
    {
#if WINDOWS
        DisableWindowsSystemFocusVisual(ContextEditButton);
        DisableWindowsSystemFocusVisual(ContextDeleteButton);
#endif
    }

    private void ConflictActionButton_Focused(object? sender, FocusEventArgs e)
    {
        if (sender is Button focusedButton)
        {
            SyncConflictDialogPseudoFocusFromButton(focusedButton);
        }

        ApplyConflictActionButtonFocusVisuals();
    }

    private void ConflictActionButton_HandlerChanged(object? sender, EventArgs e)
    {
        ApplyConflictActionButtonPlatformFocusSettings();
        ApplyConflictActionButtonFocusVisuals();
    }

    private void ConflictActionButton_Unfocused(object? sender, FocusEventArgs e)
    {
        if (sender is Button unfocusedButton)
        {
            var target = GetConflictDialogTarget(unfocusedButton);
            if (target is not null && conflictDialogPseudoFocusedTarget == target)
            {
                ClearConflictDialogPseudoFocus();
            }
        }

        ApplyConflictActionButtonFocusVisuals();
    }

    private void ApplyConflictActionButtonPlatformFocusSettings()
    {
#if WINDOWS
        DisableWindowsSystemFocusVisual(ConflictReloadButton);
        DisableWindowsSystemFocusVisual(ConflictOverwriteButton);
        DisableWindowsSystemFocusVisual(ConflictCancelButton);
#endif
    }

    private void ApplyConflictActionButtonFocusVisuals()
    {
        if (!IsConflictDialogOpen())
        {
            ApplyButtonFocusVisual(ConflictReloadButton, false);
            ApplyButtonFocusVisual(ConflictOverwriteButton, false);
            ApplyButtonFocusVisual(ConflictCancelButton, false);
            return;
        }

        if (conflictDialogPseudoFocusedTarget is ConflictDialogFocusTarget pseudoTarget)
        {
            ApplyButtonFocusVisual(ConflictReloadButton, pseudoTarget == ConflictDialogFocusTarget.Reload);
            ApplyButtonFocusVisual(ConflictOverwriteButton, pseudoTarget == ConflictDialogFocusTarget.Overwrite);
            ApplyButtonFocusVisual(ConflictCancelButton, pseudoTarget == ConflictDialogFocusTarget.Cancel);
            return;
        }

        ApplyButtonFocusVisual(ConflictReloadButton, IsButtonFocused(ConflictReloadButton));
        ApplyButtonFocusVisual(ConflictOverwriteButton, IsButtonFocused(ConflictOverwriteButton));
        ApplyButtonFocusVisual(ConflictCancelButton, IsButtonFocused(ConflictCancelButton));
    }

    private void SyncConflictDialogPseudoFocusFromButton(Button button)
    {
        var target = GetConflictDialogTarget(button);
        if (target is null)
        {
            return;
        }

        SetConflictDialogPseudoFocus(target.Value);
    }

    private ConflictDialogFocusTarget? GetConflictDialogTarget(Button button)
    {
        if (ReferenceEquals(button, ConflictReloadButton))
        {
            return ConflictDialogFocusTarget.Reload;
        }

        if (ReferenceEquals(button, ConflictOverwriteButton))
        {
            return ConflictDialogFocusTarget.Overwrite;
        }

        if (ReferenceEquals(button, ConflictCancelButton))
        {
            return ConflictDialogFocusTarget.Cancel;
        }

        return null;
    }

    private void SetConflictDialogPseudoFocus(ConflictDialogFocusTarget target)
    {
        conflictDialogPseudoFocusedTarget = target;
    }

    private void ClearConflictDialogPseudoFocus()
    {
        conflictDialogPseudoFocusedTarget = null;
    }

#if WINDOWS
    private static void DisableWindowsSystemFocusVisual(Button button)
    {
        if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Control control)
        {
            return;
        }

        var prop = control.GetType().GetProperty("UseSystemFocusVisuals", BindingFlags.Public | BindingFlags.Instance);
        if (prop?.CanWrite == true)
        {
            prop.SetValue(control, false);
        }
    }
#endif

    private void ApplyButtonFocusVisual(Button button, bool focused)
    {
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var focusedBorderColor = dark ? Color.FromArgb("#F2F2F2") : Color.FromArgb("#1A1A1A");
        if (focused)
        {
            button.BorderColor = focusedBorderColor;
            button.BorderWidth = 1.5;
            return;
        }

        button.BorderColor = Colors.Transparent;
        button.BorderWidth = 0;
    }

    private static bool IsButtonFocused(Button button)
    {
        if (button.IsFocused)
        {
            return true;
        }

#if WINDOWS
        if (button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Control control)
        {
            return control.FocusState != Microsoft.UI.Xaml.FocusState.Unfocused;
        }
#endif

#if MACCATALYST
        if (button.Handler?.PlatformView is UIResponder responder)
        {
            return responder.IsFirstResponder;
        }
#endif

        return false;
    }

    private void ApplyTabPolicy()
    {
#if WINDOWS
        var editorOpen = viewModel.IsEditorOpen;
        var contextOpen = viewModel.IsContextMenuOpen;
        var conflictOpen = IsConflictDialogOpen();
        var mainEnabled = !editorOpen && !contextOpen && !conflictOpen;
        var contextEnabled = contextOpen && !conflictOpen;
        var editorEnabled = editorOpen && !conflictOpen;

        // Main area: only Command and Search are tabbable when modal is closed.
        SetTabStop(MainCommandEntry, mainEnabled);
        SetTabStop(MainSearchEntry, mainEnabled);
        SetTabStop(CreateButton, false);

        // Context menu area: loop only Edit/Delete.
        SetTabStop(ContextEditButton, contextEnabled);
        SetTabStop(ContextDeleteButton, contextEnabled);

        // Modal editor area: keep tab navigation inside modal when open.
        SetTabStop(ModalGuidEntry, editorEnabled);
        SetTabStop(ModalCommandEntry, editorEnabled);
        SetTabStop(ModalButtonTextEntry, editorEnabled);
        SetTabStop(ModalToolEntry, editorEnabled);
        SetTabStop(ModalArgumentsEntry, editorEnabled);
        SetTabStop(ModalClipWordEditor, editorEnabled);
        SetTabStop(ModalNoteEditor, editorEnabled);
        SetTabStop(ModalCancelButton, editorEnabled);
        SetTabStop(ModalSaveButton, editorEnabled);

        // Conflict dialog area: keep tab focus only within conflict action buttons.
        SetTabStop(ConflictReloadButton, conflictOpen);
        SetTabStop(ConflictOverwriteButton, conflictOpen);
        SetTabStop(ConflictCancelButton, conflictOpen);

        // Copy buttons are clickable, but excluded from tab traversal.
        SetTabStop(CopyGuidButton, false);
        SetTabStop(CopyCommandButton, false);
        SetTabStop(CopyButtonTextButton, false);
        SetTabStop(CopyToolButton, false);
        SetTabStop(CopyArgumentsButton, false);
        SetTabStop(CopyClipWordButton, false);
        SetTabStop(CopyNoteButton, false);

        ApplyContextActionButtonFocusVisuals();
        ApplyConflictActionButtonFocusVisuals();
#endif
    }

    private void MainCommandEntry_HandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        EnsureWindowsKeyHooks();
        EnsureWindowsTextBoxHooks();
#endif
#if MACCATALYST
        ApplyMacCommandSuggestionKeyCommands();
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
        if (e.Key == Windows.System.VirtualKey.Tab)
        {
            windowsSelectAllOnTabNavigationPending = true;
        }

        if (TryHandleThemeShortcutFromKey(e))
        {
            e.Handled = true;
        }
    }

    private void CommandTextBox_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        windowsSelectAllOnTabNavigationPending = false;
        if (viewModel.ReopenCommandSuggestionsCommand.CanExecute(null))
        {
            viewModel.ReopenCommandSuggestionsCommand.Execute(null);
        }
    }

    private void WindowsTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Tab)
        {
            windowsSelectAllOnTabNavigationPending = true;
        }
    }

    private void WindowsTextBox_GotFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!windowsSelectAllOnTabNavigationPending)
        {
            return;
        }

        windowsSelectAllOnTabNavigationPending = false;
        if (sender is Microsoft.UI.Xaml.Controls.TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void WindowsTextBox_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        windowsSelectAllOnTabNavigationPending = false;
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
        var shiftDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (IsConflictDialogOpen())
        {
            if (e.Key == Windows.System.VirtualKey.Left)
            {
                MoveConflictDialogFocus(forward: false);
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Right)
            {
                MoveConflictDialogFocus(forward: true);
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Tab)
            {
                MoveConflictDialogFocus(forward: !shiftDown);
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                TryInvokeConflictDialogPrimaryAction();
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                TryInvokeConflictDialogCancelAction();
                e.Handled = true;
                return;
            }
        }

        if (viewModel.IsContextMenuOpen)
        {
            if (e.Key == Windows.System.VirtualKey.Up)
            {
                MoveContextMenuFocus(forward: false);
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Down)
            {
                MoveContextMenuFocus(forward: true);
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Tab)
            {
                MoveContextMenuFocus(forward: !shiftDown);
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                if (viewModel.CloseContextMenuCommand.CanExecute(null))
                {
                    viewModel.CloseContextMenuCommand.Execute(null);
                    e.Handled = true;
                }
                return;
            }
        }

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
        SyncWindowsTextBoxHooks(
            ref commandTextBox,
            MainCommandEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            CommandTextBox_KeyDown,
            CommandTextBox_PointerPressed);

        SyncWindowsTextBoxHooks(
            ref searchTextBox,
            MainSearchEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            SearchTextBox_KeyDown);

        SyncWindowsTextBoxHooks(
            ref modalGuidTextBox,
            ModalGuidEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox);
        SyncWindowsTextBoxHooks(
            ref modalCommandTextBox,
            ModalCommandEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox);
        SyncWindowsTextBoxHooks(
            ref modalButtonTextTextBox,
            ModalButtonTextEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox);
        SyncWindowsTextBoxHooks(
            ref modalToolTextBox,
            ModalToolEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox);
        SyncWindowsTextBoxHooks(
            ref modalArgumentsTextBox,
            ModalArgumentsEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox);
        SyncWindowsTextBoxHooks(
            ref modalClipWordTextBox,
            ModalClipWordEditor.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox);
        SyncWindowsTextBoxHooks(
            ref modalNoteTextBox,
            ModalNoteEditor.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox);
    }

    private void SyncWindowsTextBoxHooks(
        ref Microsoft.UI.Xaml.Controls.TextBox? slot,
        Microsoft.UI.Xaml.Controls.TextBox? current,
        Microsoft.UI.Xaml.Input.KeyEventHandler? extraKeyDown = null,
        Microsoft.UI.Xaml.Input.PointerEventHandler? extraPointerPressed = null)
    {
        if (ReferenceEquals(slot, current))
        {
            return;
        }

        if (slot is not null)
        {
            slot.KeyDown -= WindowsTextBox_KeyDown;
            slot.GotFocus -= WindowsTextBox_GotFocus;
            slot.PointerPressed -= WindowsTextBox_PointerPressed;
            if (extraKeyDown is not null)
            {
                slot.KeyDown -= extraKeyDown;
            }

            if (extraPointerPressed is not null)
            {
                slot.PointerPressed -= extraPointerPressed;
            }
        }

        slot = current;
        if (slot is null)
        {
            return;
        }

        slot.KeyDown += WindowsTextBox_KeyDown;
        slot.GotFocus += WindowsTextBox_GotFocus;
        slot.PointerPressed += WindowsTextBox_PointerPressed;
        if (extraKeyDown is not null)
        {
            slot.KeyDown += extraKeyDown;
        }

        if (extraPointerPressed is not null)
        {
            slot.PointerPressed += extraPointerPressed;
        }
    }

#endif

    private void CommandSuggestionsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var oldItem in e.OldItems.OfType<CommandSuggestionItemViewModel>())
            {
                oldItem.PropertyChanged -= CommandSuggestionItemOnPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var newItem in e.NewItems.OfType<CommandSuggestionItemViewModel>())
            {
                newItem.PropertyChanged += CommandSuggestionItemOnPropertyChanged;
            }
        }

        Dispatcher.Dispatch(RebuildCommandSuggestionStack);
    }

    private void CommandSuggestionItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CommandSuggestionItemViewModel.IsSelected))
        {
            Dispatcher.Dispatch(RebuildCommandSuggestionStack);
        }
    }

    private void OnThemeShortcutRequested(string mode)
    {
        Dispatcher.Dispatch(() => ApplyThemeShortcut(mode));
    }

    private void OnEditorShortcutRequested(string action)
    {
        Dispatcher.Dispatch(() =>
        {
            if (IsConflictDialogOpen())
            {
                if (string.Equals(action, "ConflictDialogNext", StringComparison.OrdinalIgnoreCase))
                {
                    MoveConflictDialogFocus(forward: true);
                    return;
                }

                if (string.Equals(action, "ConflictDialogPrevious", StringComparison.OrdinalIgnoreCase))
                {
                    MoveConflictDialogFocus(forward: false);
                    return;
                }

                if (string.Equals(action, "TabNext", StringComparison.OrdinalIgnoreCase))
                {
                    MoveConflictDialogFocus(forward: true);
                    return;
                }

                if (string.Equals(action, "TabPrevious", StringComparison.OrdinalIgnoreCase))
                {
                    MoveConflictDialogFocus(forward: false);
                    return;
                }

                if (string.Equals(action, "Cancel", StringComparison.OrdinalIgnoreCase))
                {
                    TryInvokeConflictDialogCancelAction();
                    return;
                }

                if (string.Equals(action, "PrimaryAction", StringComparison.OrdinalIgnoreCase))
                {
                    TryInvokeConflictDialogPrimaryAction();
                    return;
                }
            }

            if (viewModel.IsContextMenuOpen)
            {
                if (string.Equals(action, "ContextMenuNext", StringComparison.OrdinalIgnoreCase))
                {
                    MoveContextMenuFocus(forward: true);
                    return;
                }

                if (string.Equals(action, "ContextMenuPrevious", StringComparison.OrdinalIgnoreCase))
                {
                    MoveContextMenuFocus(forward: false);
                    return;
                }

                if (string.Equals(action, "TabNext", StringComparison.OrdinalIgnoreCase))
                {
                    MoveContextMenuFocus(forward: true);
                    return;
                }

                if (string.Equals(action, "TabPrevious", StringComparison.OrdinalIgnoreCase))
                {
                    MoveContextMenuFocus(forward: false);
                    return;
                }

                if (string.Equals(action, "Cancel", StringComparison.OrdinalIgnoreCase))
                {
                    if (viewModel.CloseContextMenuCommand.CanExecute(null))
                    {
                        viewModel.CloseContextMenuCommand.Execute(null);
                    }
                    return;
                }

                if (string.Equals(action, "PrimaryAction", StringComparison.OrdinalIgnoreCase))
                {
                    TryInvokeContextMenuPrimaryAction();
                    return;
                }
            }

            if (!viewModel.IsEditorOpen)
            {
                return;
            }

            if (string.Equals(action, "Save", StringComparison.OrdinalIgnoreCase))
            {
#if MACCATALYST
                ClearMacModalPseudoFocus();
#endif
                if (viewModel.SaveEditorCommand.CanExecute(null))
                {
                    viewModel.SaveEditorCommand.Execute(null);
                }

                return;
            }

            if (string.Equals(action, "PrimaryAction", StringComparison.OrdinalIgnoreCase))
            {
#if MACCATALYST
                if (macPseudoFocusedModalTarget == ModalFocusTarget.CancelButton)
                {
                    ClearMacModalPseudoFocus();
                    if (viewModel.CancelEditorCommand.CanExecute(null))
                    {
                        viewModel.CancelEditorCommand.Execute(null);
                    }
                }
                else if (macPseudoFocusedModalTarget == ModalFocusTarget.SaveButton)
                {
                    ClearMacModalPseudoFocus();
                    if (viewModel.SaveEditorCommand.CanExecute(null))
                    {
                        viewModel.SaveEditorCommand.Execute(null);
                    }
                }
#endif
                return;
            }

            if (string.Equals(action, "TabNext", StringComparison.OrdinalIgnoreCase))
            {
#if MACCATALYST
                MoveModalFocus(forward: true);
#endif
                return;
            }

            if (string.Equals(action, "TabPrevious", StringComparison.OrdinalIgnoreCase))
            {
#if MACCATALYST
                MoveModalFocus(forward: false);
#endif
                return;
            }

            if (!string.Equals(action, "Cancel", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

#if MACCATALYST
            ClearMacModalPseudoFocus();
#endif
            if (viewModel.CancelEditorCommand.CanExecute(null))
            {
                viewModel.CancelEditorCommand.Execute(null);
            }
        });
    }

    private void TryInvokeContextMenuPrimaryAction()
    {
        if (!viewModel.IsContextMenuOpen)
        {
            return;
        }

#if MACCATALYST
        if (macPseudoFocusedContextMenuTarget == ContextMenuFocusTarget.Delete)
        {
            if (viewModel.ContextDeleteCommand.CanExecute(null))
            {
                viewModel.ContextDeleteCommand.Execute(null);
            }
            return;
        }

        if (macPseudoFocusedContextMenuTarget == ContextMenuFocusTarget.Edit)
        {
            if (viewModel.ContextEditCommand.CanExecute(null))
            {
                viewModel.ContextEditCommand.Execute(null);
            }
            return;
        }
#endif

        if (IsButtonFocused(ContextDeleteButton))
        {
            if (viewModel.ContextDeleteCommand.CanExecute(null))
            {
                viewModel.ContextDeleteCommand.Execute(null);
            }
            return;
        }

        if (viewModel.ContextEditCommand.CanExecute(null))
        {
            viewModel.ContextEditCommand.Execute(null);
        }
    }

    private void OnCommandInputShortcutRequested(string action)
    {
#if MACCATALYST
        Dispatcher.Dispatch(() =>
        {
            if (!IsMainCommandEntryReadyForSuggestionNavigation())
            {
                return;
            }

            if (string.Equals(action, "Up", StringComparison.OrdinalIgnoreCase))
            {
                if (viewModel.MoveSuggestionUpCommand.CanExecute(null))
                {
                    viewModel.MoveSuggestionUpCommand.Execute(null);
                }

                return;
            }

            if (string.Equals(action, "Down", StringComparison.OrdinalIgnoreCase) &&
                viewModel.MoveSuggestionDownCommand.CanExecute(null))
            {
                viewModel.MoveSuggestionDownCommand.Execute(null);
            }
        });
#endif
    }

    private void OnMiddleMouseClickRequested()
    {
#if MACCATALYST
        Dispatcher.Dispatch(() =>
        {
            if (lastPointerOnRoot is null)
            {
                return;
            }

            HandleMacMiddleClick(lastPointerOnRoot.Value);
        });
#endif
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
            SetConflictDialogPseudoFocus(ConflictDialogFocusTarget.Cancel);
            ConflictOverlay.IsVisible = true;
            App.SetConflictDialogOpenState(true);
            UpdateConflictDialogModalState(isOpen: true);
            CloseCommandSuggestionPopup();
            ApplyTabPolicy();
            ApplyConflictActionButtonFocusVisuals();
            FocusConflictDialogActionButton(ConflictCancelButton, ConflictDialogFocusTarget.Cancel);
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(120), () =>
            {
                if (!IsConflictDialogOpen() || IsButtonFocused(ConflictCancelButton))
                {
                    return;
                }

                FocusConflictDialogActionButton(ConflictCancelButton, ConflictDialogFocusTarget.Cancel);
            });
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
        ClearConflictDialogPseudoFocus();
        App.SetConflictDialogOpenState(false);
        UpdateConflictDialogModalState(isOpen: false);
        ApplyTabPolicy();
        ApplyConflictActionButtonFocusVisuals();
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

    private bool IsConflictDialogOpen()
    {
        if (!xamlLoaded)
        {
            return false;
        }

        return editorConflictTcs is not null && ConflictOverlay.IsVisible;
    }

    private void UpdateConflictDialogModalState(bool isOpen)
    {
        if (!xamlLoaded)
        {
            return;
        }

        EditorOverlay.InputTransparent = isOpen;
        EditorOverlay.IsEnabled = !isOpen;
#if MACCATALYST
        if (isOpen)
        {
            ResignModalInputFirstResponder();
            ResignMainInputFirstResponder();
        }
#endif
    }

    private void FocusConflictDialogActionButton(Button button, ConflictDialogFocusTarget target)
    {
        SetConflictDialogPseudoFocus(target);
#if MACCATALYST
        ResignModalInputFirstResponder();
        ResignMainInputFirstResponder();
#endif
        ApplyConflictActionButtonFocusVisuals();
        button.Focus();
#if MACCATALYST
        if (button.Handler?.PlatformView is UIResponder responder &&
            responder.CanBecomeFirstResponder &&
            !responder.IsFirstResponder)
        {
            responder.BecomeFirstResponder();
        }
#endif
        ApplyConflictActionButtonFocusVisuals();
    }

    private int GetCurrentConflictDialogFocusIndex(Button[] buttons)
    {
        for (var i = 0; i < buttons.Length; i++)
        {
            if (IsButtonFocused(buttons[i]))
            {
                return i;
            }
        }

        return conflictDialogPseudoFocusedTarget switch
        {
            ConflictDialogFocusTarget.Reload => 0,
            ConflictDialogFocusTarget.Overwrite => 1,
            ConflictDialogFocusTarget.Cancel => 2,
            _ => -1,
        };
    }

    private void MoveConflictDialogFocus(bool forward)
    {
        if (!IsConflictDialogOpen())
        {
            return;
        }

        var order = new[]
        {
            (Button: ConflictReloadButton, Target: ConflictDialogFocusTarget.Reload),
            (Button: ConflictOverwriteButton, Target: ConflictDialogFocusTarget.Overwrite),
            (Button: ConflictCancelButton, Target: ConflictDialogFocusTarget.Cancel),
        };

        var buttons = new[] { ConflictReloadButton, ConflictOverwriteButton, ConflictCancelButton };
        var currentIndex = GetCurrentConflictDialogFocusIndex(buttons);
        if (currentIndex < 0)
        {
            FocusConflictDialogActionButton(ConflictCancelButton, ConflictDialogFocusTarget.Cancel);
            return;
        }

        var nextIndex = FocusRingNavigator.GetNextIndex(currentIndex, order.Length, forward);
        if (nextIndex < 0)
        {
            return;
        }

        FocusConflictDialogActionButton(order[nextIndex].Button, order[nextIndex].Target);
    }

    private void TryInvokeConflictDialogPrimaryAction()
    {
        if (!IsConflictDialogOpen())
        {
            return;
        }

        var order = new[] { ConflictReloadButton, ConflictOverwriteButton, ConflictCancelButton };
        var currentIndex = GetCurrentConflictDialogFocusIndex(order);
        switch (currentIndex)
        {
            case 0:
                CloseEditorConflictDialog(EditorConflictResolution.Reload);
                return;
            case 1:
                CloseEditorConflictDialog(EditorConflictResolution.Overwrite);
                return;
            case 2:
                CloseEditorConflictDialog(EditorConflictResolution.Cancel);
                return;
            default:
                CloseEditorConflictDialog(EditorConflictResolution.Cancel);
                return;
        }
    }

    private void TryInvokeConflictDialogCancelAction()
    {
        if (!IsConflictDialogOpen())
        {
            return;
        }

        CloseEditorConflictDialog(EditorConflictResolution.Cancel);
    }

    private void RootGrid_PointerPressed(object? sender, PointerEventArgs e)
    {
        if (suppressNextRootSuggestionClose)
        {
            suppressNextRootSuggestionClose = false;
            return;
        }

        var pointer = e.GetPosition(RootGrid);
        if (pointer is not null)
        {
            lastPointerOnRoot = pointer.Value;
        }

#if MACCATALYST
        if (pointer is not null && IsMiddlePointerPressed(e) && !IsSecondaryPointerPressed(e))
        {
            if (HandleMacMiddleClick(pointer.Value))
            {
                return;
            }
        }
#endif

        if (!viewModel.IsCommandSuggestionOpen)
        {
            return;
        }

        if (pointer is null)
        {
            return;
        }

        if (IsPointInsideElement(pointer.Value, MainCommandEntry) || IsPointInsideElement(pointer.Value, CommandSuggestionPopup))
        {
            return;
        }

        CloseCommandSuggestionPopup();
    }

    private void RootGrid_PointerMoved(object? sender, PointerEventArgs e)
    {
        var pointer = e.GetPosition(RootGrid);
        if (pointer is not null)
        {
            lastPointerOnRoot = pointer.Value;
        }
    }

#if MACCATALYST
    private bool HandleMacMiddleClick(Point rootPoint)
    {
        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen)
        {
            return false;
        }

        var hit = TryGetPlacementButtonAtRootPoint(rootPoint) ?? TryGetDockButtonAtRootPoint(rootPoint);
        if (hit is null)
        {
            return false;
        }

        suppressTapExecuteForItemId = hit.Id;
        if (viewModel.OpenEditorCommand.CanExecute(hit))
        {
            viewModel.OpenEditorCommand.Execute(hit);
            return true;
        }

        return false;
    }

    private LauncherButtonItemViewModel? TryGetPlacementButtonAtRootPoint(Point rootPoint)
    {
        var local = TryConvertRootPointToElementLocal(rootPoint, PlacementScroll);
        if (local is null)
        {
            return null;
        }

        var canvasPoint = new Point(local.Value.X + PlacementScroll.ScrollX, local.Value.Y + PlacementScroll.ScrollY);
        return viewModel.VisibleButtons.LastOrDefault(item =>
            canvasPoint.X >= item.X &&
            canvasPoint.X <= item.X + item.Width &&
            canvasPoint.Y >= item.Y &&
            canvasPoint.Y <= item.Y + item.Height);
    }

    private LauncherButtonItemViewModel? TryGetDockButtonAtRootPoint(Point rootPoint)
    {
        var local = TryConvertRootPointToElementLocal(rootPoint, DockScroll);
        if (local is null)
        {
            return null;
        }

        var x = local.Value.X + DockScroll.ScrollX;
        var y = local.Value.Y + DockScroll.ScrollY;
        const double spacing = 10;
        var cursor = 0.0;

        foreach (var item in viewModel.DockButtons)
        {
            var width = Math.Max(1, item.Width);
            var height = Math.Max(1, item.Height);
            if (x >= cursor && x <= cursor + width && y >= 0 && y <= height)
            {
                return item;
            }

            cursor += width + spacing;
        }

        return null;
    }

    private Point? TryConvertRootPointToElementLocal(Point rootPoint, VisualElement element)
    {
        if (element.Width <= 0 || element.Height <= 0)
        {
            return null;
        }

        var offset = GetPositionRelativeToAncestor(element, RootGrid);
        var local = new Point(rootPoint.X - offset.X, rootPoint.Y - offset.Y);
        if (local.X < 0 || local.Y < 0 || local.X > element.Width || local.Y > element.Height)
        {
            return null;
        }

        return local;
    }
#endif

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

#if !MACCATALYST
    private void RebuildCommandSuggestionStack()
    {
        if (!xamlLoaded)
        {
            return;
        }

        CommandSuggestionStack.Children.Clear();
        foreach (var item in viewModel.CommandSuggestions)
        {
            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                    new ColumnDefinition(new GridLength(4, GridUnitType.Star)),
                },
                ColumnSpacing = 10,
                Padding = new Thickness(8, 6),
                MinimumHeightRequest = 34,
                HorizontalOptions = LayoutOptions.Fill,
                BackgroundColor = item.IsSelected
                    ? (Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#3D3D3D") : Color.FromArgb("#E6E6E6"))
                    : Colors.Transparent,
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                if (viewModel.PickSuggestionCommand.CanExecute(item))
                {
                    viewModel.PickSuggestionCommand.Execute(item);
                }
            };
            row.GestureRecognizers.Add(tap);

            var commandLabel = new Label
            {
                Text = item.Command,
                LineBreakMode = LineBreakMode.TailTruncation,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#F2F2F2")
                    : Color.FromArgb("#111111"),
            };
            row.Children.Add(commandLabel);

            var buttonTextLabel = new Label
            {
                Text = item.ButtonText,
                LineBreakMode = LineBreakMode.TailTruncation,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#F2F2F2")
                    : Color.FromArgb("#111111"),
            };
            row.SetColumn(buttonTextLabel, 1);
            row.Children.Add(buttonTextLabel);

            var toolArgsLabel = new Label
            {
                Text = item.ToolArguments,
                LineBreakMode = LineBreakMode.TailTruncation,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#F2F2F2")
                    : Color.FromArgb("#111111"),
            };
            row.SetColumn(toolArgsLabel, 2);
            row.Children.Add(toolArgsLabel);

            CommandSuggestionStack.Children.Add(row);
        }
    }
#endif

#if MACCATALYST
    private void MoveModalFocus(bool forward)
    {
        if (ModalFocusOrder.Length == 0)
        {
            return;
        }

        var currentIndex = GetCurrentModalFocusIndex();
        if (currentIndex < 0)
        {
            TryFocusModalCommandTarget();
            return;
        }

        var step = forward ? 1 : -1;
        var probe = currentIndex;
        for (var i = 0; i < ModalFocusOrder.Length; i++)
        {
            probe = (probe + step + ModalFocusOrder.Length) % ModalFocusOrder.Length;
            if (TryActivateModalFocusTarget(ModalFocusOrder[probe]))
            {
                return;
            }
        }
    }

    private static bool IsModalFocusTargetActive(VisualElement target)
    {
        if (target.IsFocused)
        {
            return true;
        }

        return target.Handler?.PlatformView is UIResponder responder && responder.IsFirstResponder;
    }

    private static void FocusModalTarget(VisualElement target)
    {
        if (target.Focus())
        {
            return;
        }

        if (target.Handler?.PlatformView is UIResponder responder && responder.CanBecomeFirstResponder)
        {
            responder.BecomeFirstResponder();
        }
    }

    private int GetCurrentModalFocusIndex()
    {
        if (macPseudoFocusedModalTarget is ModalFocusTarget pseudoTarget)
        {
            return Array.IndexOf(ModalFocusOrder, pseudoTarget);
        }

        for (var i = 0; i < ModalFocusOrder.Length; i++)
        {
            if (IsModalTargetActive(ModalFocusOrder[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsModalTargetActive(ModalFocusTarget target)
    {
        return target switch
        {
            ModalFocusTarget.Guid => IsModalFocusTargetActive(ModalGuidEntry),
            ModalFocusTarget.Command => IsModalFocusTargetActive(ModalCommandEntry),
            ModalFocusTarget.ButtonText => IsModalFocusTargetActive(ModalButtonTextEntry),
            ModalFocusTarget.Tool => IsModalFocusTargetActive(ModalToolEntry),
            ModalFocusTarget.Arguments => IsModalFocusTargetActive(ModalArgumentsEntry),
            ModalFocusTarget.ClipWord => IsModalFocusTargetActive(ModalClipWordEditor),
            ModalFocusTarget.Note => IsModalFocusTargetActive(ModalNoteEditor),
            ModalFocusTarget.CancelButton => macPseudoFocusedModalTarget == ModalFocusTarget.CancelButton,
            ModalFocusTarget.SaveButton => macPseudoFocusedModalTarget == ModalFocusTarget.SaveButton,
            _ => false,
        };
    }

    private bool TryActivateModalFocusTarget(ModalFocusTarget target)
    {
        switch (target)
        {
            case ModalFocusTarget.Guid:
                return TryFocusModalGuidTarget();
            case ModalFocusTarget.Command:
                return TryFocusModalCommandTarget();
            case ModalFocusTarget.ButtonText:
                return TryFocusModalVisual(ModalButtonTextEntry);
            case ModalFocusTarget.Tool:
                return TryFocusModalVisual(ModalToolEntry);
            case ModalFocusTarget.Arguments:
                return TryFocusModalVisual(ModalArgumentsEntry);
            case ModalFocusTarget.ClipWord:
                return TryFocusModalVisual(ModalClipWordEditor);
            case ModalFocusTarget.Note:
                return TryFocusModalVisual(ModalNoteEditor);
            case ModalFocusTarget.CancelButton:
                SetMacModalPseudoFocus(ModalFocusTarget.CancelButton);
                return true;
            case ModalFocusTarget.SaveButton:
                SetMacModalPseudoFocus(ModalFocusTarget.SaveButton);
                return true;
            default:
                return false;
        }
    }

    private bool TryFocusModalGuidTarget()
    {
        ClearMacModalPseudoFocus();
        FocusModalTarget(ModalGuidEntry);
        EnsureMacGuidEntryReadOnlyBehavior();

        if (ModalGuidEntry.Handler?.PlatformView is UITextField textField)
        {
            if (!textField.IsFirstResponder)
            {
                textField.BecomeFirstResponder();
            }

            var allTextRange = textField.GetTextRange(textField.BeginningOfDocument, textField.EndOfDocument);
            if (allTextRange is not null)
            {
                textField.SelectedTextRange = allTextRange;
            }

            return textField.IsFirstResponder;
        }

        return IsModalFocusTargetActive(ModalGuidEntry);
    }

    private bool TryFocusModalVisual(VisualElement target)
    {
        ClearMacModalPseudoFocus();
        FocusModalTarget(target);
        return IsModalFocusTargetActive(target);
    }

    private bool TryFocusModalCommandTarget()
    {
        if (!TryFocusModalVisual(ModalCommandEntry))
        {
            return false;
        }

        PlaceMacEntryCaretAtEnd(ModalCommandEntry);
        return IsModalFocusTargetActive(ModalCommandEntry);
    }

    private static void PlaceMacEntryCaretAtEnd(Entry entry)
    {
        if (entry.Handler?.PlatformView is not UITextField textField)
        {
            return;
        }

        if (!textField.IsFirstResponder && textField.CanBecomeFirstResponder)
        {
            textField.BecomeFirstResponder();
        }

        var caretOffset = TextCaretPositionResolver.ResolveTailOffset(textField.Text);
        var caretPosition = textField.GetPosition(textField.BeginningOfDocument, caretOffset) ?? textField.EndOfDocument;
        if (caretPosition is null)
        {
            return;
        }

        var caretRange = textField.GetTextRange(caretPosition, caretPosition);
        if (caretRange is not null)
        {
            textField.SelectedTextRange = caretRange;
        }
    }

    private void SetMacModalPseudoFocus(ModalFocusTarget target)
    {
        ResignModalInputFirstResponder();
        macPseudoFocusedModalTarget = target;
        ApplyMacModalPseudoFocusVisuals();
        ApplyMacEditorKeyCommands();
    }

    private void ClearMacModalPseudoFocus()
    {
        if (macPseudoFocusedModalTarget is null)
        {
            return;
        }

        macPseudoFocusedModalTarget = null;
        ApplyMacModalPseudoFocusVisuals();
        ApplyMacEditorKeyCommands();
    }

    private void ApplyMacModalPseudoFocusVisuals()
    {
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var focusedBorderColor = dark ? Color.FromArgb("#F2F2F2") : Color.FromArgb("#1A1A1A");

        ApplyMacPseudoFocusVisual(ModalCancelButton, macPseudoFocusedModalTarget == ModalFocusTarget.CancelButton, focusedBorderColor);
        ApplyMacPseudoFocusVisual(ModalSaveButton, macPseudoFocusedModalTarget == ModalFocusTarget.SaveButton, focusedBorderColor);
    }

    private static void ApplyMacPseudoFocusVisual(Button button, bool focused, Color focusedBorderColor)
    {
        if (focused)
        {
            button.BorderColor = focusedBorderColor;
            button.BorderWidth = 1.5;
            return;
        }

        button.BorderColor = Colors.Transparent;
        button.BorderWidth = 0;
    }

    private void RebuildCommandSuggestionStack()
    {
        if (!xamlLoaded)
        {
            return;
        }

        CommandSuggestionStack.Children.Clear();
        foreach (var item in viewModel.CommandSuggestions)
        {
            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                    new ColumnDefinition(new GridLength(4, GridUnitType.Star)),
                },
                ColumnSpacing = 10,
                Padding = new Thickness(8, 6),
                MinimumHeightRequest = 34,
                HorizontalOptions = LayoutOptions.Fill,
                BackgroundColor = item.IsSelected
                    ? (Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#3D3D3D") : Color.FromArgb("#E6E6E6"))
                    : Colors.Transparent,
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                if (viewModel.PickSuggestionCommand.CanExecute(item))
                {
                    viewModel.PickSuggestionCommand.Execute(item);
                }
            };
            row.GestureRecognizers.Add(tap);

            var commandLabel = new Label
            {
                Text = item.Command,
                LineBreakMode = LineBreakMode.TailTruncation,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#F2F2F2")
                    : Color.FromArgb("#111111"),
            };
            row.Children.Add(commandLabel);

            var buttonTextLabel = new Label
            {
                Text = item.ButtonText,
                LineBreakMode = LineBreakMode.TailTruncation,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#F2F2F2")
                    : Color.FromArgb("#111111"),
            };
            row.SetColumn(buttonTextLabel, 1);
            row.Children.Add(buttonTextLabel);

            var toolArgsLabel = new Label
            {
                Text = item.ToolArguments,
                LineBreakMode = LineBreakMode.TailTruncation,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#F2F2F2")
                    : Color.FromArgb("#111111"),
            };
            row.SetColumn(toolArgsLabel, 2);
            row.Children.Add(toolArgsLabel);

            CommandSuggestionStack.Children.Add(row);
        }
    }

    private void ApplyMacVisualTuning()
    {
        EnsureMacFirstResponder();
        ApplyMacContentScale();
        ApplyMacClipWordEditorVisualState();
        ApplyMacNoteEditorVisualState();
        ApplyMacModalPseudoFocusVisuals();
        ApplyMacCommandSuggestionKeyCommands();
        ApplyMacEditorKeyCommands();
    }

    private void ApplyMacClipWordEditorVisualState()
    {
        if (ModalClipWordEditor.Handler?.PlatformView is not UITextView textView)
        {
            return;
        }

        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        textView.TintColor = dark ? UIColor.White : UIColor.Black;
    }

    private void ApplyMacInitialCommandFocus()
    {
        if (macInitialCommandFocusApplied || !xamlLoaded || viewModel.IsEditorOpen || !IsMacAppForegroundActive())
        {
            return;
        }

        macInitialCommandFocusApplied = true;
        Dispatcher.Dispatch(() => MainCommandEntry.Focus());
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(140), () =>
        {
            if (!IsMacAppForegroundActive())
            {
                return;
            }

            if (!MainCommandEntry.IsFocused)
            {
                MainCommandEntry.Focus();
            }
        });
    }

    private void ApplyMacNoteEditorVisualState()
    {
        if (ModalNoteEditor.Handler?.PlatformView is not UITextView textView)
        {
            return;
        }

        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        textView.TintColor = dark ? UIColor.White : UIColor.Black;
    }

    private void TryHandleMacEditorTabTextInsertion(object? sender, TextChangedEventArgs e)
    {
        if (macSuppressEditorTabFallback || !viewModel.IsEditorOpen || sender is not Editor editor || !editor.IsFocused)
        {
            return;
        }

        if (!EditorTabInsertionResolver.TryResolveNavigationAction(e.OldTextValue, e.NewTextValue, out var action, out var insertedIndex))
        {
            return;
        }

        var newText = e.NewTextValue ?? string.Empty;
        var sanitizedText = newText.Remove(insertedIndex, 1);
        try
        {
            macSuppressEditorTabFallback = true;
            editor.Text = sanitizedText;
        }
        finally
        {
            macSuppressEditorTabFallback = false;
        }

        MoveModalFocus(forward: string.Equals(action, "TabNext", StringComparison.Ordinal));
    }

    private bool IsMainCommandEntryReadyForSuggestionNavigation()
    {
        if (!xamlLoaded || viewModel.IsEditorOpen || viewModel.IsContextMenuOpen || IsConflictDialogOpen())
        {
            return false;
        }

        if (viewModel.IsCommandSuggestionOpen)
        {
            return true;
        }

        if (MainCommandEntry.IsFocused)
        {
            return true;
        }

        return MainCommandEntry.Handler?.PlatformView is UIResponder responder && responder.IsFirstResponder;
    }

    private void ApplyMacEditorKeyCommands()
    {
        if (!macDynamicKeyCommandRegistrationEnabled)
        {
            return;
        }

        modalEscapeKeyCommand ??= CreateMacEscapeKeyCommand();
        modalSaveKeyCommand ??= UIKeyCommand.Create(new NSString("s"), UIKeyModifierFlags.Command, new Selector("handleEditorSave:"));
        modalTabNextKeyCommand ??= TryCreateMacEditorKeyCommand(macTabKeyInput, 0, "handleEditorTabNext:");
        modalTabPreviousKeyCommand ??= TryCreateMacEditorKeyCommand(macTabKeyInput, UIKeyModifierFlags.Shift, "handleEditorTabPrevious:");
        modalPrimaryActionKeyCommand ??= TryCreateMacEditorKeyCommand(macReturnKeyInput, 0, "handleEditorPrimaryAction:");
        if (!string.IsNullOrEmpty(macEnterKeyInput))
        {
            modalPrimaryActionAlternateKeyCommand ??= TryCreateMacEditorKeyCommand(macEnterKeyInput!, 0, "handleEditorPrimaryAction:");
        }
        EnsureMacGuidEntryReadOnlyBehavior();
        var includePrimaryAction =
            macPseudoFocusedModalTarget is ModalFocusTarget.CancelButton or ModalFocusTarget.SaveButton &&
            modalPrimaryActionKeyCommand is not null;

        var responders = new UIResponder?[]
        {
            ModalCommandEntry.Handler?.PlatformView as UIResponder,
            ModalGuidEntry.Handler?.PlatformView as UIResponder,
            ModalButtonTextEntry.Handler?.PlatformView as UIResponder,
            ModalToolEntry.Handler?.PlatformView as UIResponder,
            ModalArgumentsEntry.Handler?.PlatformView as UIResponder,
            ModalClipWordEditor.Handler?.PlatformView as UIResponder,
            ModalNoteEditor.Handler?.PlatformView as UIResponder,
            ModalCancelButton.Handler?.PlatformView as UIResponder,
            ModalSaveButton.Handler?.PlatformView as UIResponder,
            UIApplication.SharedApplication.Delegate as UIResponder,
        };

        foreach (var responder in responders)
        {
            if (viewModel.IsEditorOpen)
            {
                RegisterMacEditorCommands(responder, includePrimaryAction);
            }
            else
            {
                UnregisterMacEditorCommands(responder);
            }
        }

        if (Window?.Handler?.PlatformView is UIWindow nativeWindow)
        {
            if (viewModel.IsEditorOpen)
            {
                RegisterMacEditorCommands(nativeWindow, includePrimaryAction);
                RegisterMacEditorCommands(nativeWindow.RootViewController, includePrimaryAction);
            }
            else
            {
                UnregisterMacEditorCommands(nativeWindow);
                UnregisterMacEditorCommands(nativeWindow.RootViewController);
            }
        }
    }

    private void ApplyMacCommandSuggestionKeyCommands()
    {
        if (!macDynamicKeyCommandRegistrationEnabled)
        {
            return;
        }

        commandSuggestionUpKeyCommand ??= CreateMacCommandSuggestionKeyCommand(macUpArrowKeyInput, "handleCommandSuggestionUp:");
        commandSuggestionDownKeyCommand ??= CreateMacCommandSuggestionKeyCommand(macDownArrowKeyInput, "handleCommandSuggestionDown:");

        RegisterMacCommandSuggestionCommands(MainCommandEntry.Handler?.PlatformView as UIResponder);
        if (Window?.Handler?.PlatformView is UIWindow nativeWindow)
        {
            RegisterMacCommandSuggestionCommands(nativeWindow);
            RegisterMacCommandSuggestionCommands(nativeWindow.RootViewController);
        }
    }

    private void RegisterMacCommandSuggestionCommands(UIResponder? responder)
    {
        if (responder is null || commandSuggestionUpKeyCommand is null || commandSuggestionDownKeyCommand is null)
        {
            return;
        }

        InvokeResponderSelector(responder, "removeKeyCommand:", commandSuggestionUpKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", commandSuggestionDownKeyCommand);
        InvokeResponderSelector(responder, "addKeyCommand:", commandSuggestionUpKeyCommand);
        InvokeResponderSelector(responder, "addKeyCommand:", commandSuggestionDownKeyCommand);
    }

    private void RegisterMacEditorCommands(UIResponder? responder, bool includePrimaryAction)
    {
        if (responder is null ||
            modalEscapeKeyCommand is null ||
            modalSaveKeyCommand is null ||
            modalTabNextKeyCommand is null ||
            modalTabPreviousKeyCommand is null ||
            modalPrimaryActionKeyCommand is null)
        {
            return;
        }

        InvokeResponderSelector(responder, "removeKeyCommand:", modalEscapeKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalSaveKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalTabNextKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalTabPreviousKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalPrimaryActionKeyCommand);
        if (modalPrimaryActionAlternateKeyCommand is not null)
        {
            InvokeResponderSelector(responder, "removeKeyCommand:", modalPrimaryActionAlternateKeyCommand);
        }
        InvokeResponderSelector(responder, "addKeyCommand:", modalEscapeKeyCommand);
        InvokeResponderSelector(responder, "addKeyCommand:", modalSaveKeyCommand);
        InvokeResponderSelector(responder, "addKeyCommand:", modalTabNextKeyCommand);
        InvokeResponderSelector(responder, "addKeyCommand:", modalTabPreviousKeyCommand);
        if (includePrimaryAction)
        {
            InvokeResponderSelector(responder, "addKeyCommand:", modalPrimaryActionKeyCommand);
            if (modalPrimaryActionAlternateKeyCommand is not null)
            {
                InvokeResponderSelector(responder, "addKeyCommand:", modalPrimaryActionAlternateKeyCommand);
            }
        }
    }

    private void UnregisterMacEditorCommands(UIResponder? responder)
    {
        if (responder is null ||
            modalEscapeKeyCommand is null ||
            modalSaveKeyCommand is null ||
            modalTabNextKeyCommand is null ||
            modalTabPreviousKeyCommand is null ||
            modalPrimaryActionKeyCommand is null)
        {
            return;
        }

        InvokeResponderSelector(responder, "removeKeyCommand:", modalEscapeKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalSaveKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalTabNextKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalTabPreviousKeyCommand);
        InvokeResponderSelector(responder, "removeKeyCommand:", modalPrimaryActionKeyCommand);
        if (modalPrimaryActionAlternateKeyCommand is not null)
        {
            InvokeResponderSelector(responder, "removeKeyCommand:", modalPrimaryActionAlternateKeyCommand);
        }
    }

    private static void InvokeResponderSelector(UIResponder responder, string selectorName, NSObject argument)
    {
        var selector = new Selector(selectorName);
        if (!responder.RespondsToSelector(selector))
        {
            return;
        }

        responder.PerformSelector(selector, argument, 0);
    }

    private static UIKeyCommand CreateMacEscapeKeyCommand()
    {
        var command = UIKeyCommand.Create(new NSString(macEscapeKeyInput), 0, new Selector("handleEditorCancel:"));
        TrySetKeyCommandPriorityOverSystem(command);
        return command;
    }

    private static UIKeyCommand CreateMacEditorKeyCommand(string keyInput, UIKeyModifierFlags modifiers, string selectorName)
    {
        var command = UIKeyCommand.Create(new NSString(keyInput), modifiers, new Selector(selectorName));
        TrySetKeyCommandPriorityOverSystem(command);
        return command;
    }

    private static UIKeyCommand? TryCreateMacEditorKeyCommand(string keyInput, UIKeyModifierFlags modifiers, string selectorName)
    {
        if (string.IsNullOrEmpty(keyInput))
        {
            return null;
        }

        try
        {
            return CreateMacEditorKeyCommand(keyInput, modifiers, selectorName);
        }
        catch
        {
            return null;
        }
    }

    private static UIKeyCommand CreateMacCommandSuggestionKeyCommand(string keyInput, string selectorName)
    {
        var command = UIKeyCommand.Create(new NSString(keyInput), 0, new Selector(selectorName));
        TrySetKeyCommandPriorityOverSystem(command);
        return command;
    }

    private static void TrySetKeyCommandPriorityOverSystem(UIKeyCommand command)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var prop = typeof(UIKeyCommand).GetProperty("WantsPriorityOverSystemBehavior", flags);
        if (prop?.CanWrite == true)
        {
            prop.SetValue(command, true);
            return;
        }

        var method = typeof(UIKeyCommand).GetMethod("SetWantsPriorityOverSystemBehavior", flags);
        method?.Invoke(command, [true]);
    }

    private static string ResolveMacKeyInput(string inputName, string fallback)
    {
        return TryResolveMacKeyInput(inputName) ?? fallback;
    }

    private static string? TryResolveMacKeyInput(string inputName)
    {
        var keyInputProperty = typeof(UIKeyCommand).GetProperty(inputName, BindingFlags.Public | BindingFlags.Static);
        if (keyInputProperty?.GetValue(null) is NSString nsInput)
        {
            return nsInput.ToString();
        }

        if (keyInputProperty?.GetValue(null) is string inputText && !string.IsNullOrEmpty(inputText))
        {
            return inputText;
        }

        var keyInputField = typeof(UIKeyCommand).GetField(inputName, BindingFlags.Public | BindingFlags.Static);
        if (keyInputField?.GetValue(null) is NSString nsInputField)
        {
            return nsInputField.ToString();
        }

        if (keyInputField?.GetValue(null) is string inputFieldText && !string.IsNullOrEmpty(inputFieldText))
        {
            return inputFieldText;
        }

        return null;
    }

    private void EnsureMacFirstResponder()
    {
        if (!IsMacAppForegroundActive())
        {
            return;
        }

        if (Window?.Handler?.PlatformView is UIWindow nativeWindow && !nativeWindow.IsKeyWindow)
        {
            nativeWindow.MakeKeyAndVisible();
        }
    }

    private static bool IsMacAppForegroundActive()
    {
        var app = UIApplication.SharedApplication;
        if (app.ApplicationState != UIApplicationState.Active)
        {
            return false;
        }

        foreach (var scene in app.ConnectedScenes)
        {
            if (scene is UIScene uiScene && uiScene.ActivationState == UISceneActivationState.ForegroundActive)
            {
                return true;
            }
        }

        return app.ConnectedScenes is null || app.ConnectedScenes.Count == 0;
    }

    private void ApplyMacContentScale()
    {
        if (Handler?.PlatformView is not UIView rootView)
        {
            return;
        }

        var scale = UIScreen.MainScreen.Scale;
        ApplyMacContentScaleRecursive(rootView, scale);
    }

    private static void ApplyMacContentScaleRecursive(UIView view, nfloat scale)
    {
        view.ContentScaleFactor = scale;
        view.Layer.ContentsScale = scale;
        foreach (var subview in view.Subviews)
        {
            ApplyMacContentScaleRecursive(subview, scale);
        }
    }

    private void StartMacMiddleButtonPolling()
    {
        if (macMiddleButtonPollTimer is not null)
        {
            return;
        }

        var dispatcher = Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        macMiddleButtonPollTimer = dispatcher.CreateTimer();
        macMiddleButtonPollTimer.Interval = TimeSpan.FromMilliseconds(16);
        macMiddleButtonPollTimer.IsRepeating = true;
        macMiddleButtonPollTimer.Tick += OnMacMiddleButtonPollTick;
        macMiddleButtonWasDown = IsMacMiddleButtonCurrentlyDown();
        macMiddleButtonPollTimer.Start();
    }

    private void StopMacMiddleButtonPolling()
    {
        if (macMiddleButtonPollTimer is null)
        {
            return;
        }

        macMiddleButtonPollTimer.Tick -= OnMacMiddleButtonPollTick;
        macMiddleButtonPollTimer.Stop();
        macMiddleButtonPollTimer = null;
    }

    private void OnMacMiddleButtonPollTick(object? sender, EventArgs e)
    {
        var isDown = IsMacMiddleButtonCurrentlyDown();
        if (isDown && !macMiddleButtonWasDown)
        {
            if (lastPointerOnRoot is Point pointer)
            {
                HandleMacMiddleClick(pointer);
            }
        }

        macMiddleButtonWasDown = isDown;
    }

    private static bool IsMacMiddleButtonCurrentlyDown()
    {
        try
        {
            return CGEventSource.GetButtonState(CGEventSourceStateID.HidSystem, CGMouseButton.Center);
        }
        catch
        {
            try
            {
                return CGEventSource.GetButtonState(CGEventSourceStateID.CombinedSession, CGMouseButton.Center);
            }
            catch
            {
                return false;
            }
        }
    }

    private void EnsureMacGuidEntryReadOnlyBehavior()
    {
        if (ModalGuidEntry.Handler?.PlatformView is not UITextField textField)
        {
            return;
        }

        if (!ReferenceEquals(macGuidNativeTextField, textField))
        {
            DetachMacGuidEntryReadOnlyBehavior();
            macGuidNativeTextField = textField;
        }

        textField.Delegate = macGuidReadOnlyDelegate;
        textField.EditingChanged -= OnMacGuidEntryEditingChanged;
        textField.EditingChanged += OnMacGuidEntryEditingChanged;
        textField.UserInteractionEnabled = true;
        textField.Enabled = true;

        if (string.IsNullOrEmpty(macGuidLockedText))
        {
            macGuidLockedText = ModalGuidEntry.Text ?? string.Empty;
        }

        if (!string.Equals(textField.Text, macGuidLockedText, StringComparison.Ordinal))
        {
            textField.Text = macGuidLockedText;
        }
    }

    private void DetachMacGuidEntryReadOnlyBehavior()
    {
        if (macGuidNativeTextField is null)
        {
            return;
        }

        macGuidNativeTextField.EditingChanged -= OnMacGuidEntryEditingChanged;
        macGuidNativeTextField = null;
    }

    private void OnMacGuidEntryEditingChanged(object? sender, EventArgs e)
    {
        if (macApplyingGuidTextLock || sender is not UITextField textField)
        {
            return;
        }

        if (string.IsNullOrEmpty(macGuidLockedText))
        {
            macGuidLockedText = viewModel.Editor.GuidText ?? string.Empty;
        }

        if (string.Equals(textField.Text, macGuidLockedText, StringComparison.Ordinal))
        {
            return;
        }

        macApplyingGuidTextLock = true;
        try
        {
            textField.Text = macGuidLockedText;
            if (!string.Equals(ModalGuidEntry.Text, macGuidLockedText, StringComparison.Ordinal))
            {
                ModalGuidEntry.Text = macGuidLockedText;
            }

            if (!string.Equals(viewModel.Editor.GuidText, macGuidLockedText, StringComparison.Ordinal))
            {
                viewModel.Editor.GuidText = macGuidLockedText;
            }

            var allTextRange = textField.GetTextRange(textField.BeginningOfDocument, textField.EndOfDocument);
            if (allTextRange is not null)
            {
                textField.SelectedTextRange = allTextRange;
            }
        }
        finally
        {
            macApplyingGuidTextLock = false;
        }
    }

    private void ResignModalInputFirstResponder()
    {
        ModalGuidEntry.Unfocus();
        ModalCommandEntry.Unfocus();
        ModalButtonTextEntry.Unfocus();
        ModalToolEntry.Unfocus();
        ModalArgumentsEntry.Unfocus();
        ModalClipWordEditor.Unfocus();
        ModalNoteEditor.Unfocus();

        if (ModalGuidEntry.Handler?.PlatformView is UITextField guidField && guidField.IsFirstResponder)
        {
            guidField.ResignFirstResponder();
        }

        if (ModalCommandEntry.Handler?.PlatformView is UITextField commandField && commandField.IsFirstResponder)
        {
            commandField.ResignFirstResponder();
        }

        if (ModalButtonTextEntry.Handler?.PlatformView is UITextField buttonTextField && buttonTextField.IsFirstResponder)
        {
            buttonTextField.ResignFirstResponder();
        }

        if (ModalToolEntry.Handler?.PlatformView is UITextField toolField && toolField.IsFirstResponder)
        {
            toolField.ResignFirstResponder();
        }

        if (ModalArgumentsEntry.Handler?.PlatformView is UITextField argumentsField && argumentsField.IsFirstResponder)
        {
            argumentsField.ResignFirstResponder();
        }

        if (ModalClipWordEditor.Handler?.PlatformView is UITextView clipWordField && clipWordField.IsFirstResponder)
        {
            clipWordField.ResignFirstResponder();
        }

        if (ModalNoteEditor.Handler?.PlatformView is UITextView noteField && noteField.IsFirstResponder)
        {
            noteField.ResignFirstResponder();
        }

        ModalClipWordFocusUnderline.IsVisible = false;
        ModalNoteFocusUnderline.IsVisible = false;
    }

    private void ResignMainInputFirstResponder()
    {
        MainCommandEntry.Unfocus();
        MainSearchEntry.Unfocus();

        if (MainCommandEntry.Handler?.PlatformView is UITextField commandField && commandField.IsFirstResponder)
        {
            commandField.ResignFirstResponder();
        }

        if (MainSearchEntry.Handler?.PlatformView is UITextField searchField && searchField.IsFirstResponder)
        {
            searchField.ResignFirstResponder();
        }
    }

    private sealed class MacGuidReadOnlyTextFieldDelegate : UITextFieldDelegate
    {
        public override bool ShouldBeginEditing(UITextField textField)
            => true;

        public override bool ShouldClear(UITextField textField)
            => false;

        public override bool ShouldChangeCharacters(UITextField textField, NSRange range, string replacementString)
            => false;
    }
#endif

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
