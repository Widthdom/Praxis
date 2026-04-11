using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

using Praxis.Behaviors;
using Praxis.Core.Logic;
using Praxis.Core.Models;
using Praxis.Services;
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
            CrashFileLogger.WriteException("MainPage.InitializeComponent", ex);
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
        DockScroll.SizeChanged += (_, _) => RefreshDockScrollBarVisibility();
        DockButtonsStack.SizeChanged += (_, _) => RefreshDockScrollBarVisibility();
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

        try
        {
            await viewModel.InitializeAsync();
            initialized = true;
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
            initialized = false;
            CrashFileLogger.WriteException("MainPage.OnAppearing.InitializeAsync", ex);
            try
            {
                await DisplayAlertAsync("Initialization Error", ex.Message, "OK");
            }
            catch (Exception alertEx)
            {
                CrashFileLogger.WriteException("MainPage.OnAppearing.DisplayAlertAsync", alertEx);
            }
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
        viewModel.NotifyWindowDisappearing();
        base.OnDisappearing();
        DetachWindowActivationHook();
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
#if MACCATALYST
            DetachMacActivationObservers();
#endif
            return;
        }

        attachedWindow.Activated -= OnWindowActivated;
        attachedWindow.Resumed -= OnWindowResumed;
        attachedWindow = null;
#if MACCATALYST
        DetachMacActivationObservers();
#endif
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
        catch (Exception ex)
        {
            CrashFileLogger.WriteWarning("MainPage.CopyIconButton_Clicked", $"Copy notice animation failed: {ex.Message}");
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                CopyNoticeOverlay.IsVisible = false;
            }
        }
    }

}
