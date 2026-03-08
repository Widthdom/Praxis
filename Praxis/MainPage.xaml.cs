using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

using Praxis.Behaviors;
using Praxis.Core.Logic;
using Praxis.Core.Models;
using Praxis.ViewModels;
#if MACCATALYST
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;
#endif

namespace Praxis;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel viewModel;
    private ButtonEditorViewModel? observedEditorViewModel;
    private const double ModalSingleLineRowHeight = 40;
    private const double ModalRowSpacing = 8;
    private const int ModalTotalRows = 7;
    private const int ModalStaticRows = 5;
    private const double ModalScrollMaxHeightFallback = 460;
    private const double ModalScrollVerticalReserve = 260;
    private bool xamlLoaded;
    private bool initialized;
    private CancellationTokenSource? copyNoticeCts;
    private CancellationTokenSource? statusFlashCts;
    private CancellationTokenSource? quickLookShowCts;
    private CancellationTokenSource? quickLookHideCts;
    private CancellationTokenSource? dockHoverExitCts;
    private Window? attachedWindow;
    private bool suppressNextRootSuggestionClose;
    private TaskCompletionSource<EditorConflictResolution>? editorConflictTcs;
    private Guid? quickLookPendingItemId;
    private VisualElement? quickLookPendingAnchor;
    private const int QuickLookShowDelayMs = 1000;
    private const int QuickLookHideDelayMs = 120;
    private const uint QuickLookFadeDurationMs = 150;
    private const double QuickLookOffsetX = 14;
    private const double QuickLookOffsetY = 2;
    private const double QuickLookViewportMargin = 10;
    private const int DockHoverExitHideDelayMs = 60;
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
    private bool windowsEditorFocusRestorePending;
    private bool windowsConflictFocusRestorePending;
    private static readonly TimeSpan windowsFocusRestorePrimaryDelay = UiTimingPolicy.WindowsFocusRestorePrimaryDelay;
    private static readonly TimeSpan windowsFocusRestoreSecondaryDelay = UiTimingPolicy.WindowsFocusRestoreSecondaryDelay;
#endif
    private enum ConflictDialogFocusTarget
    {
        Reload,
        Overwrite,
        Cancel,
    }

    private ConflictDialogFocusTarget? conflictDialogPseudoFocusedTarget;
#if MACCATALYST
    private static WeakReference<MainPage>? macLastActivePage;
    private static readonly IntPtr nsCursorClass = ObjcGetClass("NSCursor");
    private static readonly IntPtr pointingHandCursorSelector = SelRegisterName("pointingHandCursor");
    private static readonly IntPtr arrowCursorSelector = SelRegisterName("arrowCursor");
    private static readonly IntPtr setCursorSelector = SelRegisterName("set");
    private static readonly TimeSpan macActivationFocusWindow = UiTimingPolicy.MacActivationFocusWindow;
    private static readonly TimeSpan macActivationFocusRequestCoalesceDelay = UiTimingPolicy.MacActivationFocusRequestCoalesceDelay;
    private static readonly TimeSpan macSearchFocusUserIntentWindow = UiTimingPolicy.MacSearchFocusUserIntentWindow;
    private long macActivationFocusRequestId;
    private DateTimeOffset macActivationFocusSessionUntilUtc;
    private DateTimeOffset macSearchFocusUserIntentUntilUtc;
    private NSObject? macDidBecomeActiveObserver;
    private NSObject? macWillEnterForegroundObserver;
    private NSObject? macSceneDidActivateObserver;
    private NSObject? macSceneWillEnterForegroundObserver;
    private NSObject? macWindowDidBecomeKeyObserver;
    private NSObject? macWillResignActiveObserver;
    private NSObject? macDidEnterBackgroundObserver;
    private NSObject? macWindowDidResignKeyObserver;

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

        SetDockScrollBarVisibility(isPointerOverDockRegion: false);
        ApplyClearButtonGlyphAlignmentTuning();
        this.viewModel.ResolveEditorConflictAsync = ResolveEditorConflictAsync;
        App.SetEditorOpenState(this.viewModel.IsEditorOpen);
        AttachEditorPropertyChanged(this.viewModel.Editor);
        this.viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        this.viewModel.CommandSuggestions.CollectionChanged += CommandSuggestionsOnCollectionChanged;
        App.ThemeShortcutRequested += OnThemeShortcutRequested;
        App.EditorShortcutRequested += OnEditorShortcutRequested;
        App.CommandInputShortcutRequested += OnCommandInputShortcutRequested;
        App.HistoryShortcutRequested += OnHistoryShortcutRequested;
        App.MiddleMouseClickRequested += OnMiddleMouseClickRequested;
#if MACCATALYST
        App.MacApplicationDeactivating += OnMacApplicationDeactivating;
        App.MacApplicationActivated += OnMacApplicationActivated;
#endif
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
            UpdateModalEditorHeights();
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
                ApplyModalEditorThemeTextColors();
                RebuildCommandSuggestionStack();
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
#if MACCATALYST
        macLastActivePage = new WeakReference<MainPage>(this);
#endif

        if (!xamlLoaded)
        {
            return;
        }

        AttachWindowActivationHook();
        AttachEditorPropertyChanged(viewModel.Editor);

        if (initialized)
        {
#if MACCATALYST
            ScheduleMainCommandFocusAfterActivation("MainPage.OnAppearing");
#else
            Dispatcher.Dispatch(RequestMainCommandFocusAfterActivation);
#endif
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
        DetachEditorPropertyChanged();
        App.SetEditorOpenState(false);
        App.SetContextMenuOpenState(false);
        App.SetConflictDialogOpenState(false);
        UpdateConflictDialogModalState(isOpen: false);
        HideQuickLookPopup();
#if MACCATALYST
        DetachMacGuidEntryReadOnlyBehavior();
        macGuidLockedText = string.Empty;
        StopMacMiddleButtonPolling();
#endif
    }

#if MACCATALYST
    public static void RequestMacCommandFocusFromNativeActivation(string source)
    {
        if (macLastActivePage is null || !macLastActivePage.TryGetTarget(out var page))
        {
            return;
        }

        var dispatcher = page.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        dispatcher.Dispatch(() =>
        {
            page.AttachWindowActivationHook();
            page.ScheduleMainCommandFocusAfterActivation($"native:{source}");
        });
    }

    public static void MarkMacSearchFocusUserIntentFromKeyboard(string source)
    {
        if (macLastActivePage is null || !macLastActivePage.TryGetTarget(out var page))
        {
            return;
        }

        var dispatcher = page.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        dispatcher.Dispatch(() => page.MarkMacSearchFocusUserIntent(source));
    }

    public static void FocusMacSearchEntryFromCommandTab(string source)
    {
        if (macLastActivePage is null || !macLastActivePage.TryGetTarget(out var page))
        {
            return;
        }

        var dispatcher = page.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        dispatcher.Dispatch(() =>
        {
            page.MarkMacSearchFocusUserIntent(source);
            page.FocusMainSearchEntryFromCommandTabCore();
        });
    }

    public static bool ShouldAllowMacSearchEntryFocus()
    {
        if (macLastActivePage is null || !macLastActivePage.TryGetTarget(out var page))
        {
            return true;
        }

        return page.ShouldAllowMacSearchEntryFocusCore();
    }
#endif

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

    private void AttachWindowActivationHook()
    {
        var currentWindow = Window;
        if (currentWindow is null || ReferenceEquals(attachedWindow, currentWindow))
        {
            return;
        }

        DetachWindowActivationHook();
        attachedWindow = currentWindow;
        attachedWindow.Activated += OnWindowActivated;
        attachedWindow.Resumed += OnWindowResumed;
#if MACCATALYST
        AttachMacActivationObservers();
#endif
    }

    private void DetachWindowActivationHook()
    {
        if (attachedWindow is null)
        {
            return;
        }

        attachedWindow.Activated -= OnWindowActivated;
        attachedWindow.Resumed -= OnWindowResumed;
        attachedWindow = null;
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        ScheduleMainCommandFocusAfterActivation("Window.Activated");
    }

    private void OnWindowResumed(object? sender, EventArgs e)
    {
        ScheduleMainCommandFocusAfterActivation("Window.Resumed");
    }

#if MACCATALYST
    private void ScheduleMainCommandFocusAfterActivation(string _)
    {
        var requestId = ++macActivationFocusRequestId;
        Dispatcher.DispatchDelayed(macActivationFocusRequestCoalesceDelay, () =>
        {
            if (requestId != macActivationFocusRequestId)
            {
                return;
            }

            RequestMainCommandFocusAfterActivation();
        });
    }
#else
    private void ScheduleMainCommandFocusAfterActivation(string _)
    {
        Dispatcher.Dispatch(RequestMainCommandFocusAfterActivation);
    }
#endif

    private void RequestMainCommandFocusAfterActivation()
    {
#if MACCATALYST
        BeginMacActivationFocusSession();
        Dispatcher.DispatchDelayed(UiTimingPolicy.MacActivationFocusRetryFirstDelay, ApplyMacActivationCommandFocus);
        Dispatcher.DispatchDelayed(UiTimingPolicy.MacActivationFocusRetrySecondDelay, ApplyMacActivationCommandFocus);
        Dispatcher.DispatchDelayed(UiTimingPolicy.MacActivationFocusRetryThirdDelay, ApplyMacActivationCommandFocus);
#else
        FocusMainCommandAfterWindowActivation();
#endif
    }

#if MACCATALYST
    private void ApplyMacActivationCommandFocus()
    {
        if (!ShouldFocusMainCommandAfterWindowActivation())
        {
            return;
        }

        FocusMainCommandAfterWindowActivation();
    }

    private void BeginMacActivationFocusSession()
    {
        macActivationFocusSessionUntilUtc = DateTimeOffset.UtcNow + macActivationFocusWindow;
    }
#endif

    private void FocusMainCommandAfterWindowActivation()
    {
        if (!ShouldFocusMainCommandAfterWindowActivation())
        {
            return;
        }

#if WINDOWS
        EnsureWindowsTextBoxHooks();
        windowsSelectAllOnTabNavigationPending = true;
#endif

#if !MACCATALYST
        MainCommandEntry.Focus();
#endif

#if WINDOWS
        if (commandTextBox is not null)
        {
            commandTextBox.SelectAll();
            windowsSelectAllOnTabNavigationPending = false;
        }
#endif

#if MACCATALYST
        ForceMacCommandFirstResponder();
        var commandFocused = IsMainCommandEntryActive();
        var searchFocused = IsMainSearchEntryActive();
        var selectAllSatisfied = IsMainCommandSelectAllSatisfied(out _, out _);
        if (!commandFocused || searchFocused || !selectAllSatisfied)
        {
            Dispatcher.DispatchDelayed(UiTimingPolicy.MacActivationFocusReassertDelay, () =>
            {
                if (!ShouldFocusMainCommandAfterWindowActivation() ||
                    !IsMacAppForegroundActive())
                {
                    return;
                }

                ForceMacCommandFirstResponder();
            });
        }
#endif
    }

    private bool ShouldFocusMainCommandAfterWindowActivation()
    {
        if (!xamlLoaded || !initialized)
        {
            return false;
        }

        return WindowActivationCommandFocusPolicy.ShouldFocusMainCommand(
            isEditorOpen: viewModel.IsEditorOpen,
            isConflictDialogOpen: IsConflictDialogOpen());
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

            await Task.Delay(UiTimingPolicy.CopyNoticeHoldDurationMs, token);
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
        ApplyModalEditorThemeTextColors();
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
        ApplyModalEditorThemeTextColors();
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

    private void ModalActionButton_Unfocused(object? sender, FocusEventArgs e)
    {
#if WINDOWS
        QueueWindowsEditorFocusRestore();
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

    private void ApplyModalEditorThemeTextColors()
    {
        var dark = IsDarkThemeActive();
        var textColor = Color.FromArgb(ThemeTextColorPolicy.ResolveTextColorHex(dark));
        ModalClipWordEditor.TextColor = textColor;
        ModalNoteEditor.TextColor = textColor;
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
        UpdateModalEditorHeights(noteTextOverride: e.NewTextValue);
    }

    private void ModalClipWordEditor_TextChanged(object? sender, TextChangedEventArgs e)
    {
#if MACCATALYST
        TryHandleMacEditorTabTextInsertion(sender, e);
#endif
        UpdateModalEditorHeights(clipTextOverride: e.NewTextValue);
    }

    private void UpdateModalEditorHeights(string? clipTextOverride = null, string? noteTextOverride = null)
    {
        if (!xamlLoaded || EditorFieldsScrollView is null)
        {
            return;
        }

        var clipText = clipTextOverride ?? ModalClipWordEditor.Text ?? viewModel.Editor.ClipText;
        var noteText = noteTextOverride ?? ModalNoteEditor.Text ?? viewModel.Editor.Note;
        var clipHeight = ModalEditorHeightResolver.ResolveHeight(clipText);
        var noteHeight = ModalEditorHeightResolver.ResolveHeight(noteText);

        UpdateEditorHeight(ModalClipWordEditor, ModalClipWordContainer, CopyClipWordButton, clipHeight);
        UpdateEditorHeight(ModalNoteEditor, ModalNoteContainer, CopyNoteButton, noteHeight);

        var contentHeight = ResolveModalEditorScrollContentHeight(clipHeight, noteHeight);
        var maxHeight = ResolveModalEditorScrollMaxHeight();
        EditorFieldsScrollView.HeightRequest = ModalEditorScrollHeightResolver.Resolve(contentHeight, maxHeight);
        EditorFieldsScrollView.InvalidateMeasure();

        ModalClipWordContainer.InvalidateMeasure();
        ModalNoteContainer.InvalidateMeasure();
        EditorOverlay.InvalidateMeasure();
    }

    private static void UpdateEditorHeight(Editor editor, Border container, Button? copyButton, double targetHeight)
    {
        editor.HeightRequest = targetHeight;
        container.HeightRequest = targetHeight;
        if (copyButton is not null)
        {
            copyButton.HeightRequest = targetHeight;
        }
    }

    private static double ResolveModalEditorScrollContentHeight(double clipHeight, double noteHeight)
    {
        var dynamicHeight = clipHeight + noteHeight;
        var staticRowsHeight = ModalStaticRows * ModalSingleLineRowHeight;
        var totalSpacing = (ModalTotalRows - 1) * ModalRowSpacing;
        return staticRowsHeight + dynamicHeight + totalSpacing;
    }

    private double ResolveModalEditorScrollMaxHeight()
    {
        var hostHeight = RootGrid.Height > 0 ? RootGrid.Height : Height;
        if (hostHeight <= 0)
        {
            return ModalScrollMaxHeightFallback;
        }

        return Math.Max(180, hostHeight - ModalScrollVerticalReserve);
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
        if (e.PropertyName == nameof(MainViewModel.Editor))
        {
            AttachEditorPropertyChanged(viewModel.Editor);
            UpdateModalEditorHeights();
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.SelectedTheme))
        {
            ApplyNeutralStatusBackground();
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.StatusRevision))
        {
            TriggerStatusFlash(viewModel.StatusText);
#if MACCATALYST
            RefocusMainCommandAfterCommandNotFound(viewModel.StatusText);
#endif
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
                HideQuickLookPopup();
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
                Dispatcher.DispatchDelayed(UiTimingPolicy.ContextMenuFocusInitialDelay, () =>
                {
                    if (!viewModel.IsContextMenuOpen || viewModel.IsEditorOpen || IsConflictDialogOpen())
                    {
                        return;
                    }

                    FocusContextActionButton(ContextEditButton);
#if MACCATALYST
                    EnsureMacFirstResponder();
#endif
                    ApplyContextActionButtonFocusVisuals();
                });
                Dispatcher.DispatchDelayed(UiTimingPolicy.ContextMenuFocusRetryDelay, () =>
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
        HideQuickLookPopup();
        ApplyTabPolicy();
        UpdateModalEditorHeights();
#if MACCATALYST
        macGuidLockedText = viewModel.Editor.GuidText ?? string.Empty;
#endif
        Dispatcher.DispatchDelayed(UiTimingPolicy.EditorOpenFocusDelay, () =>
        {
            if (!viewModel.IsEditorOpen || IsConflictDialogOpen())
            {
                return;
            }

            FocusModalCommandEntryForOpen();
#if MACCATALYST
            EnsureMacFirstResponder();
            ApplyMacEditorKeyCommands();
            Dispatcher.DispatchDelayed(UiTimingPolicy.MacEditorKeyCommandsRetryDelay, ApplyMacEditorKeyCommands);
#endif
        });
    }

    private void AttachEditorPropertyChanged(ButtonEditorViewModel? editorViewModel)
    {
        if (ReferenceEquals(observedEditorViewModel, editorViewModel))
        {
            return;
        }

        if (observedEditorViewModel is not null)
        {
            observedEditorViewModel.PropertyChanged -= EditorViewModelOnPropertyChanged;
        }

        observedEditorViewModel = editorViewModel;
        if (observedEditorViewModel is not null)
        {
            observedEditorViewModel.PropertyChanged += EditorViewModelOnPropertyChanged;
        }
    }

    private void DetachEditorPropertyChanged()
    {
        if (observedEditorViewModel is null)
        {
            return;
        }

        observedEditorViewModel.PropertyChanged -= EditorViewModelOnPropertyChanged;
        observedEditorViewModel = null;
    }

    private void EditorViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(ButtonEditorViewModel.ClipText) or nameof(ButtonEditorViewModel.Note) or ""))
        {
            return;
        }

        Dispatcher.Dispatch(() => UpdateModalEditorHeights());
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
        var flash = StatusFlashErrorPolicy.IsErrorStatus(message)
            ? Color.FromArgb("#D94A4A")
            : Color.FromArgb("#4AAE6A");

        try
        {
            await AnimateStatusBackgroundAsync(neutral, flash, UiTimingPolicy.StatusFlashInDurationMs, Easing.CubicOut, token);
            token.ThrowIfCancellationRequested();
            await AnimateStatusBackgroundAsync(flash, neutral, UiTimingPolicy.StatusFlashOutDurationMs, Easing.CubicIn, token);
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

#if MACCATALYST
    private void RefocusMainCommandAfterCommandNotFound(string? statusMessage)
    {
        if (!CommandNotFoundRefocusPolicy.ShouldRefocusMainCommand(statusMessage))
        {
            return;
        }

        Dispatcher.DispatchDelayed(UiTimingPolicy.CommandNotFoundRefocusDelay, () =>
        {
            if (!xamlLoaded || viewModel.IsEditorOpen || viewModel.IsContextMenuOpen || IsConflictDialogOpen())
            {
                return;
            }

            ApplyEntryFocusAfterClearButtonTap(MainCommandEntry);
            EnsureMacFirstResponder();
        });
    }
#endif

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
            ThemeMode.Light => AppTheme.Light,
            ThemeMode.Dark => AppTheme.Dark,
            _ => Application.Current?.RequestedTheme ?? AppTheme.Unspecified,
        };

        return theme == AppTheme.Dark
            ? Color.FromArgb("#1E1E1E")
            : Color.FromArgb("#F2F2F2");
    }

    private bool IsDarkThemeActive()
    {
        var selectedTheme = Application.Current?.UserAppTheme switch
        {
            AppTheme.Dark => ThemeMode.Dark,
            AppTheme.Light => ThemeMode.Light,
            _ => ThemeMode.System,
        };

        var requestedThemeDark = Application.Current?.RequestedTheme == AppTheme.Dark;
#if MACCATALYST
        bool? traitDark = (Window?.Handler?.PlatformView as UIWindow)?.TraitCollection?.UserInterfaceStyle switch
        {
            UIUserInterfaceStyle.Dark => true,
            UIUserInterfaceStyle.Light => false,
            _ => null,
        };
#else
        bool? traitDark = null;
#endif
        return ThemeDarkStateResolver.Resolve(selectedTheme, requestedThemeDark, traitDark);
    }

    private void ApplyNeutralStatusBackground()
    {
        ResetStatusBarBackgroundToThemeBinding();
    }

    private void ResetStatusBarBackgroundToThemeBinding()
    {
        StatusBarBorder.ClearValue(VisualElement.BackgroundColorProperty);
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

    private void MainSearchEntry_PointerPressed(object? sender, PointerEventArgs e)
    {
#if MACCATALYST
        MarkMacSearchFocusUserIntent("MainSearchEntry.PointerPressed");
#endif
    }

    private void MainSearchEntry_Tapped(object? sender, TappedEventArgs e)
    {
#if MACCATALYST
        MarkMacSearchFocusUserIntent("MainSearchEntry.Tapped");
#endif
    }

    private void MainSearchEntry_HandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        EnsureWindowsKeyHooks();
        EnsureWindowsTextBoxHooks();
#endif
    }

    private void DockRegion_PointerEntered(object? sender, PointerEventArgs e)
    {
        CancelDockHoverExitHide();
        SetDockScrollBarVisibility(isPointerOverDockRegion: true);
    }

    private void DockRegion_PointerExited(object? sender, PointerEventArgs e)
    {
        ScheduleDockHoverExitHide();
    }

    private void DockScroll_HandlerChanged(object? sender, EventArgs e)
    {
        ApplyNativeDockScrollBarVisibility(isDockPointerHovering);
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(16), () =>
        {
            ApplyNativeDockScrollBarVisibility(isDockPointerHovering);
        });
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(80), () =>
        {
            ApplyNativeDockScrollBarVisibility(isDockPointerHovering);
        });
    }

    private void SetDockScrollBarVisibility(bool isPointerOverDockRegion)
    {
        isDockPointerHovering = isPointerOverDockRegion;
        var showHorizontalScrollBar = DockScrollBarVisibilityPolicy.ShouldShowHorizontalScrollBar(isPointerOverDockRegion: isPointerOverDockRegion);
        DockScrollBarMask.IsVisible = DockScrollBarVisibilityPolicy.ShouldShowScrollBarMask(showHorizontalScrollBar);
        ApplyNativeDockScrollBarVisibility(showHorizontalScrollBar);
    }

    private void ScheduleDockHoverExitHide()
    {
        CancelDockHoverExitHide();
        var cts = new CancellationTokenSource();
        dockHoverExitCts = cts;
        _ = HideDockScrollBarAfterExitDelayAsync(cts.Token);
    }

    private void CancelDockHoverExitHide()
    {
        dockHoverExitCts?.Cancel();
        dockHoverExitCts?.Dispose();
        dockHoverExitCts = null;
    }

    private async Task HideDockScrollBarAfterExitDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(DockHoverExitHideDelayMs, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        SetDockScrollBarVisibility(isPointerOverDockRegion: false);
    }

    private void ApplyNativeDockScrollBarVisibility(bool showHorizontalScrollBar)
    {
#if WINDOWS
        if (DockScroll.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer scrollViewer)
        {
            scrollViewer.HorizontalScrollBarVisibility = showHorizontalScrollBar
                ? Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Visible
                : Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Hidden;
        }
#endif
#if MACCATALYST
        if (TryResolveMacDockScrollView() is UIScrollView scrollView)
        {
            scrollView.ShowsHorizontalScrollIndicator = showHorizontalScrollBar;
            scrollView.SetNeedsLayout();
            scrollView.LayoutIfNeeded();
        }
#endif
    }

#if MACCATALYST
    private UIScrollView? TryResolveMacDockScrollView()
    {
        if (DockScroll.Handler?.PlatformView is not UIView platformView)
        {
            return null;
        }

        return platformView as UIScrollView ?? FindFirstScrollView(platformView);
    }

    private static UIScrollView? FindFirstScrollView(UIView root)
    {
        foreach (var subview in root.Subviews)
        {
            if (subview is UIScrollView scrollView)
            {
                return scrollView;
            }

            var nested = FindFirstScrollView(subview);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
#endif

    private void ButtonQuickLook_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not VisualElement anchor || anchor.BindingContext is not LauncherButtonItemViewModel item)
        {
            return;
        }

        QueueQuickLookPopup(item, anchor);
    }

    private void ButtonQuickLook_PointerExited(object? sender, PointerEventArgs e)
    {
        ScheduleQuickLookHide();
    }

    private void QueueQuickLookPopup(LauncherButtonItemViewModel item, VisualElement anchor)
    {
        if (!xamlLoaded || viewModel.IsEditorOpen || viewModel.IsContextMenuOpen || IsConflictDialogOpen())
        {
            return;
        }

        CancelQuickLookHide();
        CancelQuickLookShow();
        quickLookPendingItemId = item.Id;
        quickLookPendingAnchor = anchor;

        var cts = new CancellationTokenSource();
        quickLookShowCts = cts;
        _ = ShowQuickLookAfterDelayAsync(item, anchor, cts.Token);
    }

    private async Task ShowQuickLookAfterDelayAsync(LauncherButtonItemViewModel item, VisualElement anchor, CancellationToken token)
    {
        try
        {
            await Task.Delay(QuickLookShowDelayMs, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested ||
            quickLookPendingItemId != item.Id ||
            !ReferenceEquals(quickLookPendingAnchor, anchor))
        {
            return;
        }

        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen || IsConflictDialogOpen())
        {
            return;
        }

        QuickLookCommandLabel.Text = QuickLookPreviewFormatter.BuildLine("Command", item.Command);
        QuickLookToolLabel.Text = QuickLookPreviewFormatter.BuildLine("Tool", item.Tool);
        QuickLookArgumentsLabel.Text = QuickLookPreviewFormatter.BuildLine("Arguments", item.Arguments);
        QuickLookClipWordLabel.Text = QuickLookPreviewFormatter.BuildLine("Clip Word", item.ClipText);
        QuickLookNoteLabel.Text = QuickLookPreviewFormatter.BuildLine("Note", item.Note);
        PositionQuickLookPopup(anchor);

        QuickLookPopup.CancelAnimations();
        if (!QuickLookPopup.IsVisible)
        {
            QuickLookPopup.Opacity = 0;
            QuickLookPopup.IsVisible = true;
        }

        if (QuickLookPopup.Opacity < 1)
        {
            await QuickLookPopup.FadeToAsync(1, QuickLookFadeDurationMs, Easing.CubicOut);
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

    }

    private void ScheduleQuickLookHide()
    {
        CancelQuickLookShow();
        CancelQuickLookHide();
        var cts = new CancellationTokenSource();
        quickLookHideCts = cts;
        _ = HideQuickLookAfterDelayAsync(cts.Token);
    }

    private async Task HideQuickLookAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(QuickLookHideDelayMs, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested || !xamlLoaded || !QuickLookPopup.IsVisible)
        {
            return;
        }

        QuickLookPopup.CancelAnimations();
        if (QuickLookPopup.Opacity > 0)
        {
            await QuickLookPopup.FadeToAsync(0, QuickLookFadeDurationMs, Easing.CubicIn);
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        QuickLookPopup.IsVisible = false;
        QuickLookPopup.Opacity = 0;
    }

    private void PositionQuickLookPopup(VisualElement anchor)
    {
        if (RootGrid.Width <= 0 || RootGrid.Height <= 0 || anchor.Width <= 0 || anchor.Height <= 0)
        {
            return;
        }

        var anchorPos = GetPositionRelativeToAncestor(anchor, RootGrid);
        var popupWidth = QuickLookPopup.WidthRequest > 0 ? QuickLookPopup.WidthRequest : Math.Min(420, RootGrid.Width * 0.5);
        var popupHeight = Math.Max(140, QuickLookPopup.Height);

        var targetX = anchorPos.X + anchor.Width + QuickLookOffsetX;
        var targetY = anchorPos.Y + QuickLookOffsetY;

        var maxX = Math.Max(QuickLookViewportMargin, RootGrid.Width - popupWidth - QuickLookViewportMargin);
        var maxY = Math.Max(QuickLookViewportMargin, RootGrid.Height - popupHeight - QuickLookViewportMargin);

        if (targetX > maxX)
        {
            targetX = anchorPos.X - popupWidth - QuickLookOffsetX;
        }

        QuickLookPopup.TranslationX = Math.Clamp(targetX, QuickLookViewportMargin, maxX);
        QuickLookPopup.TranslationY = Math.Clamp(targetY, QuickLookViewportMargin, maxY);
    }

    private void HideQuickLookPopup()
    {
        CancelQuickLookShow();
        CancelQuickLookHide();
        quickLookPendingItemId = null;
        quickLookPendingAnchor = null;
        if (!xamlLoaded)
        {
            return;
        }

        QuickLookPopup.CancelAnimations();
        QuickLookPopup.Opacity = 0;
        QuickLookPopup.IsVisible = false;
    }

    private void CancelQuickLookShow()
    {
        quickLookShowCts?.Cancel();
        quickLookShowCts?.Dispose();
        quickLookShowCts = null;
    }

    private void CancelQuickLookHide()
    {
        quickLookHideCts?.Cancel();
        quickLookHideCts?.Dispose();
        quickLookHideCts = null;
    }

    private void ApplyClearButtonGlyphAlignmentTuning()
    {
        var translation = ClearButtonGlyphAlignmentPolicy.ResolveTranslation(OperatingSystem.IsWindows());
        CommandClearGlyph.TranslationX = translation;
        CommandClearGlyph.TranslationY = translation;
        SearchClearGlyph.TranslationX = translation;
        SearchClearGlyph.TranslationY = translation;
    }

    private void CommandClearButton_Tapped(object? sender, TappedEventArgs e)
    {
        if (viewModel.ClearCommandInputCommand.CanExecute(null))
        {
            viewModel.ClearCommandInputCommand.Execute(null);
        }

        FocusEntryAfterClearButtonTap(MainCommandEntry);
    }

    private void SearchClearButton_Tapped(object? sender, TappedEventArgs e)
    {
#if MACCATALYST
        MarkMacSearchFocusUserIntent("SearchClearButton.Tapped");
#endif
        if (viewModel.ClearSearchTextCommand.CanExecute(null))
        {
            viewModel.ClearSearchTextCommand.Execute(null);
        }

        FocusEntryAfterClearButtonTap(MainSearchEntry);
    }

    private void FocusEntryAfterClearButtonTap(Entry entry)
    {
        var retryDelays = ClearButtonRefocusPolicy.ResolveRetryDelays(OperatingSystem.IsWindows());
#if WINDOWS
        EnsureWindowsTextBoxHooks();
        windowsSelectAllOnTabNavigationPending = false;
#endif
        foreach (var delay in retryDelays)
        {
            if (delay <= TimeSpan.Zero)
            {
                Dispatcher.Dispatch(() => ApplyEntryFocusAfterClearButtonTap(entry));
                continue;
            }

            Dispatcher.DispatchDelayed(delay, () => ApplyEntryFocusAfterClearButtonTap(entry));
        }
    }

    private void ApplyEntryFocusAfterClearButtonTap(Entry entry)
    {
        entry.Focus();
#if MACCATALYST
        PlaceMacEntryCaretAtEnd(entry);
#endif
#if WINDOWS
        windowsSelectAllOnTabNavigationPending = false;
        var textBox = ResolveWindowsTextBoxForEntry(entry);
        if (textBox is not null)
        {
            textBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
            PlaceWindowsTextBoxCaretAtEnd(textBox);
        }
#endif
    }

#if WINDOWS
    private Microsoft.UI.Xaml.Controls.TextBox? ResolveWindowsTextBoxForEntry(Entry entry)
    {
        if (ReferenceEquals(entry, MainCommandEntry))
        {
            return commandTextBox ?? entry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox;
        }

        if (ReferenceEquals(entry, MainSearchEntry))
        {
            return searchTextBox ?? entry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox;
        }

        return entry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox;
    }

    private static void PlaceWindowsTextBoxCaretAtEnd(Microsoft.UI.Xaml.Controls.TextBox textBox)
    {
        var caretPosition = TextCaretPositionResolver.ResolveTailOffset(textBox.Text);
        textBox.Select(caretPosition, 0);
    }
#endif

    private void ClearButton_PointerEntered(object? sender, PointerEventArgs e)
    {
        SetClearButtonHandCursor(sender, useHandCursor: true);
    }

    private void ClearButton_PointerExited(object? sender, PointerEventArgs e)
    {
        SetClearButtonHandCursor(sender, useHandCursor: false);
    }

    private static void SetClearButtonHandCursor(object? sender, bool useHandCursor)
    {
#if WINDOWS
        if (sender is VisualElement element &&
            element.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement frameworkElement)
        {
            var cursor = useHandCursor
                ? Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand)
                : null;
            NonPublicPropertySetter.TrySet(frameworkElement, "ProtectedCursor", cursor);
        }
#endif

#if MACCATALYST
        if (nsCursorClass == IntPtr.Zero)
        {
            return;
        }

        var cursorSelector = useHandCursor ? pointingHandCursorSelector : arrowCursorSelector;
        var cursor = ObjcMsgSendIntPtr(nsCursorClass, cursorSelector);
        if (cursor != IntPtr.Zero)
        {
            ObjcMsgSendVoid(cursor, setCursorSelector);
        }
#endif
    }

#if MACCATALYST
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr ObjcGetClass(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr SelRegisterName(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjcMsgSendIntPtr(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void ObjcMsgSendVoid(IntPtr receiver, IntPtr selector);
#endif

#if WINDOWS
    private void CommandTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

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
        if (e.Handled)
        {
            return;
        }

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
        if (e.Handled)
        {
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Tab)
        {
            windowsSelectAllOnTabNavigationPending = true;
        }
    }

    private void WindowsTextBox_PreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (!TryGetWindowsHistoryShortcutAction(e, out var action))
        {
            return;
        }

        if (sender is Microsoft.UI.Xaml.Controls.TextBox textBox &&
            ShouldPreferTextHistoryShortcut(textBox) &&
            HasWindowsTextHistoryShortcut(textBox, action))
        {
            return;
        }

        if (TryExecuteHistoryShortcut(action))
        {
            e.Handled = true;
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

    private void WindowsModalInput_LostFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        QueueWindowsEditorFocusRestore();
    }

    private void QueueWindowsEditorFocusRestore()
    {
        if (windowsEditorFocusRestorePending)
        {
            return;
        }

        windowsEditorFocusRestorePending = true;
        Dispatcher.DispatchDelayed(windowsFocusRestorePrimaryDelay, () => TryRestoreWindowsEditorFocus(isFinalAttempt: false));
        Dispatcher.DispatchDelayed(windowsFocusRestoreSecondaryDelay, () => TryRestoreWindowsEditorFocus(isFinalAttempt: true));
    }

    private void QueueWindowsConflictDialogFocusRestore()
    {
        if (windowsConflictFocusRestorePending)
        {
            return;
        }

        windowsConflictFocusRestorePending = true;
        Dispatcher.DispatchDelayed(windowsFocusRestorePrimaryDelay, () => TryRestoreWindowsConflictDialogFocus(isFinalAttempt: false));
        Dispatcher.DispatchDelayed(windowsFocusRestoreSecondaryDelay, () => TryRestoreWindowsConflictDialogFocus(isFinalAttempt: true));
    }

    private void TryRestoreWindowsEditorFocus(bool isFinalAttempt)
    {
        if (!windowsEditorFocusRestorePending)
        {
            return;
        }

        var hasEditorFocus = HasWindowsEditorModalFocus();
        if (!WindowsModalFocusRestorePolicy.ShouldRestoreEditorFocus(
                isWindows: true,
                isEditorOpen: viewModel.IsEditorOpen,
                isConflictDialogOpen: IsConflictDialogOpen(),
                hasEditorFocus: hasEditorFocus))
        {
            windowsEditorFocusRestorePending = false;
            return;
        }

        ModalCommandEntry.Focus();
        EnsureWindowsKeyHooks();
        var restored = HasWindowsEditorModalFocus();
        if (restored || isFinalAttempt)
        {
            windowsEditorFocusRestorePending = false;
        }
    }

    private void TryRestoreWindowsConflictDialogFocus(bool isFinalAttempt)
    {
        if (!windowsConflictFocusRestorePending)
        {
            return;
        }

        var hasConflictFocus = HasWindowsConflictDialogButtonFocus();
        if (!WindowsModalFocusRestorePolicy.ShouldRestoreConflictDialogFocus(
                isWindows: true,
                isConflictDialogOpen: IsConflictDialogOpen(),
                hasConflictButtonFocus: hasConflictFocus))
        {
            windowsConflictFocusRestorePending = false;
            return;
        }

        var target = conflictDialogPseudoFocusedTarget ?? ConflictDialogFocusTarget.Cancel;
        var button = target switch
        {
            ConflictDialogFocusTarget.Reload => ConflictReloadButton,
            ConflictDialogFocusTarget.Overwrite => ConflictOverwriteButton,
            _ => ConflictCancelButton,
        };

        FocusConflictDialogActionButton(button, target);
        EnsureWindowsKeyHooks();
        var restored = HasWindowsConflictDialogButtonFocus();
        if (restored || isFinalAttempt)
        {
            windowsConflictFocusRestorePending = false;
        }
    }

    private bool HasWindowsEditorModalFocus()
    {
        return IsWindowsTextBoxFocused(modalGuidTextBox) ||
            IsWindowsTextBoxFocused(modalCommandTextBox) ||
            IsWindowsTextBoxFocused(modalButtonTextTextBox) ||
            IsWindowsTextBoxFocused(modalToolTextBox) ||
            IsWindowsTextBoxFocused(modalArgumentsTextBox) ||
            IsWindowsTextBoxFocused(modalClipWordTextBox) ||
            IsWindowsTextBoxFocused(modalNoteTextBox) ||
            IsButtonFocused(ModalCancelButton) ||
            IsButtonFocused(ModalSaveButton);
    }

    private bool HasWindowsConflictDialogButtonFocus()
    {
        return IsButtonFocused(ConflictReloadButton) ||
            IsButtonFocused(ConflictOverwriteButton) ||
            IsButtonFocused(ConflictCancelButton);
    }

    private static bool IsWindowsTextBoxFocused(Microsoft.UI.Xaml.Controls.TextBox? textBox)
    {
        return textBox is not null &&
            textBox.FocusState != Microsoft.UI.Xaml.FocusState.Unfocused;
    }

    private void PageNativeElement_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (TryHandleThemeShortcutFromKey(e))
        {
            e.Handled = true;
            return;
        }

        var ctrlDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var shiftDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (TryGetWindowsHistoryShortcutAction(e, out var historyAction) &&
            TryExecuteHistoryShortcut(historyAction))
        {
            e.Handled = true;
            return;
        }

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

    private static bool TryGetWindowsHistoryShortcutAction(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e, out string action)
    {
        action = string.Empty;
        var ctrlDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var shiftDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        if (!ctrlDown)
        {
            return false;
        }

        if (!shiftDown && e.Key == Windows.System.VirtualKey.Z)
        {
            action = "Undo";
            return true;
        }

        if ((!shiftDown && e.Key == Windows.System.VirtualKey.Y) ||
            (shiftDown && e.Key == Windows.System.VirtualKey.Z))
        {
            action = "Redo";
            return true;
        }

        return false;
    }

    private bool ShouldPreferTextHistoryShortcut(Microsoft.UI.Xaml.Controls.TextBox textBox)
    {
        return ReferenceEquals(textBox, commandTextBox) ||
            ReferenceEquals(textBox, searchTextBox);
    }

    private static bool HasWindowsTextHistoryShortcut(Microsoft.UI.Xaml.Controls.TextBox textBox, string action)
    {
        if (string.Equals(action, "Undo", StringComparison.OrdinalIgnoreCase))
        {
            return textBox.CanUndo;
        }

        if (string.Equals(action, "Redo", StringComparison.OrdinalIgnoreCase))
        {
            return textBox.CanRedo;
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
            ModalGuidEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            extraLostFocus: WindowsModalInput_LostFocus);
        SyncWindowsTextBoxHooks(
            ref modalCommandTextBox,
            ModalCommandEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            extraLostFocus: WindowsModalInput_LostFocus);
        SyncWindowsTextBoxHooks(
            ref modalButtonTextTextBox,
            ModalButtonTextEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            extraLostFocus: WindowsModalInput_LostFocus);
        SyncWindowsTextBoxHooks(
            ref modalToolTextBox,
            ModalToolEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            extraLostFocus: WindowsModalInput_LostFocus);
        SyncWindowsTextBoxHooks(
            ref modalArgumentsTextBox,
            ModalArgumentsEntry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            extraLostFocus: WindowsModalInput_LostFocus);
        SyncWindowsTextBoxHooks(
            ref modalClipWordTextBox,
            ModalClipWordEditor.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            extraLostFocus: WindowsModalInput_LostFocus);
        SyncWindowsTextBoxHooks(
            ref modalNoteTextBox,
            ModalNoteEditor.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox,
            extraLostFocus: WindowsModalInput_LostFocus);

        ConfigureWindowsMultilineEditor(modalClipWordTextBox);
        ConfigureWindowsMultilineEditor(modalNoteTextBox);
    }

    private void SyncWindowsTextBoxHooks(
        ref Microsoft.UI.Xaml.Controls.TextBox? slot,
        Microsoft.UI.Xaml.Controls.TextBox? current,
        Microsoft.UI.Xaml.Input.KeyEventHandler? extraKeyDown = null,
        Microsoft.UI.Xaml.Input.PointerEventHandler? extraPointerPressed = null,
        Microsoft.UI.Xaml.RoutedEventHandler? extraLostFocus = null)
    {
        if (ReferenceEquals(slot, current))
        {
            return;
        }

        if (slot is not null)
        {
            slot.PreviewKeyDown -= WindowsTextBox_PreviewKeyDown;
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

            if (extraLostFocus is not null)
            {
                slot.LostFocus -= extraLostFocus;
            }
        }

        slot = current;
        if (slot is null)
        {
            return;
        }

        slot.PreviewKeyDown += WindowsTextBox_PreviewKeyDown;
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

        if (extraLostFocus is not null)
        {
            slot.LostFocus += extraLostFocus;
        }
    }

    private static void ConfigureWindowsMultilineEditor(Microsoft.UI.Xaml.Controls.TextBox? textBox)
    {
        if (textBox is null)
        {
            return;
        }

        textBox.AcceptsReturn = true;
        textBox.TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap;
        Microsoft.UI.Xaml.Controls.ScrollViewer.SetVerticalScrollBarVisibility(
            textBox,
            Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto);
        Microsoft.UI.Xaml.Controls.ScrollViewer.SetVerticalScrollMode(
            textBox,
            Microsoft.UI.Xaml.Controls.ScrollMode.Auto);
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
        void HandleShortcut()
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
            var canCancel = viewModel.CancelEditorCommand.CanExecute(null);
            if (canCancel)
            {
                viewModel.CancelEditorCommand.Execute(null);
            }
        }

        if (Dispatcher.IsDispatchRequired)
        {
            Dispatcher.Dispatch(HandleShortcut);
            return;
        }

        HandleShortcut();
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

    private void OnHistoryShortcutRequested(string action)
    {
        Dispatcher.Dispatch(() => TryExecuteHistoryShortcut(action));
    }

    private bool TryExecuteHistoryShortcut(string action)
    {
        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen || IsConflictDialogOpen())
        {
            return false;
        }

        if (string.Equals(action, "Undo", StringComparison.OrdinalIgnoreCase))
        {
            if (viewModel.UndoCommand.CanExecute(null))
            {
                HideQuickLookPopup();
                viewModel.UndoCommand.Execute(null);
                return true;
            }

            return false;
        }

        if (string.Equals(action, "Redo", StringComparison.OrdinalIgnoreCase))
        {
            if (viewModel.RedoCommand.CanExecute(null))
            {
                HideQuickLookPopup();
                viewModel.RedoCommand.Execute(null);
                return true;
            }

            return false;
        }

        return false;
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

#if MACCATALYST
    private void OnMacApplicationActivated()
    {
        if (!xamlLoaded)
        {
            return;
        }

        ScheduleMainCommandFocusAfterActivation("App.MacApplicationActivated");
    }

    private void OnMacApplicationDeactivating()
    {
        lastPointerOnRoot = null;
        macSearchFocusUserIntentUntilUtc = DateTimeOffset.MinValue;
    }

    private void AttachMacActivationObservers()
    {
        if (macDidBecomeActiveObserver is not null)
        {
            return;
        }

        var center = NSNotificationCenter.DefaultCenter;
        macDidBecomeActiveObserver = center.AddObserver(UIApplication.DidBecomeActiveNotification, _ =>
        {
            App.RecordActivation();
            App.SetMacApplicationActive(true);
            ScheduleMainCommandFocusAfterActivation("UIApplication.DidBecomeActiveNotification");
        });
        macWillEnterForegroundObserver = center.AddObserver(UIApplication.WillEnterForegroundNotification, _ =>
        {
            App.RecordActivation();
            App.SetMacApplicationActive(true);
            ScheduleMainCommandFocusAfterActivation("UIApplication.WillEnterForegroundNotification");
        });
        macSceneWillEnterForegroundObserver = center.AddObserver(UIScene.WillEnterForegroundNotification, _ =>
        {
            App.RecordActivation();
            App.SetMacApplicationActive(true);
            ScheduleMainCommandFocusAfterActivation("UIScene.WillEnterForegroundNotification");
        });
        macSceneDidActivateObserver = center.AddObserver(UIScene.DidActivateNotification, _ =>
        {
            App.RecordActivation();
            App.SetMacApplicationActive(true);
            ScheduleMainCommandFocusAfterActivation("UIScene.DidActivateNotification");
        });
        macWindowDidBecomeKeyObserver = center.AddObserver(UIWindow.DidBecomeKeyNotification, _ =>
        {
            if (UIApplication.SharedApplication.ApplicationState != UIApplicationState.Active)
            {
                return;
            }

            App.RecordActivation();
            App.SetMacApplicationActive(true);
            ScheduleMainCommandFocusAfterActivation("UIWindow.DidBecomeKeyNotification");
        });
        macWillResignActiveObserver = center.AddObserver(UIApplication.WillResignActiveNotification, _ =>
        {
            App.SetMacApplicationActive(false);
        });
        macDidEnterBackgroundObserver = center.AddObserver(UIApplication.DidEnterBackgroundNotification, _ =>
        {
            App.SetMacApplicationActive(false);
        });
        macWindowDidResignKeyObserver = center.AddObserver(UIWindow.DidResignKeyNotification, _ =>
        {
            App.SetMacApplicationActive(false);
        });
    }

    private void DetachMacActivationObservers()
    {
        if (macDidBecomeActiveObserver is null &&
            macWillEnterForegroundObserver is null &&
            macSceneDidActivateObserver is null &&
            macSceneWillEnterForegroundObserver is null &&
            macWindowDidBecomeKeyObserver is null &&
            macWillResignActiveObserver is null &&
            macDidEnterBackgroundObserver is null &&
            macWindowDidResignKeyObserver is null)
        {
            return;
        }

        var center = NSNotificationCenter.DefaultCenter;
        DisposeMacObserver(center, ref macDidBecomeActiveObserver);
        DisposeMacObserver(center, ref macWillEnterForegroundObserver);
        DisposeMacObserver(center, ref macSceneDidActivateObserver);
        DisposeMacObserver(center, ref macSceneWillEnterForegroundObserver);
        DisposeMacObserver(center, ref macWindowDidBecomeKeyObserver);
        DisposeMacObserver(center, ref macWillResignActiveObserver);
        DisposeMacObserver(center, ref macDidEnterBackgroundObserver);
        DisposeMacObserver(center, ref macWindowDidResignKeyObserver);
    }

    private static void DisposeMacObserver(NSNotificationCenter center, ref NSObject? observer)
    {
        if (observer is null)
        {
            return;
        }

        center.RemoveObserver(observer);
        observer.Dispose();
        observer = null;
    }

    private bool IsMacSearchFocusUserInitiated()
    {
        return DateTimeOffset.UtcNow <= macSearchFocusUserIntentUntilUtc;
    }

    private bool IsMacActivationFocusSessionInProgress()
    {
        return DateTimeOffset.UtcNow <= macActivationFocusSessionUntilUtc;
    }

    private bool ShouldAllowMacSearchEntryFocusCore()
    {
        var shouldFocusMainCommand = ShouldFocusMainCommandAfterWindowActivation() && IsMacActivationFocusSessionInProgress();
        var isForeground = IsMacAppForegroundActive();
        var userInitiated = IsMacSearchFocusUserInitiated();
        return SearchFocusGuardPolicy.ShouldAllowSearchFocus(
            shouldFocusMainCommand: shouldFocusMainCommand,
            isAppForeground: isForeground,
            isUserInitiated: userInitiated);
    }

    private void MarkMacSearchFocusUserIntent(string _)
    {
        macSearchFocusUserIntentUntilUtc = DateTimeOffset.UtcNow + macSearchFocusUserIntentWindow;
    }

    private void FocusMainSearchEntryFromCommandTabCore()
    {
        if (!xamlLoaded || !initialized || viewModel.IsEditorOpen || IsConflictDialogOpen())
        {
            return;
        }

        MainSearchEntry.Focus();
        if (MainSearchEntry.Handler?.PlatformView is UITextField searchField && searchField.CanBecomeFirstResponder)
        {
            searchField.BecomeFirstResponder();
        }
    }
#endif

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
            HideQuickLookPopup();
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
            Dispatcher.DispatchDelayed(UiTimingPolicy.ConflictDialogFocusRetryDelay, () =>
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
        RestoreEditorFocusAfterConflictDialogClose();
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

    private void RestoreEditorFocusAfterConflictDialogClose()
    {
        Dispatcher.DispatchDelayed(UiTimingPolicy.ConflictDialogEditorFocusRestoreDelay, () =>
        {
            if (!ConflictDialogFocusRestorePolicy.ShouldRestoreEditorFocus(viewModel.IsEditorOpen, IsConflictDialogOpen()))
            {
                return;
            }

            ModalCommandEntry.Focus();
#if WINDOWS
            EnsureWindowsKeyHooks();
#endif
        });
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

        HideQuickLookPopup();

        var pointer = e.GetPosition(RootGrid);
        if (pointer is not null)
        {
            lastPointerOnRoot = pointer.Value;
            SyncDockScrollBarVisibility(pointer.Value);
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
            SyncDockScrollBarVisibility(pointer.Value);
            return;
        }

        lastPointerOnRoot = null;
        CancelDockHoverExitHide();
        SetDockScrollBarVisibility(isPointerOverDockRegion: false);
    }

    private void RootGrid_PointerExited(object? sender, PointerEventArgs e)
    {
        lastPointerOnRoot = null;
        CancelDockHoverExitHide();
        SetDockScrollBarVisibility(isPointerOverDockRegion: false);
    }

    private void SyncDockScrollBarVisibility(Point rootPoint)
    {
        var isPointerOverDockRegion = IsPointInsideElement(rootPoint, DockRegionBorder);
        if (isPointerOverDockRegion)
        {
            CancelDockHoverExitHide();
        }
        SetDockScrollBarVisibility(isPointerOverDockRegion);
    }

#if MACCATALYST
    private bool HandleMacMiddleClick(Point rootPoint)
    {
        if (!App.IsMacApplicationActive() || App.IsActivationSuppressionActive())
        {
            return false;
        }

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

    private static void ApplyCommandSuggestionRowThemeColors(Grid row, bool selected)
    {
        row.SetAppThemeColor(
            VisualElement.BackgroundColorProperty,
            Color.FromArgb(CommandSuggestionRowColorPolicy.ResolveBackgroundHex(selected, isDarkTheme: false)),
            Color.FromArgb(CommandSuggestionRowColorPolicy.ResolveBackgroundHex(selected, isDarkTheme: true)));
    }

    private static void ApplyCommandSuggestionLabelThemeColors(Label label)
    {
        label.SetAppThemeColor(
            Label.TextColorProperty,
            Color.FromArgb(ThemeTextColorPolicy.ResolveTextColorHex(isDarkTheme: false)),
            Color.FromArgb(ThemeTextColorPolicy.ResolveTextColorHex(isDarkTheme: true)));
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
            };
            ApplyCommandSuggestionRowThemeColors(row, item.IsSelected);

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
            };
            ApplyCommandSuggestionLabelThemeColors(commandLabel);
            row.Children.Add(commandLabel);

            var buttonTextLabel = new Label
            {
                Text = item.ButtonText,
                LineBreakMode = LineBreakMode.TailTruncation,
            };
            ApplyCommandSuggestionLabelThemeColors(buttonTextLabel);
            row.SetColumn(buttonTextLabel, 1);
            row.Children.Add(buttonTextLabel);

            var toolArgsLabel = new Label
            {
                Text = item.ToolArguments,
                LineBreakMode = LineBreakMode.TailTruncation,
            };
            ApplyCommandSuggestionLabelThemeColors(toolArgsLabel);
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

    private static void SelectAllMacEntryText(Entry entry)
    {
        if (entry.Handler?.PlatformView is not UITextField textField)
        {
            return;
        }

        if (!textField.IsFirstResponder && textField.CanBecomeFirstResponder)
        {
            textField.BecomeFirstResponder();
        }

        var allTextRange = textField.GetTextRange(textField.BeginningOfDocument, textField.EndOfDocument);
        if (allTextRange is not null)
        {
            textField.SelectedTextRange = allTextRange;
        }
    }

    private void ForceMacCommandFirstResponder()
    {
        if (MainCommandEntry.Handler?.PlatformView is not UITextField commandField)
        {
            return;
        }

        var commandWindow = commandField.Window ?? Window?.Handler?.PlatformView as UIWindow;
        if (commandWindow is not null && !commandWindow.IsKeyWindow)
        {
            commandWindow.MakeKeyAndVisible();
        }

        var firstResponder = GetCurrentMacFirstResponder();
        var commandAlreadyFirst = ReferenceEquals(firstResponder, commandField) || commandField.IsFirstResponder;
        if (commandAlreadyFirst)
        {
            SelectAllMacTextField(commandField);
            return;
        }

        MainCommandEntry.Focus();

        firstResponder = GetCurrentMacFirstResponder();
        if (firstResponder is not null &&
            !ReferenceEquals(firstResponder, commandField) &&
            firstResponder.CanResignFirstResponder)
        {
            firstResponder.ResignFirstResponder();
        }

        if (commandField.CanBecomeFirstResponder)
        {
            commandField.BecomeFirstResponder();
        }

        SelectAllMacTextField(commandField);
    }

    private static void SelectAllMacTextField(UITextField textField)
    {
        var allTextRange = textField.GetTextRange(textField.BeginningOfDocument, textField.EndOfDocument);
        if (allTextRange is not null)
        {
            textField.SelectedTextRange = allTextRange;
        }

        var selectAllSelector = new Selector("selectAll:");
        if (textField.RespondsToSelector(selectAllSelector))
        {
            textField.PerformSelector(selectAllSelector, null, 0);
        }
    }

    private static int GetSelectedLength(UITextField textField)
    {
        var selectedRange = textField.SelectedTextRange;
        if (selectedRange is null)
        {
            return 0;
        }

        return (int)textField.GetOffsetFromPosition(selectedRange.Start, selectedRange.End);
    }

    private bool IsMainCommandSelectAllSatisfied(out int textLength, out int selectedLength)
    {
        if (MainCommandEntry.Handler?.PlatformView is UITextField commandField)
        {
            textLength = commandField.Text?.Length ?? 0;
            selectedLength = GetSelectedLength(commandField);
            return textLength <= 0 || selectedLength >= textLength;
        }

        textLength = MainCommandEntry.Text?.Length ?? 0;
        selectedLength = MainCommandEntry.SelectionLength;
        return textLength <= 0 || selectedLength >= textLength;
    }

    private static UIResponder? GetCurrentMacFirstResponder()
    {
        var keyWindow = GetMacKeyWindow();
        if (keyWindow is not null)
        {
            var keyWindowFirstResponder = FindFirstResponderInWindow(keyWindow);
            if (keyWindowFirstResponder is not null)
            {
                return keyWindowFirstResponder;
            }
        }

        foreach (var window in EnumerateMacWindows())
        {
            var responder = FindFirstResponderInWindow(window);
            if (responder is not null)
            {
                return responder;
            }
        }

        return null;
    }

    private static UIWindow? GetMacKeyWindow()
    {
        foreach (var window in EnumerateMacWindows())
        {
            if (window.IsKeyWindow)
            {
                return window;
            }
        }

        return null;
    }

    private static IEnumerable<UIWindow> EnumerateMacWindows()
    {
        var app = UIApplication.SharedApplication;
        if (app is null)
        {
            yield break;
        }

        var seenWindows = new HashSet<nint>();
        if (app.ConnectedScenes is not null)
        {
            foreach (var scene in app.ConnectedScenes)
            {
                if (scene is not UIWindowScene windowScene)
                {
                    continue;
                }

                foreach (var window in windowScene.Windows)
                {
                    if (window is null || !seenWindows.Add(window.Handle))
                    {
                        continue;
                    }

                    yield return window;
                }
            }
        }

    }

    private static UIResponder? FindFirstResponderInWindow(UIWindow window)
    {
        if (window.IsFirstResponder)
        {
            return window;
        }

        foreach (var subview in window.Subviews)
        {
            if (subview is null)
            {
                continue;
            }

            var responder = FindFirstResponderInView(subview);
            if (responder is not null)
            {
                return responder;
            }
        }

        return null;
    }

    private static UIResponder? FindFirstResponderInView(UIView view)
    {
        if (view.IsFirstResponder)
        {
            return view;
        }

        foreach (var subview in view.Subviews)
        {
            if (subview is null)
            {
                continue;
            }

            var responder = FindFirstResponderInView(subview);
            if (responder is not null)
            {
                return responder;
            }
        }

        return null;
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
        var dark = IsDarkThemeActive();
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
            };
            ApplyCommandSuggestionRowThemeColors(row, item.IsSelected);

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
            };
            ApplyCommandSuggestionLabelThemeColors(commandLabel);
            row.Children.Add(commandLabel);

            var buttonTextLabel = new Label
            {
                Text = item.ButtonText,
                LineBreakMode = LineBreakMode.TailTruncation,
            };
            ApplyCommandSuggestionLabelThemeColors(buttonTextLabel);
            row.SetColumn(buttonTextLabel, 1);
            row.Children.Add(buttonTextLabel);

            var toolArgsLabel = new Label
            {
                Text = item.ToolArguments,
                LineBreakMode = LineBreakMode.TailTruncation,
            };
            ApplyCommandSuggestionLabelThemeColors(toolArgsLabel);
            row.SetColumn(toolArgsLabel, 2);
            row.Children.Add(toolArgsLabel);

            CommandSuggestionStack.Children.Add(row);
        }
    }

    private void ApplyMacVisualTuning()
    {
        EnsureMacFirstResponder();
        ApplyMacContentScale();
        ApplyMacEntryVisualState();
        ApplyMacClipWordEditorVisualState();
        ApplyMacNoteEditorVisualState();
        ApplyMacModalPseudoFocusVisuals();
        ApplyMacCommandSuggestionKeyCommands();
        ApplyMacEditorKeyCommands();
    }

    private void ApplyMacEntryVisualState()
    {
        RefreshMacEntryVisualState(MainCommandEntry);
        RefreshMacEntryVisualState(MainSearchEntry);
        RefreshMacEntryVisualState(ModalGuidEntry);
        RefreshMacEntryVisualState(ModalCommandEntry);
        RefreshMacEntryVisualState(ModalButtonTextEntry);
        RefreshMacEntryVisualState(ModalToolEntry);
        RefreshMacEntryVisualState(ModalArgumentsEntry);
    }

    private static void RefreshMacEntryVisualState(Entry entry)
    {
        if (entry.Handler?.PlatformView is not UITextField textField)
        {
            return;
        }

        textField.SetNeedsLayout();
        textField.LayoutIfNeeded();
    }

    private void ApplyMacClipWordEditorVisualState()
    {
        if (ModalClipWordEditor.Handler?.PlatformView is not UITextView textView)
        {
            return;
        }

        var dark = IsDarkThemeActive();
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
        Dispatcher.DispatchDelayed(UiTimingPolicy.MacInitialCommandFocusRetryDelay, () =>
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

        var dark = IsDarkThemeActive();
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
        if (app.ApplicationState == UIApplicationState.Active)
        {
            return true;
        }

        if (app.ApplicationState == UIApplicationState.Background)
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

        if (app.ApplicationState == UIApplicationState.Inactive)
        {
            foreach (var scene in app.ConnectedScenes)
            {
                if (scene is UIScene uiScene && uiScene.ActivationState == UISceneActivationState.ForegroundInactive)
                {
                    return true;
                }
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
        macMiddleButtonPollTimer.Interval = UiTimingPolicy.MacMiddleButtonPollingInterval;
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

        try
        {
            prop.SetValue(platformView, isTabStop);
        }
        catch
        {
        }
    }
#endif
}
