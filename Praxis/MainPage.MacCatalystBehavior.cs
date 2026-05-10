using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Praxis.Behaviors;
using Praxis.Core.Logic;
using Praxis.Core.Models;
using Praxis.Services;
#if MACCATALYST
using CoreAnimation;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;
#endif

namespace Praxis;

public partial class MainPage
{
#if MACCATALYST
    private const nint MacMaterialFrameBackdropTag = 0x50475842;
    private const nint MacModalTextEditorGlassBackdropTag = 0x50544542;
    private static readonly bool MacPlacementPrimarySelectionUsesPolling = false;
    private static readonly TimeSpan MacPlacementRecentHoverWindow = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan MacPlacementPrimaryReleaseGraceWindow = TimeSpan.FromMilliseconds(80);
    private static readonly Point MacPlacementSelectionPointerOffset = new(-4, -14);
    private static readonly UIColor LightMacTextSelectionColor = UIColor.FromRGB(0x4B, 0x00, 0xD9);
    private static readonly UIColor DarkMacTextSelectionColor = UIColor.FromRGB(0x35, 0x00, 0xA8);

    private enum MacPointerScreenPointKind
    {
        Location,
        UnflippedLocation,
        ScaledLocation,
        ScaledUnflippedLocation,
    }

    private enum MacAppKitRootPointKind
    {
        ScaledContentViewFlipped,
        ContentViewFlipped,
        RootFrameFlipped,
        NativeWindowFlipped,
        ContentHeightFlipped,
        Window,
    }

    private void MoveModalFocus(bool forward)
    {
        if (ModalFocusOrder.Length == 0)
        {
            return;
        }

        var currentIndex = GetCurrentModalFocusIndex();
        if (currentIndex < 0)
        {
            TryFocusModalPrimaryTarget();
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
            ModalFocusTarget.InvertThemeColors => IsModalFocusTargetActive(ModalInvertThemeCheckBox),
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
            case ModalFocusTarget.InvertThemeColors:
                return TryFocusModalVisual(ModalInvertThemeCheckBox);
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

    private bool TryFocusModalPrimaryTarget(bool selectAllText = false)
    {
        if (!TryFocusModalVisual(ModalButtonTextEntry))
        {
            return false;
        }

        if (selectAllText)
        {
            SelectAllMacEntryText(ModalButtonTextEntry);
        }

        return IsModalFocusTargetActive(ModalButtonTextEntry);
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

        ApplyMacPseudoFocusVisual(ModalCancelButton, macPseudoFocusedModalTarget == ModalFocusTarget.CancelButton, focusedBorderColor, dark);
        ApplyMacPseudoFocusVisual(ModalSaveButton, macPseudoFocusedModalTarget == ModalFocusTarget.SaveButton, focusedBorderColor, dark);
    }

    private static void ApplyMacPseudoFocusVisual(Border button, bool focused, Color focusedBorderColor, bool dark)
    {
        if (focused)
        {
            button.Stroke = new SolidColorBrush(focusedBorderColor);
            button.StrokeThickness = 1.5;
            return;
        }

        button.Stroke = new SolidColorBrush(Colors.Transparent);
        button.StrokeThickness = 0;
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

            var pointer = new PointerGestureRecognizer();
            pointer.PointerPressed += (_, e) =>
            {
                if (!App.IsMacApplicationActive() || App.IsActivationSuppressionActive())
                {
                    return;
                }

                if (IsOtherMouseFromPlatformArgs(e.PlatformArgs) || IsMiddlePointerPressed(e))
                {
                    CloseCommandSuggestionPopup();
                    if (viewModel.OpenEditorCommand.CanExecute(item.Source))
                    {
                        viewModel.OpenEditorCommand.Execute(item.Source);
                    }

                    return;
                }

                if (IsSecondaryPointerPressed(e))
                {
                    CloseCommandSuggestionPopup();
                    if (viewModel.OpenContextMenuCommand.CanExecute(item.Source))
                    {
                        viewModel.OpenContextMenuCommand.Execute(item.Source);
                    }
                }
            };
            row.GestureRecognizers.Add(pointer);

            var secondaryTap = new TapGestureRecognizer { Buttons = ButtonsMask.Secondary };
            secondaryTap.Tapped += (_, _) =>
            {
                CloseCommandSuggestionPopup();
                if (viewModel.OpenContextMenuCommand.CanExecute(item.Source))
                {
                    viewModel.OpenContextMenuCommand.Execute(item.Source);
                }
            };
            row.GestureRecognizers.Add(secondaryTap);

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
        ScheduleMacRootTransparencyRefresh();
        EnsureMacFirstResponder();
        ApplyMacContentScale();
        ApplyMacEntryVisualState();
        ApplyMacClipWordEditorVisualState();
        ApplyMacNoteEditorVisualState();
        ScheduleMacModalButtonVisualStateRefresh();
        ApplyMacModalPseudoFocusVisuals();
        ApplyMacCommandSuggestionKeyCommands();
        ApplyMacEditorKeyCommands();
        ScheduleMacPlacementCanvasNativeGesturesRefresh();
    }

    private void EnsureMacPlacementCanvasNativeGestures()
    {
        if (ResolveMacPlacementGestureNativeView() is not UIView nativeView)
        {
            if (ShouldLogMacPlacementInputDiagnostic(ref macPlacementAttachDiagnosticCount, limit: 8, every: 120))
            {
                LogMacPlacementInputDiagnostic("attach-wait", "native view unavailable");
            }
            return;
        }

        if (nativeView.Bounds.Width <= 0 ||
            nativeView.Bounds.Height <= 0 ||
            PlacementScroll.Width <= 0 ||
            PlacementScroll.Height <= 0)
        {
            if (ShouldLogMacPlacementInputDiagnostic(ref macPlacementAttachDiagnosticCount, limit: 8, every: 120))
            {
                LogMacPlacementInputDiagnostic(
                    "attach-wait",
                    $"view={nativeView.GetType().Name} bounds={nativeView.Bounds} root={RootGrid.Width:0.##}x{RootGrid.Height:0.##} scroll={PlacementScroll.Width:0.##}x{PlacementScroll.Height:0.##}");
            }
            return;
        }

        if (ReferenceEquals(macPlacementGestureNativeView, nativeView) &&
            macPlacementPrimarySelectionRecognizer is not null &&
            macPlacementSecondaryCreateRecognizer is not null)
        {
            return;
        }

        DetachMacPlacementCanvasNativeGestures();
        macPlacementGestureNativeView = nativeView;
        nativeView.UserInteractionEnabled = true;

        macPlacementHoverRecognizer = CreateMacPlacementHoverRecognizer();
        nativeView.AddGestureRecognizer(macPlacementHoverRecognizer);
        macPlacementPrimarySelectionRecognizer = CreateMacPlacementPrimarySelectionRecognizer();
        nativeView.AddGestureRecognizer(macPlacementPrimarySelectionRecognizer);
        macPlacementSecondaryCreateRecognizer = CreateMacPlacementSecondaryCreateRecognizer();
        nativeView.AddGestureRecognizer(macPlacementSecondaryCreateRecognizer);
        if (ShouldLogMacPlacementInputDiagnostic(ref macPlacementAttachDiagnosticCount, limit: 8, every: 0))
        {
            LogMacPlacementInputDiagnostic(
                "attach",
                $"view={nativeView.GetType().Name} bounds={nativeView.Bounds} root={RootGrid.Width:0.##}x{RootGrid.Height:0.##} scroll={PlacementScroll.Width:0.##}x{PlacementScroll.Height:0.##}");
        }
    }

    private void ScheduleMacPlacementCanvasNativeGesturesRefresh()
    {
        EnsureMacPlacementCanvasNativeGestures();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(16), EnsureMacPlacementCanvasNativeGestures);
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(120), EnsureMacPlacementCanvasNativeGestures);
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), EnsureMacPlacementCanvasNativeGestures);
    }

    private UIView? ResolveMacPlacementGestureNativeView()
    {
        if (RootGrid.Handler?.PlatformView is UIView rootView &&
            rootView.Window is not null &&
            rootView.Bounds.Width > 0 &&
            rootView.Bounds.Height > 0)
        {
            return rootView;
        }

        if (Handler?.PlatformView is UIView pageView &&
            pageView.Window is not null &&
            pageView.Bounds.Width > 0 &&
            pageView.Bounds.Height > 0)
        {
            return pageView;
        }

        if (Handler?.PlatformView is UIView pageWindowView && pageWindowView.Window is UIWindow pageWindow)
        {
            return pageWindow;
        }

        if (RootGrid.Handler?.PlatformView is UIView fallbackRootView)
        {
            return fallbackRootView;
        }

        return null;
    }

    private void DetachMacPlacementCanvasNativeGestures()
    {
        if (macPlacementSelectionOverlayView is not null)
        {
            macPlacementSelectionOverlayView.RemoveFromSuperview();
            macPlacementSelectionOverlayView = null;
        }

        if (macPlacementGestureNativeView is not null)
        {
            if (macPlacementHoverRecognizer is not null)
            {
                macPlacementGestureNativeView.RemoveGestureRecognizer(macPlacementHoverRecognizer);
            }

            if (macPlacementPrimarySelectionRecognizer is not null)
            {
                macPlacementGestureNativeView.RemoveGestureRecognizer(macPlacementPrimarySelectionRecognizer);
            }

            if (macPlacementSecondaryCreateRecognizer is not null)
            {
                macPlacementGestureNativeView.RemoveGestureRecognizer(macPlacementSecondaryCreateRecognizer);
            }
        }

        macPlacementGestureNativeView = null;
        macPlacementHoverRootPoint = null;
        macPlacementHoverRecognizer = null;
        macPlacementPrimarySelectionRecognizer = null;
        macPlacementSecondaryCreateRecognizer = null;
        macPlacementNativeSelectionIgnored = false;
        if (ShouldLogMacPlacementInputDiagnostic(ref macPlacementDetachDiagnosticCount, limit: 8, every: 0))
        {
            LogMacPlacementInputDiagnostic("detach", "placement native recognizers detached");
        }
    }

    private UIHoverGestureRecognizer CreateMacPlacementHoverRecognizer()
    {
        var recognizer = new UIHoverGestureRecognizer(HandleMacPlacementHoverRecognizer)
        {
            CancelsTouchesInView = false,
            Delegate = macPlacementGestureDelegate,
        };
        return recognizer;
    }

    private void HandleMacPlacementHoverRecognizer()
    {
        if (macPlacementHoverRecognizer is null || macPlacementGestureNativeView is null)
        {
            return;
        }

        switch (macPlacementHoverRecognizer.State)
        {
            case UIGestureRecognizerState.Began:
            case UIGestureRecognizerState.Changed:
                var location = macPlacementHoverRecognizer.LocationInView(macPlacementGestureNativeView);
                macPlacementHoverRootPoint = new Point(location.X, location.Y);
                macPlacementHoverRootPointUpdatedAtUtc = DateTimeOffset.UtcNow;
                lastPointerOnRoot = macPlacementHoverRootPoint;
                SyncDockScrollBarVisibility(macPlacementHoverRootPoint.Value);
                if (ShouldLogMacPlacementInputDiagnostic(ref macPlacementHoverDiagnosticCount, limit: 12, every: 240))
                {
                    LogMacPlacementInputDiagnostic(
                        "hover",
                        $"state={macPlacementHoverRecognizer.State} root=({macPlacementHoverRootPoint.Value.X:0.##},{macPlacementHoverRootPoint.Value.Y:0.##})");
                }
                return;
            case UIGestureRecognizerState.Ended:
            case UIGestureRecognizerState.Cancelled:
            case UIGestureRecognizerState.Failed:
                return;
            default:
                return;
        }
    }

    private UILongPressGestureRecognizer CreateMacPlacementPrimarySelectionRecognizer()
    {
        var recognizer = new UILongPressGestureRecognizer(HandleMacPlacementPrimarySelectionRecognizer)
        {
            CancelsTouchesInView = false,
            DelaysTouchesBegan = false,
            DelaysTouchesEnded = false,
            NumberOfTouchesRequired = 1,
            MinimumPressDuration = 0,
            AllowableMovement = nfloat.MaxValue,
            Delegate = macPlacementGestureDelegate,
        };
        TrySetMacPlacementButtonMaskRequired(recognizer, 0x1);
        return recognizer;
    }

    private UILongPressGestureRecognizer CreateMacPlacementSecondaryCreateRecognizer()
    {
        var recognizer = new UILongPressGestureRecognizer(HandleMacPlacementSecondaryCreateRecognizer)
        {
            CancelsTouchesInView = false,
            DelaysTouchesBegan = false,
            DelaysTouchesEnded = false,
            NumberOfTouchesRequired = 1,
            MinimumPressDuration = 0.01,
            AllowableMovement = nfloat.MaxValue,
            Delegate = macPlacementGestureDelegate,
        };
        TrySetMacPlacementButtonMaskRequired(recognizer, 0x2);
        return recognizer;
    }

    private void HandleMacPlacementPrimarySelectionRecognizer()
    {
        if (macPlacementPrimarySelectionRecognizer is null)
        {
            return;
        }

        if (ShouldLogMacPlacementInputDiagnostic(ref macPlacementPrimaryRecognizerDiagnosticCount, limit: 12, every: 120))
        {
            LogMacPlacementInputDiagnostic("primary-recognizer", $"state={macPlacementPrimarySelectionRecognizer.State}");
        }

        if (MacPlacementPrimarySelectionUsesPolling)
        {
            return;
        }

        switch (macPlacementPrimarySelectionRecognizer.State)
        {
            case UIGestureRecognizerState.Began:
                BeginMacPlacementNativeSelection();
                return;
            case UIGestureRecognizerState.Changed:
                ContinueMacPlacementNativeSelection();
                return;
            case UIGestureRecognizerState.Ended:
            case UIGestureRecognizerState.Cancelled:
            case UIGestureRecognizerState.Failed:
                CompleteMacPlacementNativeSelection();
                return;
            default:
                return;
        }
    }

    private void BeginMacPlacementNativeSelection()
    {
        macPlacementNativeSelectionIgnored = true;

        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen)
        {
            return;
        }

        if (selectionDragging)
        {
            return;
        }

        if (!TryGetMacPlacementPrimaryRecognizerRootPoint(out var rootPoint) ||
            !TryGetMacPlacementRootCanvasPoint(rootPoint, allowOutside: false, out var canvasPoint, out var viewportPoint) ||
            IsOnAnyVisibleButton(canvasPoint))
        {
            return;
        }

        var selectionRootPoint = ApplyMacPlacementSelectionPointerOffset(rootPoint);
        if (!TryGetMacPlacementRootCanvasPoint(selectionRootPoint, allowOutside: false, out var selectionCanvasPoint, out var selectionViewportPoint))
        {
            selectionRootPoint = rootPoint;
            selectionCanvasPoint = canvasPoint;
            selectionViewportPoint = viewportPoint;
        }

        CloseCommandSuggestionPopup();
        macPlacementNativeSelectionIgnored = false;
        macPlacementNativeSelectionActive = true;
        selectionDragging = true;
        selectionPanPrimed = true;
        selectionStartCanvas = selectionCanvasPoint;
        selectionStartViewport = selectionViewportPoint;
        selectionLastCanvas = selectionStartCanvas;
        selectionLastViewport = selectionStartViewport;
        macPlacementNativeSelectionStartRootPoint = selectionRootPoint;
        SelectionRect.IsVisible = false;
        SetMacPlacementSelectionCursor();
        UpdateMacPlacementNativeSelectionOverlay(selectionRootPoint, selectionRootPoint);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, selectionStartCanvas.X, selectionStartCanvas.Y, GestureStatus.Started));
        LogMacPlacementInputDiagnostic(
            "native-selection-started",
            $"viewport=({selectionStartViewport.X:0.##},{selectionStartViewport.Y:0.##})");
    }

    private void ContinueMacPlacementNativeSelection()
    {
        if (macPlacementNativeSelectionIgnored || !selectionDragging)
        {
            return;
        }

        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen)
        {
            CancelMacPlacementNativeSelection();
            return;
        }

        if (!TryGetMacPlacementPrimaryRecognizerRunningRootPoint(out var rootPoint))
        {
            return;
        }

        var selectionRootPoint = ApplyMacPlacementSelectionPointerOffset(rootPoint);
        if (!TryGetMacPlacementRootCanvasPoint(selectionRootPoint, allowOutside: true, out var canvasPoint, out var viewportPoint))
        {
            return;
        }

        selectionLastCanvas = canvasPoint;
        selectionLastViewport = viewportPoint;
        SetMacPlacementSelectionCursor();
        UpdateMacPlacementNativeSelectionOverlay(macPlacementNativeSelectionStartRootPoint ?? selectionRootPoint, selectionRootPoint);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, canvasPoint.X, canvasPoint.Y, GestureStatus.Running));
    }

    private void CompleteMacPlacementNativeSelection()
    {
        if (macPlacementNativeSelectionIgnored)
        {
            macPlacementNativeSelectionIgnored = false;
            macPlacementNativeSelectionStartRootPoint = null;
            HideMacPlacementNativeSelectionOverlay();
            return;
        }

        if (!selectionDragging)
        {
            ResetMacPlacementNativeSelectionState();
            return;
        }

        if (TryGetMacPlacementCanvasPoint(macPlacementPrimarySelectionRecognizer, allowOutside: true, out var canvasPoint, out var viewportPoint))
        {
            selectionLastCanvas = canvasPoint;
            selectionLastViewport = viewportPoint;
        }

        selectionDragging = false;
        selectionPanPrimed = false;
        macPlacementNativeSelectionActive = false;
        UpdateSelectionRect(selectionStartViewport, selectionLastViewport, hide: true);
        HideMacPlacementNativeSelectionOverlay(fade: true);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, selectionLastCanvas.X, selectionLastCanvas.Y, GestureStatus.Completed));
        macPlacementNativeSelectionIgnored = false;
        macPlacementNativeSelectionStartRootPoint = null;
    }

    private void CancelMacPlacementNativeSelection()
    {
        if (selectionDragging)
        {
            UpdateSelectionRect(selectionStartViewport, selectionLastViewport, hide: true);
            HideMacPlacementNativeSelectionOverlay();
        }

        ResetMacPlacementNativeSelectionState();
    }

    private void ResetMacPlacementNativeSelectionState()
    {
        selectionDragging = false;
        selectionPanPrimed = false;
        macPlacementNativeSelectionActive = false;
        macPlacementNativeSelectionIgnored = false;
        macPlacementNativeSelectionStartRootPoint = null;
        HideMacPlacementNativeSelectionOverlay();
    }

    private void HandleMacPlacementSecondaryCreateRecognizer()
    {
        if (macPlacementSecondaryCreateRecognizer is not null &&
            ShouldLogMacPlacementInputDiagnostic(ref macPlacementSecondaryRecognizerDiagnosticCount, limit: 12, every: 120))
        {
            LogMacPlacementInputDiagnostic("secondary-recognizer", $"state={macPlacementSecondaryCreateRecognizer.State}");
        }

        if (macPlacementSecondaryCreateRecognizer?.State != UIGestureRecognizerState.Began ||
            viewModel.IsEditorOpen ||
            viewModel.IsContextMenuOpen)
        {
            return;
        }

        if (!TryGetMacPlacementCanvasPoint(macPlacementSecondaryCreateRecognizer, allowOutside: false, out var canvasPoint, out _) ||
            IsOnAnyVisibleButton(canvasPoint))
        {
            return;
        }

        CloseCommandSuggestionPopup();
        _ = OpenCreateEditorFromCanvasPointWithWarningAsync(canvasPoint, nameof(HandleMacPlacementSecondaryCreateRecognizer));
    }

    private bool TryGetMacPlacementCanvasPoint(UIGestureRecognizer? recognizer, bool allowOutside, out Point canvasPoint, out Point viewportPoint)
    {
        canvasPoint = default;
        viewportPoint = default;

        if (recognizer is null)
        {
            return false;
        }

        if (!TryGetMacPlacementRecognizerRootPoint(recognizer, out var rootPoint) ||
            !TryGetMacPlacementRootCanvasPoint(rootPoint, allowOutside, out canvasPoint, out viewportPoint))
        {
            return false;
        }

        return true;
    }

    private bool TryGetMacPlacementPrimaryRecognizerRootPoint(out Point rootPoint)
    {
        if (macPlacementPrimarySelectionRecognizer is null ||
            (macPlacementPrimarySelectionRecognizer.State != UIGestureRecognizerState.Began &&
             macPlacementPrimarySelectionRecognizer.State != UIGestureRecognizerState.Changed))
        {
            rootPoint = default;
            return false;
        }

        return TryGetMacPlacementRecognizerRootPoint(macPlacementPrimarySelectionRecognizer, out rootPoint);
    }

    private bool TryGetMacPlacementPrimaryRecognizerRunningRootPoint(out Point rootPoint)
    {
        if (macPlacementPrimarySelectionRecognizer?.State != UIGestureRecognizerState.Changed)
        {
            rootPoint = default;
            return false;
        }

        return TryGetMacPlacementRecognizerRootPoint(macPlacementPrimarySelectionRecognizer, out rootPoint);
    }

    private bool TryGetMacPlacementRecognizerRootPoint(UIGestureRecognizer recognizer, out Point rootPoint)
    {
        rootPoint = default;

        if (macPlacementGestureNativeView is null ||
            RootGrid.Handler?.PlatformView is not UIView rootView)
        {
            return false;
        }

        var nativeLocation = recognizer.LocationInView(macPlacementGestureNativeView);
        var rootLocation = ReferenceEquals(macPlacementGestureNativeView, rootView)
            ? nativeLocation
            : rootView.ConvertPointFromView(nativeLocation, macPlacementGestureNativeView);
        var candidate = new Point(rootLocation.X, rootLocation.Y);
        if (!IsMacRootPointInsideRoot(candidate))
        {
            return false;
        }

        rootPoint = candidate;
        lastPointerOnRoot = rootPoint;
        return true;
    }

    private static void TrySetMacPlacementButtonMaskRequired(UIGestureRecognizer recognizer, nuint mask)
    {
        try
        {
            recognizer.SetValueForKey(NSNumber.FromUInt64(mask), new NSString("buttonMaskRequired"));
        }
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(TrySetMacPlacementButtonMaskRequired), $"Failed to set placement buttonMaskRequired={mask}: {safeMessage}");
        }
    }

    private sealed class MacPlacementGestureDelegate : UIGestureRecognizerDelegate
    {
        public override bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)
            => true;
    }

    private void ScheduleMacRootTransparencyRefresh()
    {
        App.RefreshMacWindowBackdropForConnectedScenes();
        ApplyMacRootTransparency();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(60), RefreshMacWindowBackdropAndRootTransparency);
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(240), RefreshMacWindowBackdropAndRootTransparency);
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(700), RefreshMacWindowBackdropAndRootTransparency);
    }

    private void RefreshMacWindowBackdropAndRootTransparency()
    {
        App.RefreshMacWindowBackdropForConnectedScenes();
        ApplyMacRootTransparency();
    }

    private void ApplyMacRootTransparency()
    {
        if (Handler?.PlatformView is UIView pageView)
        {
            var rootSize = ResolveMacRootTransparencySize(pageView);
            ClearMacLargeContainerBackgrounds(pageView, rootSize, 0);
        }

        if (Window?.Handler?.PlatformView is UIWindow nativeWindow)
        {
            nativeWindow.Opaque = false;
            nativeWindow.BackgroundColor = UIColor.Clear;
            nativeWindow.Layer.Opaque = false;
            nativeWindow.Layer.BackgroundColor = UIColor.Clear.CGColor;
            if (nativeWindow.RootViewController?.View is UIView rootView)
            {
                var rootSize = ResolveMacRootTransparencySize(rootView);
                ClearMacLargeContainerBackgrounds(rootView, rootSize, 0);
            }
        }
    }

    private static CGSize ResolveMacRootTransparencySize(UIView view)
    {
        if (view.Window?.Bounds.Size is CGSize windowSize &&
            windowSize.Width > 0 &&
            windowSize.Height > 0)
        {
            return windowSize;
        }

        return view.Bounds.Size;
    }

    private static void ClearMacLargeContainerBackgrounds(UIView view, CGSize rootSize, int depth)
    {
        if (ShouldClearMacLargeContainerBackground(view, rootSize, depth))
        {
            view.Opaque = false;
            view.BackgroundColor = UIColor.Clear;
            view.Layer.Opaque = false;
            view.Layer.BackgroundColor = UIColor.Clear.CGColor;
            view.Layer.ShouldRasterize = false;
            if (view is UIVisualEffectView effectView)
            {
                effectView.Effect = null;
                effectView.ContentView.Opaque = false;
                effectView.ContentView.BackgroundColor = UIColor.Clear;
                effectView.ContentView.Layer.Opaque = false;
                effectView.ContentView.Layer.BackgroundColor = UIColor.Clear.CGColor;
            }
        }

        if (depth >= 10)
        {
            return;
        }

        foreach (var subview in view.Subviews)
        {
            ClearMacLargeContainerBackgrounds(subview, rootSize, depth + 1);
        }
    }

    private static bool ShouldClearMacLargeContainerBackground(UIView view, CGSize rootSize, int depth)
    {
        if (view is UIControl or UILabel or UITextField or UITextView or UIImageView)
        {
            return false;
        }

        if (IsIntentionalMacGlassBackdrop(view))
        {
            return false;
        }

        if (depth <= 2)
        {
            return true;
        }

        var width = view.Bounds.Width;
        var height = view.Bounds.Height;
        return rootSize.Width > 0 &&
            rootSize.Height > 0 &&
            width >= rootSize.Width * 0.82 &&
            height >= rootSize.Height * 0.82;
    }

    private static bool IsIntentionalMacGlassBackdrop(UIView view)
    {
        return view.Tag == MacMaterialFrameBackdropTag ||
            view.Tag == MacModalTextEditorGlassBackdropTag;
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

    private void RefreshMacEntryVisualState(Entry entry)
    {
        if (entry.Handler?.PlatformView is not UITextField textField)
        {
            return;
        }

        if (textField is Praxis.Controls.MacEntryHandler.MacEntryTextField macEntryTextField)
        {
            macEntryTextField.SetGlassFieldVisual(IsGlassEntryVisual(entry));
        }

        var dark = IsDarkThemeActive();
        var textColor = ResolveMacTextInputForeground(dark);
        textField.TextColor = textColor;
        textField.TintColor = ResolveMacTextSelectionColor(dark);
        textField.SetNeedsLayout();
        textField.LayoutIfNeeded();
    }

    private static UIColor ResolveMacTextInputForeground(bool dark)
        => dark ? UIColor.White : UIColor.Black;

    private static UIColor ResolveMacTextSelectionColor(bool dark)
        => dark ? DarkMacTextSelectionColor : LightMacTextSelectionColor;

    private bool IsGlassEntryVisual(Entry entry)
    {
        return ReferenceEquals(entry, MainCommandEntry) ||
            ReferenceEquals(entry, MainSearchEntry) ||
            ReferenceEquals(entry, ModalGuidEntry) ||
            ReferenceEquals(entry, ModalButtonTextEntry) ||
            ReferenceEquals(entry, ModalCommandEntry) ||
            ReferenceEquals(entry, ModalToolEntry) ||
            ReferenceEquals(entry, ModalArgumentsEntry);
    }

    private void ApplyMacClipWordEditorVisualState()
    {
        if (ModalClipWordEditor.Handler?.PlatformView is not UITextView textView)
        {
            return;
        }

        ApplyMacModalTextEditorVisualState(textView);
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

        ApplyMacModalTextEditorVisualState(textView);
    }

    private void ApplyMacModalTextEditorVisualState(UITextView textView)
    {
        var dark = IsDarkThemeActive();
        var backdropTintColor = UIColor.Clear;
        var textColor = ResolveMacTextInputForeground(dark);
        var backdropView = EnsureMacModalTextEditorGlassBackdrop(textView);

        textView.Opaque = false;
        textView.BackgroundColor = UIColor.Clear;
        textView.Layer.BackgroundColor = UIColor.Clear.CGColor;
        textView.Layer.CornerRadius = 4;
        textView.Layer.MasksToBounds = true;
        textView.TextColor = textColor;
        textView.TintColor = ResolveMacTextSelectionColor(dark);
        textView.Font = UIFont.SystemFontOfSize(textView.Font?.PointSize ?? 13, UIFontWeight.Medium);
        backdropView.Alpha = 1f;
        backdropView.BackgroundColor = backdropTintColor;
        backdropView.Opaque = false;
        backdropView.Frame = textView.Bounds;
        backdropView.Layer.CornerRadius = 4;
        backdropView.Layer.MasksToBounds = true;
        backdropView.ClipsToBounds = true;
        textView.SendSubviewToBack(backdropView);
        ClearMacGlassTextEditorBackgrounds(textView, backdropView);
        ClearMacGlassTextEditorLayers(textView, backdropView);
        ClearMacGlassTextEditorWrapperBackgrounds(textView);
    }

    private static UIView EnsureMacModalTextEditorGlassBackdrop(UITextView textView)
    {
        foreach (var subview in textView.Subviews)
        {
            if (subview.Tag == MacModalTextEditorGlassBackdropTag)
            {
                return subview;
            }
        }

        var backdropView = new UIView
        {
            Tag = MacModalTextEditorGlassBackdropTag,
            UserInteractionEnabled = false,
            Opaque = false,
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
        };
        textView.InsertSubview(backdropView, 0);
        return backdropView;
    }

    private static void ClearMacGlassTextEditorBackgrounds(UITextView textView, UIView preservedBackdrop)
    {
        foreach (var subview in textView.Subviews)
        {
            ClearMacGlassTextEditorSubviewBackground(subview, preservedBackdrop);
        }
    }

    private static void ClearMacGlassTextEditorSubviewBackground(UIView view, UIView preservedBackdrop)
    {
        if (ReferenceEquals(view, preservedBackdrop))
        {
            return;
        }

        if (view is UIVisualEffectView effectView)
        {
            effectView.Effect = null;
            effectView.ContentView.Opaque = false;
            effectView.ContentView.BackgroundColor = UIColor.Clear;
        }

        view.Opaque = false;
        view.BackgroundColor = UIColor.Clear;
        view.Layer.BackgroundColor = UIColor.Clear.CGColor;
        foreach (var subview in view.Subviews)
        {
            ClearMacGlassTextEditorSubviewBackground(subview, preservedBackdrop);
        }
    }

    private static void ClearMacGlassTextEditorLayers(UITextView textView, UIView preservedBackdrop)
    {
        var layers = textView.Layer.Sublayers;
        if (layers is null)
        {
            return;
        }

        foreach (var layer in layers)
        {
            if (ReferenceEquals(layer, preservedBackdrop.Layer))
            {
                continue;
            }

            layer.BackgroundColor = UIColor.Clear.CGColor;
            layer.ShouldRasterize = false;
        }
    }

    private static void ClearMacGlassTextEditorWrapperBackgrounds(UITextView textView)
    {
        var ancestor = textView.Superview;
        var depth = 0;
        while (ancestor is not null && ancestor is not UIWindow && depth < 4)
        {
            if (IsLikelyMacGlassTextEditorWrapper(textView, ancestor))
            {
                ancestor.Opaque = false;
                ancestor.BackgroundColor = UIColor.Clear;
                ancestor.Layer.BackgroundColor = UIColor.Clear.CGColor;
                ClearMacGlassWrapperLayers(ancestor.Layer);
            }

            ancestor = ancestor.Superview;
            depth++;
        }
    }

    private static bool IsLikelyMacGlassTextEditorWrapper(UITextView textView, UIView view)
    {
        const double widthSlack = 72;
        const double heightSlack = 18;
        return view.Bounds.Width <= textView.Bounds.Width + widthSlack &&
            view.Bounds.Height <= textView.Bounds.Height + heightSlack;
    }

    private static void ClearMacGlassWrapperLayers(CALayer layer)
    {
        var layers = layer.Sublayers;
        if (layers is null)
        {
            return;
        }

        foreach (var sublayer in layers)
        {
            sublayer.BackgroundColor = UIColor.Clear.CGColor;
            sublayer.ShouldRasterize = false;
        }
    }

    private void ApplyMacModalButtonVisualState()
    {
        ApplyMacGlassButtonVisual(CopyGuidButton);
        ApplyMacGlassButtonVisual(CopyButtonTextButton);
        ApplyMacGlassButtonVisual(CopyCommandButton);
        ApplyMacGlassButtonVisual(CopyToolButton);
        ApplyMacGlassButtonVisual(CopyArgumentsButton);
        ApplyMacGlassButtonVisual(CopyClipWordButton);
        ApplyMacGlassButtonVisual(CopyNoteButton);
        ApplyMacGlassButtonVisual(ModalCancelButton);
        ApplyMacGlassButtonVisual(ModalSaveButton);
    }

    private void ScheduleMacModalButtonVisualStateRefresh()
    {
        ApplyMacModalButtonVisualState();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(16), ApplyMacModalButtonVisualState);
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(120), ApplyMacModalButtonVisualState);
    }

    private void ApplyMacGlassButtonVisual(Border button)
    {
        var dark = IsMacGlassDarkThemeActive();
        button.BackgroundColor = dark ? Color.FromArgb("#9A53606B") : Color.FromArgb("#8AFFFFFF");
        button.Stroke = new SolidColorBrush(Colors.Transparent);
        button.StrokeThickness = 0;
    }

    private bool IsMacGlassDarkThemeActive()
    {
        return viewModel.SelectedTheme switch
        {
            ThemeMode.Dark => true,
            ThemeMode.Light => false,
            _ => IsDarkThemeActive(),
        };
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
            ModalInvertThemeCheckBox.Handler?.PlatformView as UIResponder,
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
        catch (Exception ex)
        {
            var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
            CrashFileLogger.WriteWarning(nameof(TryCreateMacEditorKeyCommand), $"Failed to create Mac editor key command '{selectorName}' for input '{keyInput}': {safeMessage}");
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
        ApplyMacCrispTextRendering(view, scale);
        foreach (var subview in view.Subviews)
        {
            ApplyMacContentScaleRecursive(subview, scale);
        }
    }

    private static void ApplyMacCrispTextRendering(UIView view, nfloat scale)
    {
        view.Layer.ShouldRasterize = false;
        view.Layer.RasterizationScale = scale;
        if (view is UILabel label && label.Font is not null && label.Font.PointSize <= 15)
        {
            label.Opaque = false;
            label.ContentScaleFactor = scale;
            label.Layer.ContentsScale = scale;
            label.Layer.ShouldRasterize = false;
            label.Font = UIFont.SystemFontOfSize(label.Font.PointSize, UIFontWeight.Medium);
        }

        if (view is UIButton button && button.TitleLabel is UILabel titleLabel && titleLabel.Font is not null)
        {
            titleLabel.Opaque = false;
            titleLabel.ContentScaleFactor = scale;
            titleLabel.Layer.ContentsScale = scale;
            titleLabel.Layer.ShouldRasterize = false;
            titleLabel.Font = UIFont.SystemFontOfSize(titleLabel.Font.PointSize, UIFontWeight.Medium);
        }

        if (view is UITextField textField && textField.Font is not null)
        {
            textField.Opaque = false;
            textField.ContentScaleFactor = scale;
            textField.Layer.ContentsScale = scale;
            textField.Layer.ShouldRasterize = false;
            textField.Font = UIFont.SystemFontOfSize(textField.Font.PointSize, UIFontWeight.Medium);
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
        macPrimaryButtonWasDown = IsMacMouseButtonCurrentlyDown(CGMouseButton.Left);
        macSecondaryButtonWasDown = IsMacMouseButtonCurrentlyDown(CGMouseButton.Right);
        macMiddleButtonWasDown = IsMacMouseButtonCurrentlyDown(CGMouseButton.Center);
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
        var rawPrimaryDown = IsMacMouseButtonCurrentlyDown(CGMouseButton.Left);
        var primaryDown = rawPrimaryDown;
        var secondaryDown = IsMacMouseButtonCurrentlyDown(CGMouseButton.Right);
        var middleDown = IsMacMouseButtonCurrentlyDown(CGMouseButton.Center);
        if (ShouldLogMacPlacementInputDiagnostic(ref macPlacementPollTickDiagnosticCount, limit: 8, every: 600))
        {
            var pointerText = FormatMacPlacementPointerDiagnostic();
            LogMacPlacementInputDiagnostic(
                "poll",
                $"primary={rawPrimaryDown} effectivePrimary={primaryDown} secondary={secondaryDown} middle={middleDown} wasPrimary={macPrimaryButtonWasDown} wasSecondary={macSecondaryButtonWasDown} pointer={pointerText}");
        }

        if (rawPrimaryDown)
        {
            macPlacementPrimaryReleaseStartedAtUtc = null;
        }

        if (rawPrimaryDown && !macPrimaryButtonWasDown)
        {
            LogMacPlacementInputDiagnostic("poll-primary-down", "transition detected");
            ScheduleMacPlacementPollingSelectionStart();
        }
        else if (rawPrimaryDown && macPlacementPollingSelectionActive)
        {
            ContinueMacPlacementPollingSelection();
        }
        else if (!rawPrimaryDown && macPlacementPollingSelectionActive)
        {
            var now = DateTimeOffset.UtcNow;
            macPlacementPrimaryReleaseStartedAtUtc ??= now;
            if (now - macPlacementPrimaryReleaseStartedAtUtc.Value < MacPlacementPrimaryReleaseGraceWindow)
            {
                primaryDown = true;
                ContinueMacPlacementPollingSelection();
            }
            else if (macPrimaryButtonWasDown)
            {
                CompleteMacPlacementPollingSelection();
            }
        }
        else if (!rawPrimaryDown)
        {
            macPlacementPrimaryReleaseStartedAtUtc = null;
        }

        if (secondaryDown && !macSecondaryButtonWasDown)
        {
            LogMacPlacementInputDiagnostic("poll-secondary-down", "transition detected");
            TryHandleMacPlacementPollingSecondaryCreate();
        }

        if (middleDown && !macMiddleButtonWasDown)
        {
            LogMacPlacementInputDiagnostic("poll-middle-down", "transition detected");
            if (TryGetMacCurrentRootPointer(out var pointer))
            {
                HandleMacMiddleClick(pointer);
            }
        }

        macPrimaryButtonWasDown = primaryDown;
        macSecondaryButtonWasDown = secondaryDown;
        macMiddleButtonWasDown = middleDown;
    }

    private void ScheduleMacPlacementPollingSelectionStart()
    {
        var requestId = ++macPlacementDeferredPrimaryDownId;
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(16), () =>
        {
            if (requestId != macPlacementDeferredPrimaryDownId ||
                !IsMacMouseButtonCurrentlyDown(CGMouseButton.Left) ||
                macPlacementPollingSelectionActive)
            {
                return;
            }

            LogMacPlacementInputDiagnostic("poll-primary-deferred", "starting after pointer settle");
            TryBeginMacPlacementPollingSelection();
        });
    }

    private void TryBeginMacPlacementPollingSelection()
    {
        if (selectionDragging ||
            viewModel.IsEditorOpen ||
            viewModel.IsContextMenuOpen ||
            !TryGetMacPlacementPollingRootPoint(out var rootPoint))
        {
            if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen)
            {
                ClearMacPlacementHoverRootPoint();
            }

            LogMacPlacementInputDiagnostic(
                "poll-selection-reject",
                $"dragging={selectionDragging} editor={viewModel.IsEditorOpen} context={viewModel.IsContextMenuOpen} pointer={FormatMacPlacementPointerDiagnostic()}");
            return;
        }

        if (TryHandleMacPlacementPollingButtonPress(rootPoint))
        {
            LogMacPlacementInputDiagnostic("poll-selection-button-hit", $"root=({rootPoint.X:0.##},{rootPoint.Y:0.##})");
            return;
        }

        if (!TryGetMacPlacementRootCanvasPoint(rootPoint, allowOutside: false, out var canvasPoint, out var viewportPoint) ||
            IsOnAnyVisibleButton(canvasPoint))
        {
            LogMacPlacementInputDiagnostic(
                "poll-selection-hit-reject",
                $"root=({rootPoint.X:0.##},{rootPoint.Y:0.##}) canvas=({canvasPoint.X:0.##},{canvasPoint.Y:0.##}) scroll={PlacementScroll.Width:0.##}x{PlacementScroll.Height:0.##}");
            return;
        }

        var hasRawStartScreen = TryGetMacCurrentScreenPointer(rootPoint, out var rawStartScreen, out var rawStartScreenPointKind);
        var hasAppKitStartRoot = TryGetMacCurrentRootPointerFromAppKit(rootPoint, out _, out var appKitRootPointKind);
        var selectionRootPoint = ApplyMacPlacementSelectionPointerOffset(rootPoint);
        if (!TryGetMacPlacementRootCanvasPoint(selectionRootPoint, allowOutside: false, out var selectionCanvasPoint, out var selectionViewportPoint))
        {
            selectionRootPoint = rootPoint;
            selectionCanvasPoint = canvasPoint;
            selectionViewportPoint = viewportPoint;
        }

        CloseCommandSuggestionPopup();
        macPlacementPollingSelectionActive = true;
        selectionDragging = true;
        selectionPanPrimed = true;
        macPlacementPrimaryReleaseStartedAtUtc = null;
        macPlacementPollingAppKitRootPointKind = hasAppKitStartRoot ? appKitRootPointKind : null;
        macPlacementPollingStartRootPoint = selectionRootPoint;
        macPlacementPollingRawStartScreen = hasRawStartScreen ? rawStartScreen : null;
        macPlacementPollingScreenPointKind = hasRawStartScreen ? rawStartScreenPointKind : null;
        macPlacementPollingAnchorViewport = selectionViewportPoint;
        macPlacementPollingGeometryDiagnosticCount = 0;
        selectionStartCanvas = selectionCanvasPoint;
        selectionStartViewport = selectionViewportPoint;
        selectionLastCanvas = selectionStartCanvas;
        selectionLastViewport = selectionStartViewport;
        SelectionRect.IsVisible = false;
        SetMacPlacementSelectionCursor();
        UpdateMacPlacementNativeSelectionOverlay(selectionRootPoint, selectionRootPoint);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, selectionStartCanvas.X, selectionStartCanvas.Y, GestureStatus.Started));
        LogMacPlacementInputDiagnostic(
            "poll-selection-started",
            $"root=({selectionRootPoint.X:0.##},{selectionRootPoint.Y:0.##}) viewport=({selectionViewportPoint.X:0.##},{selectionViewportPoint.Y:0.##}) canvas=({selectionCanvasPoint.X:0.##},{selectionCanvasPoint.Y:0.##})");
        if (ShouldLogMacPlacementInputDiagnostic(ref macPlacementPollingGeometryDiagnosticCount, limit: 24, every: 0))
        {
            var rawText = hasRawStartScreen
                ? $" raw=({rawStartScreen.X:0.##},{rawStartScreen.Y:0.##}) rawKind={rawStartScreenPointKind}"
                : " raw=(null)";
            var appKitText = hasAppKitStartRoot
                ? $" appKitKind={appKitRootPointKind}"
                : " appKitKind=(null)";
            LogMacPlacementInputDiagnostic(
                "poll-geometry-start",
                $"root=({selectionRootPoint.X:0.##},{selectionRootPoint.Y:0.##}) viewport=({selectionViewportPoint.X:0.##},{selectionViewportPoint.Y:0.##}){rawText}{appKitText} pointer={FormatMacPlacementPointerDiagnostic()}");
        }
    }

    private bool TryHandleMacPlacementPollingButtonPress(Point rootPoint)
    {
        var hit = TryGetPlacementButtonAtRootPoint(rootPoint);
        if (hit is null)
        {
            return false;
        }

        if (IsMacSelectionModifierCurrentlyDown())
        {
            if (suppressTapExecuteForItemId != hit.Id)
            {
                viewModel.ToggleSelection(hit);
                macPlacementPollingCommandSelectionItemId = hit.Id;
            }

            suppressTapExecuteForItemId = hit.Id;
            pointerDragging = false;
            ReleaseCapturedPointer();
        }

        return true;
    }

    private bool TryGetMacPlacementPollingRootPoint(out Point rootPoint)
    {
        if (TryIsMacCurrentPointerInsidePlacementWindow(out var pointerInsidePlacementWindow) &&
            !pointerInsidePlacementWindow)
        {
            rootPoint = default;
            return false;
        }

        if (TryGetMacCurrentPlacementRootPointer(
            out rootPoint,
            preferCached: false,
            allowCachedFallback: false,
            useCachedDistance: false))
        {
            return true;
        }

        if (TryGetRecentMacPlacementHoverRootPoint(out rootPoint))
        {
            lastPointerOnRoot = rootPoint;
            return true;
        }

        return false;
    }

    private bool TryGetRecentMacPlacementHoverRootPoint(out Point rootPoint)
    {
        if (DateTimeOffset.UtcNow - macPlacementHoverRootPointUpdatedAtUtc <= MacPlacementRecentHoverWindow &&
            TryGetMacPlacementHoverRootPoint(out rootPoint))
        {
            return true;
        }

        rootPoint = default;
        return false;
    }

    private void ClearMacPlacementHoverRootPoint()
    {
        macPlacementHoverRootPoint = null;
        macPlacementHoverRootPointUpdatedAtUtc = default;
    }

    private bool TryGetMacPlacementPollingRunningPoint(out Point canvasPoint, out Point viewportPoint, out Point rootPoint)
    {
        rootPoint = default;

        if (macPlacementPollingAppKitRootPointKind is MacAppKitRootPointKind appKitRootPointKind &&
            TryGetMacCurrentRootPointerFromAppKit(
                appKitRootPointKind,
                allowOutsideRoot: true,
                allowOutsideWindow: true,
                out var lockedAppKitRootPoint))
        {
            var selectionRootPoint = ApplyMacPlacementSelectionPointerOffset(lockedAppKitRootPoint);
            if (TryGetMacPlacementRootCanvasPoint(selectionRootPoint, allowOutside: true, out canvasPoint, out viewportPoint))
            {
                rootPoint = selectionRootPoint;
                if (ShouldLogMacPlacementInputDiagnostic(ref macPlacementPollingGeometryDiagnosticCount, limit: 24, every: 0))
                {
                    LogMacPlacementInputDiagnostic(
                        "poll-geometry-run",
                        $"source=appkit kind={appKitRootPointKind} viewport=({viewportPoint.X:0.##},{viewportPoint.Y:0.##}) root=({rootPoint.X:0.##},{rootPoint.Y:0.##})");
                }

                return true;
            }
        }

        if (macPlacementPollingRawStartScreen is Point rawStartScreen &&
            macPlacementPollingScreenPointKind is MacPointerScreenPointKind fallbackScreenPointKind &&
            macPlacementPollingAnchorViewport is Point anchorViewport &&
            TryGetMacCurrentScreenPointer(fallbackScreenPointKind, out var rawScreenPoint))
        {
            viewportPoint = ClampToPlacementViewport(new Point(
                anchorViewport.X + rawScreenPoint.X - rawStartScreen.X,
                anchorViewport.Y + rawScreenPoint.Y - rawStartScreen.Y));
            canvasPoint = new Point(
                viewportPoint.X + PlacementScroll.ScrollX,
                viewportPoint.Y + PlacementScroll.ScrollY);
            var scrollOffset = GetPositionRelativeToAncestor(PlacementScroll, RootGrid);
            rootPoint = new Point(viewportPoint.X + scrollOffset.X, viewportPoint.Y + scrollOffset.Y);
            if (ShouldLogMacPlacementInputDiagnostic(ref macPlacementPollingGeometryDiagnosticCount, limit: 24, every: 0))
            {
                LogMacPlacementInputDiagnostic(
                    "poll-geometry-run",
                    $"source=raw kind={fallbackScreenPointKind} rawStart=({rawStartScreen.X:0.##},{rawStartScreen.Y:0.##}) raw=({rawScreenPoint.X:0.##},{rawScreenPoint.Y:0.##}) anchor=({anchorViewport.X:0.##},{anchorViewport.Y:0.##}) viewport=({viewportPoint.X:0.##},{viewportPoint.Y:0.##}) root=({rootPoint.X:0.##},{rootPoint.Y:0.##})");
            }

            return true;
        }

        if (macPlacementPollingScreenPointKind is MacPointerScreenPointKind screenPointKind &&
            TryGetMacCurrentRootPointer(screenPointKind, allowOutsideRoot: true, out var lockedRootPoint))
        {
            var selectionRootPoint = ApplyMacPlacementSelectionPointerOffset(lockedRootPoint);
            if (TryGetMacPlacementRootCanvasPoint(selectionRootPoint, allowOutside: true, out canvasPoint, out viewportPoint))
            {
                rootPoint = selectionRootPoint;
                if (ShouldLogMacPlacementInputDiagnostic(ref macPlacementPollingGeometryDiagnosticCount, limit: 24, every: 0))
                {
                    LogMacPlacementInputDiagnostic(
                        "poll-geometry-run",
                        $"source=cg kind={screenPointKind} viewport=({viewportPoint.X:0.##},{viewportPoint.Y:0.##}) root=({rootPoint.X:0.##},{rootPoint.Y:0.##})");
                }

                return true;
            }
        }

        if (TryGetMacPlacementPollingCurrentRootPoint(out var currentRootPoint))
        {
            var selectionRootPoint = ApplyMacPlacementSelectionPointerOffset(currentRootPoint);
            if (TryGetMacPlacementRootCanvasPoint(selectionRootPoint, allowOutside: true, out canvasPoint, out viewportPoint))
            {
                rootPoint = selectionRootPoint;
                return true;
            }
        }

        if (TryGetRecentMacPlacementHoverRootPoint(out var recentHoverRootPoint))
        {
            var selectionRootPoint = ApplyMacPlacementSelectionPointerOffset(recentHoverRootPoint);
            if (TryGetMacPlacementRootCanvasPoint(selectionRootPoint, allowOutside: true, out canvasPoint, out viewportPoint))
            {
                rootPoint = selectionRootPoint;
                return true;
            }
        }

        canvasPoint = default;
        viewportPoint = default;
        rootPoint = default;
        return false;
    }

    private void UpdateMacPlacementNativeSelectionOverlay(Point startRootPoint, Point currentRootPoint, bool hide = false)
    {
        if (hide)
        {
            HideMacPlacementNativeSelectionOverlay();
            return;
        }

        if (RootGrid.Handler?.PlatformView is not UIView rootView)
        {
            return;
        }

        var overlay = EnsureMacPlacementNativeSelectionOverlay(rootView);
        if (overlay is null)
        {
            return;
        }

        macPlacementSelectionOverlayFadeRevision++;
        overlay.Layer.RemoveAllAnimations();
        overlay.Alpha = 1;
        ApplyMacPlacementNativeSelectionOverlayColors(overlay);
        var x = Math.Min(startRootPoint.X, currentRootPoint.X);
        var y = Math.Min(startRootPoint.Y, currentRootPoint.Y);
        var w = Math.Abs(currentRootPoint.X - startRootPoint.X);
        var h = Math.Abs(currentRootPoint.Y - startRootPoint.Y);
        overlay.Hidden = w <= 2 || h <= 2;
        overlay.Frame = new CGRect(x, y, w, h);
        rootView.BringSubviewToFront(overlay);
    }

    private UIView? EnsureMacPlacementNativeSelectionOverlay(UIView rootView)
    {
        if (macPlacementSelectionOverlayView is UIView existingOverlay)
        {
            if (ReferenceEquals(existingOverlay.Superview, rootView))
            {
                return existingOverlay;
            }

            existingOverlay.RemoveFromSuperview();
            macPlacementSelectionOverlayView = null;
        }

        var overlay = new UIView(CGRect.Empty)
        {
            UserInteractionEnabled = false,
            Opaque = false,
            Hidden = true,
        };
        overlay.Layer.BorderWidth = 1;
        ApplyMacPlacementNativeSelectionOverlayColors(overlay);
        rootView.AddSubview(overlay);
        macPlacementSelectionOverlayView = overlay;
        return overlay;
    }

    private static void ApplyMacPlacementNativeSelectionOverlayColors(UIView overlay)
    {
        var fillColor = UIColor.FromWhiteAlpha(0.42f, 0.30f);
        var borderColor = UIColor.FromWhiteAlpha(0.45f, 0.85f);
        overlay.BackgroundColor = fillColor;
        overlay.Layer.BackgroundColor = fillColor.CGColor;
        overlay.Layer.BorderColor = borderColor.CGColor;
    }

    private static void SetMacPlacementSelectionCursor()
    {
        if (nsCursorClass == IntPtr.Zero)
        {
            return;
        }

        var cursor = ObjcMsgSendIntPtr(nsCursorClass, arrowCursorSelector);
        if (cursor != IntPtr.Zero)
        {
            ObjcMsgSendVoid(cursor, setCursorSelector);
        }
    }

    private void HideMacPlacementNativeSelectionOverlay(bool fade = false)
    {
        if (macPlacementSelectionOverlayView is UIView overlay)
        {
            var fadeRevision = ++macPlacementSelectionOverlayFadeRevision;
            overlay.Layer.RemoveAllAnimations();
            if (fade && !overlay.Hidden && overlay.Frame.Width > 0 && overlay.Frame.Height > 0)
            {
                UIView.AnimateNotify(
                    UiTimingPolicy.SelectionRectFadeOutDurationMs / 1000d,
                    0,
                    UIViewAnimationOptions.BeginFromCurrentState | UIViewAnimationOptions.CurveEaseOut,
                    () => overlay.Alpha = 0,
                    _ =>
                    {
                        if (macPlacementSelectionOverlayFadeRevision == fadeRevision)
                        {
                            overlay.Hidden = true;
                            overlay.Frame = CGRect.Empty;
                            overlay.Alpha = 1;
                        }
                    });
                return;
            }

            overlay.Hidden = true;
            overlay.Frame = CGRect.Empty;
            overlay.Alpha = 1;
        }
    }

    private bool TryGetMacCurrentRootPointer(MacPointerScreenPointKind screenPointKind, out Point rootPoint)
        => TryGetMacCurrentRootPointer(screenPointKind, allowOutsideRoot: false, out rootPoint);

    private bool TryGetMacCurrentRootPointer(
        MacPointerScreenPointKind screenPointKind,
        bool allowOutsideRoot,
        out Point rootPoint)
    {
        rootPoint = default;

        if (RootGrid.Handler?.PlatformView is not UIView rootView ||
            rootView.Window is not UIWindow nativeWindow)
        {
            return false;
        }

        try
        {
            using var currentEvent = new CGEvent((CGEventSource?)null);
            if (!TryGetMacPointerScreenPointCandidate(currentEvent, screenPointKind, out var screenPoint))
            {
                return false;
            }

            var windowPoint = nativeWindow.ConvertPointFromWindow(screenPoint, null);
            var localPoint = rootView.ConvertPointFromView(windowPoint, nativeWindow);
            var candidate = new Point(localPoint.X, localPoint.Y);
            if (!allowOutsideRoot && !IsMacRootPointInsideRoot(candidate))
            {
                return false;
            }

            rootPoint = candidate;
            if (IsMacRootPointInsideRoot(rootPoint))
            {
                lastPointerOnRoot = rootPoint;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryGetMacPlacementPollingCurrentRootPoint(out Point rootPoint)
    {
        if (TryGetMacCurrentRootPointerFromAppKit(out rootPoint, useCachedDistance: true) ||
            TryGetMacCurrentRootPointerFromCoreGraphics(out rootPoint, useCachedDistance: true))
        {
            lastPointerOnRoot = rootPoint;
            return true;
        }

        rootPoint = default;
        return false;
    }

    private void ContinueMacPlacementPollingSelection()
    {
        if (!selectionDragging)
        {
            macPlacementPollingSelectionActive = false;
            macPlacementPrimaryReleaseStartedAtUtc = null;
            HideMacPlacementNativeSelectionOverlay();
            return;
        }

        if (viewModel.IsEditorOpen || viewModel.IsContextMenuOpen)
        {
            CancelMacPlacementNativeSelection();
            macPlacementPollingSelectionActive = false;
            macPlacementPrimaryReleaseStartedAtUtc = null;
            HideMacPlacementNativeSelectionOverlay();
            ResetMacPlacementPollingSelectionTracking();
            return;
        }

        if (!TryGetMacPlacementPollingRunningPoint(out var canvasPoint, out var viewportPoint, out var rootPoint))
        {
            return;
        }

        selectionLastCanvas = canvasPoint;
        selectionLastViewport = viewportPoint;
        SetMacPlacementSelectionCursor();
        UpdateMacPlacementNativeSelectionOverlay(macPlacementPollingStartRootPoint ?? rootPoint, rootPoint);

        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, canvasPoint.X, canvasPoint.Y, GestureStatus.Running));
    }

    private void CompleteMacPlacementPollingSelection()
    {
        if (!selectionDragging)
        {
            macPlacementPollingSelectionActive = false;
            macPlacementPrimaryReleaseStartedAtUtc = null;
            HideMacPlacementNativeSelectionOverlay();
            ResetMacPlacementPollingSelectionTracking();
            return;
        }

        if (TryGetMacPlacementPollingRunningPoint(out var canvasPoint, out var viewportPoint, out _))
        {
            selectionLastCanvas = canvasPoint;
            selectionLastViewport = viewportPoint;
        }

        selectionDragging = false;
        selectionPanPrimed = false;
        macPlacementPollingSelectionActive = false;
        macPlacementPrimaryReleaseStartedAtUtc = null;
        ResetMacPlacementPollingSelectionTracking();
        UpdateSelectionRect(selectionStartViewport, selectionLastViewport, hide: true);
        HideMacPlacementNativeSelectionOverlay(fade: true);
        ExecuteSelectionPayload(new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, selectionLastCanvas.X, selectionLastCanvas.Y, GestureStatus.Completed));
    }

    private void ResetMacPlacementPollingSelectionTracking()
    {
        macPlacementPollingAppKitRootPointKind = null;
        macPlacementPollingStartRootPoint = null;
        macPlacementPollingRawStartScreen = null;
        macPlacementPollingScreenPointKind = null;
        macPlacementPollingAnchorViewport = null;
    }

    private void TryHandleMacPlacementPollingSecondaryCreate()
    {
        if (viewModel.IsEditorOpen ||
            viewModel.IsContextMenuOpen ||
            !TryGetMacCurrentPlacementRootPointer(out var rootPoint))
        {
            LogMacPlacementInputDiagnostic(
                "poll-secondary-reject",
                $"editor={viewModel.IsEditorOpen} context={viewModel.IsContextMenuOpen} pointer={FormatMacPlacementPointerDiagnostic()}");
            return;
        }

        if (!TryGetMacPlacementRootCanvasPoint(rootPoint, allowOutside: false, out var canvasPoint, out _) ||
            IsOnAnyVisibleButton(canvasPoint))
        {
            LogMacPlacementInputDiagnostic(
                "poll-secondary-hit-reject",
                $"root=({rootPoint.X:0.##},{rootPoint.Y:0.##}) canvas=({canvasPoint.X:0.##},{canvasPoint.Y:0.##})");
            return;
        }

        CloseCommandSuggestionPopup();
        ClearMacPlacementHoverRootPoint();
        _ = OpenCreateEditorFromCanvasPointWithWarningAsync(canvasPoint, nameof(TryHandleMacPlacementPollingSecondaryCreate));
        LogMacPlacementInputDiagnostic("poll-secondary-create", $"canvas=({canvasPoint.X:0.##},{canvasPoint.Y:0.##})");
    }

    private bool TryGetMacPlacementRootCanvasPoint(Point rootPoint, bool allowOutside, out Point canvasPoint, out Point viewportPoint)
    {
        canvasPoint = default;
        viewportPoint = default;

        if (PlacementScroll.Width <= 0 || PlacementScroll.Height <= 0)
        {
            return false;
        }

        var offset = GetPositionRelativeToAncestor(PlacementScroll, RootGrid);
        var local = new Point(rootPoint.X - offset.X, rootPoint.Y - offset.Y);
        if (!allowOutside &&
            (local.X < 0 || local.Y < 0 || local.X > PlacementScroll.Width || local.Y > PlacementScroll.Height))
        {
            return false;
        }

        viewportPoint = ClampToPlacementViewport(local);
        canvasPoint = new Point(
            viewportPoint.X + PlacementScroll.ScrollX,
            viewportPoint.Y + PlacementScroll.ScrollY);
        return true;
    }

    private static Point ApplyMacPlacementSelectionPointerOffset(Point rootPoint)
        => new(rootPoint.X + MacPlacementSelectionPointerOffset.X, rootPoint.Y + MacPlacementSelectionPointerOffset.Y);

    private bool TryGetMacCurrentPlacementRootPointer(
        out Point rootPoint,
        bool preferCached = true,
        bool allowCachedFallback = true,
        bool useCachedDistance = true)
    {
        if (TryIsMacCurrentPointerInsidePlacementWindow(out var pointerInsidePlacementWindow) &&
            !pointerInsidePlacementWindow)
        {
            rootPoint = default;
            return false;
        }

        if (TryGetMacCurrentRootPointerFromAppKit(out rootPoint, useCachedDistance))
        {
            lastPointerOnRoot = rootPoint;
            return true;
        }

        if (TryGetMacCurrentRootPointerFromCoreGraphics(out rootPoint, useCachedDistance))
        {
            lastPointerOnRoot = rootPoint;
            return true;
        }

        if (TryGetMacPlacementHoverRootPoint(out rootPoint))
        {
            lastPointerOnRoot = rootPoint;
            return true;
        }

        if (allowCachedFallback && preferCached && lastPointerOnRoot is Point fallbackRootPoint)
        {
            rootPoint = fallbackRootPoint;
            return true;
        }

        if (allowCachedFallback && !preferCached && lastPointerOnRoot is Point uncachedFallbackRootPoint)
        {
            rootPoint = uncachedFallbackRootPoint;
            return true;
        }

        rootPoint = default;
        return false;
    }

    private bool TryGetMacPlacementHoverRootPoint(out Point rootPoint)
    {
        if (macPlacementHoverRootPoint is Point cachedHoverPoint && IsMacRootPointInsideRoot(cachedHoverPoint))
        {
            rootPoint = cachedHoverPoint;
            return true;
        }

        if (macPlacementHoverRecognizer is not null &&
            macPlacementGestureNativeView is not null &&
            (macPlacementHoverRecognizer.State == UIGestureRecognizerState.Began ||
             macPlacementHoverRecognizer.State == UIGestureRecognizerState.Changed))
        {
            var location = macPlacementHoverRecognizer.LocationInView(macPlacementGestureNativeView);
            var liveHoverPoint = new Point(location.X, location.Y);
            if (IsMacRootPointInsideRoot(liveHoverPoint))
            {
                macPlacementHoverRootPoint = liveHoverPoint;
                rootPoint = liveHoverPoint;
                return true;
            }
        }

        rootPoint = default;
        return false;
    }

    private string FormatMacPlacementPointerDiagnostic()
    {
        var pointerText = lastPointerOnRoot is Point pointer
            ? $"last=({pointer.X:0.##},{pointer.Y:0.##})"
            : "last=(null)";
        var hoverText = macPlacementHoverRootPoint is Point hover
            ? $"hover=({hover.X:0.##},{hover.Y:0.##})"
            : "hover=(null)";
        return $"{pointerText} {hoverText}";
    }

    private bool TryGetMacCurrentRootPointer(out Point rootPoint, bool preferCached = true)
    {
        if (preferCached && lastPointerOnRoot is Point cachedRootPoint && IsMacRootPointInsideRoot(cachedRootPoint))
        {
            rootPoint = cachedRootPoint;
            return true;
        }

        if (TryGetMacCurrentRootPointerFromCoreGraphics(out rootPoint))
        {
            lastPointerOnRoot = rootPoint;
            return true;
        }

        if (TryGetMacCurrentRootPointerFromAppKit(out rootPoint))
        {
            lastPointerOnRoot = rootPoint;
            return true;
        }

        if (lastPointerOnRoot is Point fallbackRootPoint)
        {
            rootPoint = fallbackRootPoint;
            return true;
        }

        rootPoint = default;
        return false;
    }

    private bool IsMacRootPointInsideRoot(Point rootPoint)
    {
        return RootGrid.Width > 0 &&
            RootGrid.Height > 0 &&
            rootPoint.X >= 0 &&
            rootPoint.Y >= 0 &&
            rootPoint.X <= RootGrid.Width &&
            rootPoint.Y <= RootGrid.Height;
    }

    private bool TryGetMacCurrentRootPointerFromCoreGraphics(out Point rootPoint, bool useCachedDistance = true)
    {
        rootPoint = default;

        if (TryIsMacCurrentPointerInsidePlacementWindow(out var pointerInsidePlacementWindow) &&
            !pointerInsidePlacementWindow)
        {
            return false;
        }

        if (RootGrid.Handler?.PlatformView is not UIView rootView ||
            rootView.Window is not UIWindow nativeWindow)
        {
            return false;
        }

        try
        {
            var cachedRootPoint = default(Point);
            var hasCachedRootPoint = false;
            if (useCachedDistance &&
                lastPointerOnRoot is Point currentCachedRootPoint &&
                IsMacRootPointInsideRoot(currentCachedRootPoint))
            {
                cachedRootPoint = currentCachedRootPoint;
                hasCachedRootPoint = true;
            }

            Point? bestRootPoint = null;
            var bestDistance = double.MaxValue;
            using var currentEvent = new CGEvent((CGEventSource?)null);
            foreach (var screenPoint in EnumerateMacPointerScreenPointCandidates(currentEvent))
            {
                var windowPoint = nativeWindow.ConvertPointFromWindow(screenPoint, null);
                var localPoint = rootView.ConvertPointFromView(windowPoint, nativeWindow);
                if (localPoint.X < 0 ||
                    localPoint.Y < 0 ||
                    localPoint.X > RootGrid.Width ||
                    localPoint.Y > RootGrid.Height)
                {
                    continue;
                }

                var candidate = new Point(localPoint.X, localPoint.Y);
                if (!hasCachedRootPoint)
                {
                    rootPoint = candidate;
                    return true;
                }

                var distance = Math.Pow(candidate.X - cachedRootPoint.X, 2) + Math.Pow(candidate.Y - cachedRootPoint.Y, 2);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestRootPoint = candidate;
                }
            }

            if (bestRootPoint is Point nearestRootPoint)
            {
                rootPoint = nearestRootPoint;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool TryGetMacCurrentRootPointerFromAppKit(out Point rootPoint, bool useCachedDistance = true)
    {
        rootPoint = default;

        if (RootGrid.Handler?.PlatformView is not UIView rootView ||
            rootView.Window is not UIWindow nativeWindow)
        {
            return false;
        }

        try
        {
            var nsEventClass = ObjcGetClass("NSEvent");
            if (nsEventClass == IntPtr.Zero ||
                TryGetMacPlacementNativeWindow(nativeWindow) is not NSObject nsWindow)
            {
                return false;
            }

            var screenPoint = ObjcMsgSendCGPoint(nsEventClass, SelRegisterName("mouseLocation"));
            if (!MacAppKitWindowContainsScreenPoint(nsWindow, screenPoint))
            {
                return false;
            }

            var windowPoint = ObjcMsgSendCGPointCGPoint(nsWindow.Handle, SelRegisterName("convertPointFromScreen:"), screenPoint);

            var contentFrame = GetMacNativeFrame(nsWindow.Handle, "contentView", fallbackHeight: RootGrid.Height);
            var rootFrame = rootView.Frame;
            var nativeWindowHeight = nativeWindow.Bounds.Height > 0 ? nativeWindow.Bounds.Height : RootGrid.Height;

            Point? bestRootPoint = null;
            var bestDistance = double.MaxValue;
            var cachedRootPoint = default(Point);
            var hasCachedRootPoint = useCachedDistance && TryGetMacCachedRootPointForDistance(out cachedRootPoint);

            foreach (var candidate in EnumerateMacAppKitRootPointCandidatesWithKinds(windowPoint, contentFrame, rootFrame, nativeWindowHeight))
            {
                if (!IsMacRootPointInsideRoot(candidate.Point))
                {
                    continue;
                }

                if (!hasCachedRootPoint)
                {
                    rootPoint = candidate.Point;
                    return true;
                }

                var distance = Math.Pow(candidate.Point.X - cachedRootPoint.X, 2) + Math.Pow(candidate.Point.Y - cachedRootPoint.Y, 2);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestRootPoint = candidate.Point;
                }
            }

            if (bestRootPoint is Point nearestRootPoint)
            {
                rootPoint = nearestRootPoint;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool TryGetMacCurrentRootPointerFromAppKit(
        Point referenceRootPoint,
        out Point rootPoint,
        out MacAppKitRootPointKind rootPointKind)
    {
        rootPoint = default;
        rootPointKind = default;

        if (RootGrid.Handler?.PlatformView is not UIView rootView ||
            rootView.Window is not UIWindow nativeWindow)
        {
            return false;
        }

        try
        {
            if (!TryGetMacCurrentAppKitWindowPoint(
                nativeWindow,
                rootView,
                allowOutsideWindow: false,
                out var windowPoint,
                out var contentFrame,
                out var rootFrame,
                out var nativeWindowHeight))
            {
                return false;
            }

            var hasBest = false;
            var bestDistance = double.MaxValue;
            var bestRootPoint = default(Point);
            var bestRootPointKind = default(MacAppKitRootPointKind);
            foreach (var candidate in EnumerateMacAppKitRootPointCandidatesWithKinds(windowPoint, contentFrame, rootFrame, nativeWindowHeight))
            {
                if (!IsMacRootPointInsideRoot(candidate.Point))
                {
                    continue;
                }

                var distance = Math.Pow(candidate.Point.X - referenceRootPoint.X, 2) + Math.Pow(candidate.Point.Y - referenceRootPoint.Y, 2);
                if (distance < bestDistance)
                {
                    hasBest = true;
                    bestDistance = distance;
                    bestRootPoint = candidate.Point;
                    bestRootPointKind = candidate.Kind;
                }
            }

            if (!hasBest)
            {
                return false;
            }

            rootPoint = bestRootPoint;
            rootPointKind = bestRootPointKind;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryGetMacCurrentRootPointerFromAppKit(
        MacAppKitRootPointKind rootPointKind,
        bool allowOutsideRoot,
        bool allowOutsideWindow,
        out Point rootPoint)
    {
        rootPoint = default;

        if (RootGrid.Handler?.PlatformView is not UIView rootView ||
            rootView.Window is not UIWindow nativeWindow)
        {
            return false;
        }

        try
        {
            if (!TryGetMacCurrentAppKitWindowPoint(
                nativeWindow,
                rootView,
                allowOutsideWindow,
                out var windowPoint,
                out var contentFrame,
                out var rootFrame,
                out var nativeWindowHeight))
            {
                return false;
            }

            foreach (var candidate in EnumerateMacAppKitRootPointCandidatesWithKinds(windowPoint, contentFrame, rootFrame, nativeWindowHeight))
            {
                if (candidate.Kind != rootPointKind ||
                    (!allowOutsideRoot && !IsMacRootPointInsideRoot(candidate.Point)))
                {
                    continue;
                }

                rootPoint = candidate.Point;
                if (IsMacRootPointInsideRoot(rootPoint))
                {
                    lastPointerOnRoot = rootPoint;
                }

                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool TryGetMacCurrentAppKitWindowPoint(
        UIWindow nativeWindow,
        UIView rootView,
        bool allowOutsideWindow,
        out CGPoint windowPoint,
        out CGRect contentFrame,
        out CGRect rootFrame,
        out double nativeWindowHeight)
    {
        windowPoint = default;
        contentFrame = default;
        rootFrame = default;
        nativeWindowHeight = default;

        var nsEventClass = ObjcGetClass("NSEvent");
        if (nsEventClass == IntPtr.Zero ||
            TryGetMacPlacementNativeWindow(nativeWindow) is not NSObject nsWindow)
        {
            return false;
        }

        var screenPoint = ObjcMsgSendCGPoint(nsEventClass, SelRegisterName("mouseLocation"));
        if (!allowOutsideWindow &&
            !MacAppKitWindowContainsScreenPoint(nsWindow, screenPoint))
        {
            return false;
        }

        windowPoint = ObjcMsgSendCGPointCGPoint(nsWindow.Handle, SelRegisterName("convertPointFromScreen:"), screenPoint);
        contentFrame = GetMacNativeFrame(nsWindow.Handle, "contentView", fallbackHeight: RootGrid.Height);
        rootFrame = rootView.Frame;
        nativeWindowHeight = nativeWindow.Bounds.Height > 0 ? nativeWindow.Bounds.Height : RootGrid.Height;
        return true;
    }

    private bool TryIsMacCurrentPointerInsidePlacementWindow(out bool isInside)
    {
        isInside = false;

        if (RootGrid.Handler?.PlatformView is not UIView rootView ||
            rootView.Window is not UIWindow nativeWindow)
        {
            return false;
        }

        try
        {
            var nsEventClass = ObjcGetClass("NSEvent");
            if (nsEventClass == IntPtr.Zero ||
                TryGetMacPlacementNativeWindow(nativeWindow) is not NSObject nsWindow)
            {
                return false;
            }

            var screenPoint = ObjcMsgSendCGPoint(nsEventClass, SelRegisterName("mouseLocation"));
            isInside = MacAppKitWindowContainsScreenPoint(nsWindow, screenPoint);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool MacAppKitWindowContainsScreenPoint(NSObject nsWindow, CGPoint screenPoint)
    {
        try
        {
            var frame = ObjcMsgSendCGRect(nsWindow.Handle, SelRegisterName("frame"));
            return frame.Width > 0 &&
                frame.Height > 0 &&
                screenPoint.X >= frame.X &&
                screenPoint.X <= frame.X + frame.Width &&
                screenPoint.Y >= frame.Y &&
                screenPoint.Y <= frame.Y + frame.Height;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<(MacAppKitRootPointKind Kind, Point Point)> EnumerateMacAppKitRootPointCandidatesWithKinds(CGPoint windowPoint, CGRect contentFrame, CGRect rootFrame, double nativeWindowHeight)
    {
        var contentHeight = contentFrame.Height > 0 ? contentFrame.Height : nativeWindowHeight;
        var contentWidth = contentFrame.Width > 0 ? contentFrame.Width : rootFrame.Width;
        var rootHeight = rootFrame.Height > 0 ? rootFrame.Height : contentHeight;
        var rootWidth = rootFrame.Width > 0 ? rootFrame.Width : contentWidth;
        var contentX = windowPoint.X - contentFrame.X;
        var contentY = contentHeight - (windowPoint.Y - contentFrame.Y);

        if (contentWidth > 0 && contentHeight > 0 && rootWidth > 0 && rootHeight > 0)
        {
            var contentScaleX = rootWidth / contentWidth;
            var contentScaleY = rootHeight / contentHeight;
            if (contentScaleX > 0 &&
                contentScaleY > 0 &&
                (Math.Abs(contentScaleX - 1) > 0.01 || Math.Abs(contentScaleY - 1) > 0.01))
            {
                yield return (MacAppKitRootPointKind.ScaledContentViewFlipped, new Point(
                    contentX * contentScaleX,
                    contentY * contentScaleY));
            }
        }

        yield return (MacAppKitRootPointKind.ContentViewFlipped, new Point(
            contentX,
            contentY));

        yield return (MacAppKitRootPointKind.RootFrameFlipped, new Point(
            windowPoint.X - rootFrame.X,
            rootHeight - (windowPoint.Y - rootFrame.Y)));

        yield return (MacAppKitRootPointKind.NativeWindowFlipped, new Point(windowPoint.X, nativeWindowHeight - windowPoint.Y));
        yield return (MacAppKitRootPointKind.ContentHeightFlipped, new Point(windowPoint.X, contentHeight - windowPoint.Y));
        yield return (MacAppKitRootPointKind.Window, new Point(windowPoint.X, windowPoint.Y));
    }

    private bool TryGetMacCachedRootPointForDistance(out Point rootPoint)
    {
        if (lastPointerOnRoot is Point cachedRootPoint && IsMacRootPointInsideRoot(cachedRootPoint))
        {
            rootPoint = cachedRootPoint;
            return true;
        }

        if (macPlacementHoverRootPoint is Point cachedHoverPoint && IsMacRootPointInsideRoot(cachedHoverPoint))
        {
            rootPoint = cachedHoverPoint;
            return true;
        }

        rootPoint = default;
        return false;
    }

    private static CGRect GetMacNativeFrame(IntPtr receiver, string nestedSelectorName, double fallbackHeight)
    {
        try
        {
            var nested = ObjcMsgSendIntPtr(receiver, SelRegisterName(nestedSelectorName));
            if (nested != IntPtr.Zero)
            {
                var frame = ObjcMsgSendCGRect(nested, SelRegisterName("frame"));
                if (frame.Height > 0)
                {
                    return frame;
                }
            }
        }
        catch
        {
        }

        return new CGRect(0, 0, 0, fallbackHeight);
    }

    private static NSObject? TryGetMacPlacementNativeWindow(UIWindow nativeWindow)
    {
        try
        {
            var applicationClass = Runtime.GetNSObject(Class.GetHandle("NSApplication"));
            if (applicationClass is null)
            {
                return null;
            }

            if (applicationClass.ValueForKeyPath(new NSString("sharedApplication.windows")) is not NSArray nsWindows)
            {
                return null;
            }

            foreach (var candidate in nsWindows)
            {
                if (candidate is NSObject nsWindow && MacPlacementWindowMatchesUiWindow(nsWindow, nativeWindow))
                {
                    return nsWindow;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool MacPlacementWindowMatchesUiWindow(NSObject nsWindow, UIWindow nativeWindow)
    {
        try
        {
            if (nsWindow.ValueForKey(new NSString("uiWindows")) is not NSArray uiWindows)
            {
                return false;
            }

            foreach (var candidate in uiWindows)
            {
                if (candidate is UIWindow uiWindow && ReferenceEquals(uiWindow, nativeWindow))
                {
                    return true;
                }
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool TryGetMacCurrentScreenPointer(out Point screenPoint)
    {
        try
        {
            using var currentEvent = new CGEvent((CGEventSource?)null);
            var location = currentEvent.Location;
            screenPoint = new Point(location.X, location.Y);
            return true;
        }
        catch
        {
            screenPoint = default;
            return false;
        }
    }

    private bool TryGetMacCurrentScreenPointer(
        Point rootPoint,
        out Point screenPoint,
        out MacPointerScreenPointKind screenPointKind)
    {
        screenPoint = default;
        screenPointKind = default;

        if (RootGrid.Handler?.PlatformView is not UIView rootView ||
            rootView.Window is not UIWindow nativeWindow)
        {
            return TryGetFallbackMacCurrentScreenPointer(out screenPoint, out screenPointKind);
        }

        try
        {
            var hasBest = false;
            var bestDistance = double.MaxValue;
            var bestPoint = default(CGPoint);
            var bestKind = default(MacPointerScreenPointKind);
            using var currentEvent = new CGEvent((CGEventSource?)null);
            foreach (var candidate in EnumerateMacPointerScreenPointCandidatesWithKinds(currentEvent))
            {
                var windowPoint = nativeWindow.ConvertPointFromWindow(candidate.Point, null);
                var localPoint = rootView.ConvertPointFromView(windowPoint, nativeWindow);
                var candidateRoot = new Point(localPoint.X, localPoint.Y);
                if (!IsMacRootPointInsideRoot(candidateRoot))
                {
                    continue;
                }

                var distance = Math.Pow(candidateRoot.X - rootPoint.X, 2) + Math.Pow(candidateRoot.Y - rootPoint.Y, 2);
                if (distance < bestDistance)
                {
                    hasBest = true;
                    bestDistance = distance;
                    bestPoint = candidate.Point;
                    bestKind = candidate.Kind;
                }
            }

            if (hasBest)
            {
                screenPoint = new Point(bestPoint.X, bestPoint.Y);
                screenPointKind = bestKind;
                return true;
            }
        }
        catch
        {
        }

        return TryGetFallbackMacCurrentScreenPointer(out screenPoint, out screenPointKind);
    }

    private static bool TryGetMacCurrentScreenPointer(MacPointerScreenPointKind screenPointKind, out Point screenPoint)
    {
        try
        {
            using var currentEvent = new CGEvent((CGEventSource?)null);
            if (TryGetMacPointerScreenPointCandidate(currentEvent, screenPointKind, out var point))
            {
                screenPoint = new Point(point.X, point.Y);
                return true;
            }
        }
        catch
        {
        }

        screenPoint = default;
        return false;
    }

    private static bool TryGetFallbackMacCurrentScreenPointer(out Point screenPoint, out MacPointerScreenPointKind screenPointKind)
    {
        if (TryGetMacCurrentScreenPointer(out screenPoint))
        {
            screenPointKind = MacPointerScreenPointKind.Location;
            return true;
        }

        screenPointKind = default;
        return false;
    }

    private static bool IsMacSelectionModifierCurrentlyDown()
    {
        try
        {
            if (HasMacSelectionModifierFlags(CGEventSource.GetFlagsState(CGEventSourceStateID.HidSystem)))
            {
                return true;
            }
        }
        catch
        {
        }

        try
        {
            return HasMacSelectionModifierFlags(CGEventSource.GetFlagsState(CGEventSourceStateID.CombinedSession));
        }
        catch
        {
            return false;
        }
    }

    private static bool HasMacSelectionModifierFlags(CGEventFlags flags)
    {
        var modifierText = flags.ToString();
        return modifierText.Contains("Command", StringComparison.OrdinalIgnoreCase) ||
            modifierText.Contains("Control", StringComparison.OrdinalIgnoreCase) ||
            modifierText.Contains("Meta", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<CGPoint> EnumerateMacPointerScreenPointCandidates(CGEvent currentEvent)
    {
        foreach (var candidate in EnumerateMacPointerScreenPointCandidatesWithKinds(currentEvent))
        {
            yield return candidate.Point;
        }
    }

    private static IEnumerable<(MacPointerScreenPointKind Kind, CGPoint Point)> EnumerateMacPointerScreenPointCandidatesWithKinds(CGEvent currentEvent)
    {
        var location = currentEvent.Location;
        yield return (MacPointerScreenPointKind.Location, location);

        var unflipped = currentEvent.UnflippedLocation;
        if (!MacPointEquals(unflipped, location))
        {
            yield return (MacPointerScreenPointKind.UnflippedLocation, unflipped);
        }

        var scale = UIScreen.MainScreen.Scale;
        if (scale > 1)
        {
            yield return (MacPointerScreenPointKind.ScaledLocation, new CGPoint(location.X / scale, location.Y / scale));
            if (!MacPointEquals(unflipped, location))
            {
                yield return (MacPointerScreenPointKind.ScaledUnflippedLocation, new CGPoint(unflipped.X / scale, unflipped.Y / scale));
            }
        }
    }

    private static bool TryGetMacPointerScreenPointCandidate(
        CGEvent currentEvent,
        MacPointerScreenPointKind screenPointKind,
        out CGPoint point)
    {
        var location = currentEvent.Location;
        var unflipped = currentEvent.UnflippedLocation;
        var scale = UIScreen.MainScreen.Scale;
        switch (screenPointKind)
        {
            case MacPointerScreenPointKind.Location:
                point = location;
                return true;
            case MacPointerScreenPointKind.UnflippedLocation:
                point = unflipped;
                return true;
            case MacPointerScreenPointKind.ScaledLocation when scale > 0:
                point = new CGPoint(location.X / scale, location.Y / scale);
                return true;
            case MacPointerScreenPointKind.ScaledUnflippedLocation when scale > 0:
                point = new CGPoint(unflipped.X / scale, unflipped.Y / scale);
                return true;
            default:
                point = default;
                return false;
        }
    }

    private static bool MacPointEquals(CGPoint left, CGPoint right)
        => Math.Abs(left.X - right.X) < 0.01 && Math.Abs(left.Y - right.Y) < 0.01;

    private static bool IsMacMouseButtonCurrentlyDown(CGMouseButton button)
    {
        Exception? hidFailure = null;
        try
        {
            if (CGEventSource.GetButtonState(CGEventSourceStateID.HidSystem, button))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            hidFailure = ex;
        }

        try
        {
            if (CGEventSource.GetButtonState(CGEventSourceStateID.CombinedSession, button))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            LogMacMouseButtonQueryFailure(button, ex);
            return false;
        }

        if (hidFailure is not null)
        {
            LogMacMouseButtonQueryFailure(button, hidFailure);
        }

        return false;
    }

    private static void LogMacMouseButtonQueryFailure(CGMouseButton button, Exception ex)
    {
        var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);
        var isActive = App.IsMacApplicationActive();
        var activationSuppressed = App.IsActivationSuppressionActive();
        var pointerKnown = macLastActivePage is not null &&
            macLastActivePage.TryGetTarget(out var page) &&
            (page.lastPointerOnRoot is not null || page.macPlacementHoverRootPoint is not null);
        CrashFileLogger.WriteWarning(nameof(IsMacMouseButtonCurrentlyDown), $"Failed to query mouse button state from CoreGraphics for button={button} while isActive={isActive} activationSuppressed={activationSuppressed} pointerKnown={pointerKnown}: {safeMessage}");
    }

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern CGPoint ObjcMsgSendCGPoint(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern CGPoint ObjcMsgSendCGPointCGPoint(IntPtr receiver, IntPtr selector, CGPoint point);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern CGRect ObjcMsgSendCGRect(IntPtr receiver, IntPtr selector);

    private static bool ShouldLogMacPlacementInputDiagnostic(ref int counter, int limit, int every)
    {
        counter++;
        return counter <= limit || (every > 0 && counter % every == 0);
    }

    [Conditional("DEBUG")]
    private static void LogMacPlacementInputDiagnostic(string stage, string detail)
    {
        CrashFileLogger.WriteInfo("MacPlacementInput", $"{stage}: {detail}");
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
        ModalInvertThemeCheckBox.Unfocus();

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

        if (ModalInvertThemeCheckBox.Handler?.PlatformView is UIResponder invertThemeCheckBox &&
            invertThemeCheckBox.IsFirstResponder)
        {
            invertThemeCheckBox.ResignFirstResponder();
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
}
