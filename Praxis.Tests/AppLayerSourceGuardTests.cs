namespace Praxis.Tests;

public class AppLayerSourceGuardTests
{
    [Fact]
    public void MainPage_XamlLoadFailure_IsCrashLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        Assert.Contains("CrashFileLogger.WriteException(\"MainPage.InitializeComponent\", ex);", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("new Label { Text = safeMessage },", source);
    }

    [Fact]
    public void MainPage_InitializationFailure_ResetsInitializedAndCrashLogs()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");

        Assert.Contains("CrashFileLogger.WriteException(\"MainPage.OnAppearing.InitializeAsync\", ex);", source);
        Assert.Contains("initialized = false;", source);

        var initializeIndex = source.IndexOf("await viewModel.InitializeAsync();", StringComparison.Ordinal);
        var initializedIndex = source.IndexOf("initialized = true;", StringComparison.Ordinal);

        Assert.True(initializeIndex >= 0, "MainPage should await ViewModel initialization.");
        Assert.True(initializedIndex > initializeIndex, "initialized should only flip true after InitializeAsync succeeds.");
    }

    [Fact]
    public void MainPage_InitializationAlertFailure_IsCrashLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("await DisplayAlertAsync(\"Initialization Error\", safeMessage, \"OK\");", source);
        Assert.Contains("CrashFileLogger.WriteException(\"MainPage.OnAppearing.DisplayAlertAsync\", alertEx);", source);
    }

    [Fact]
    public void MainPage_OnDisappearing_DetachesWindowActivationHooks()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        Assert.Contains("DetachWindowActivationHook();", source);
    }

    [Fact]
    public void MainPage_DetachWindowActivationHook_AlsoDetachesMacObservers()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        Assert.Equal(2, CountOccurrences(source, "DetachMacActivationObservers();"));
    }

    [Fact]
    public void MauiClipboardService_HonorsCancellationTokens()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "MauiClipboardService.cs");

        Assert.Equal(2, CountOccurrences(source, "cancellationToken.ThrowIfCancellationRequested();"));
        Assert.Equal(2, CountOccurrences(source, ".WaitAsync(cancellationToken)"));
    }

    [Fact]
    public void HoverHandCursorBehavior_SetsPlatformCursor_OnPointerEnterAndExit()
    {
        var source = ReadRepositoryFile("Praxis", "Behaviors", "HoverHandCursorBehavior.cs");

        Assert.Contains("public sealed class HoverHandCursorBehavior : Behavior<View>", source);
        Assert.Contains("private readonly PointerGestureRecognizer pointer = new();", source);
        Assert.Contains("pointer.PointerEntered += OnPointerEntered;", source);
        Assert.Contains("pointer.PointerExited += OnPointerExited;", source);
        Assert.Contains("SetHandCursor(sender, useHandCursor: true);", source);
        Assert.Contains("SetHandCursor(sender, useHandCursor: false);", source);
        Assert.Contains("NonPublicPropertySetter.TrySet(frameworkElement, \"ProtectedCursor\", cursor);", source);
        Assert.Contains("var cursorSelector = useHandCursor ? pointingHandCursorSelector : arrowCursorSelector;", source);
        Assert.Contains("ObjcMsgSendVoid(cursor, setCursorSelector);", source);
    }

    [Fact]
    public void MainPage_InteractiveButtons_UseHoverHandCursorBehavior()
    {
        var xaml = ReadRepositoryFile("Praxis", "MainPage.xaml");

        Assert.Equal(18, CountOccurrences(xaml, "<behaviors:HoverHandCursorBehavior />"));
        Assert.Contains("<Border x:Name=\"CreateButton\"", xaml);
        Assert.Contains("<behaviors:HoverHandCursorBehavior />\n                                        <behaviors:MiddleClickBehavior", xaml);
        Assert.Contains("<Button x:Name=\"ContextEditButton\"", xaml);
        Assert.Contains("<Button x:Name=\"ContextDeleteButton\"", xaml);
        Assert.Contains("<Border x:Name=\"ContextEditFocusRing\"", xaml);
        Assert.Contains("<Border x:Name=\"ContextDeleteFocusRing\"", xaml);
        Assert.Contains("<Border x:Name=\"CopyClipWordButton\"", xaml);
        Assert.Contains("<Border x:Name=\"CopyNoteButton\"", xaml);
        Assert.Contains("<Border x:Name=\"ModalCancelButton\"", xaml);
        Assert.Contains("<Border x:Name=\"ModalSaveButton\"", xaml);
        Assert.Contains("<CheckBox x:Name=\"ModalInvertThemeCheckBox\"", xaml);
        Assert.Contains("<Label Text=\"Use opposite theme colors for this button\"", xaml);
        Assert.Contains("<Button x:Name=\"ConflictReloadButton\"", xaml);
        Assert.Contains("<Button x:Name=\"ConflictOverwriteButton\"", xaml);
        Assert.Contains("<Button x:Name=\"ConflictCancelButton\"", xaml);
    }

    [Fact]
    public void GrabHandCursorBehavior_SetsPlatformGrabCursor_WhilePointerIsPressed()
    {
        var source = ReadRepositoryFile("Praxis", "Behaviors", "GrabHandCursorBehavior.cs");

        Assert.Contains("public sealed class GrabHandCursorBehavior : Behavior<View>", source);
        Assert.Contains("private static readonly object activeGrabLock = new();", source);
        Assert.Contains("private static GrabHandCursorBehavior? activeGrabBehavior;", source);
        Assert.Contains("private readonly PointerGestureRecognizer pointer = new();", source);
        Assert.Contains("private bool isGrabbing;", source);
        Assert.Contains("pointer.PointerPressed += OnPointerPressed;", source);
        Assert.Contains("pointer.PointerReleased += OnPointerReleased;", source);
        Assert.Contains("pointer.PointerMoved += OnPointerMoved;", source);
        Assert.Contains("pointer.PointerEntered += OnPointerEntered;", source);
        Assert.Contains("pointer.PointerExited += OnPointerExited;", source);
        Assert.Contains("private static void SetActiveGrab(GrabHandCursorBehavior behavior, object? sender)", source);
        Assert.Contains("internal static void ClearActiveGrab()", source);
        Assert.Contains("private void OnPointerReleased(object? sender, PointerEventArgs e)", source);
        Assert.Contains("ClearActiveGrab();", source);
        Assert.Contains("private void OnPointerExited(object? sender, PointerEventArgs e)", source);
        Assert.Contains("NonPublicPropertySetter.TrySet(frameworkElement, \"ProtectedCursor\", cursor);", source);
        Assert.Contains("Microsoft.UI.Input.InputSystemCursorShape.SizeAll", source);
        Assert.Contains("var cursorSelector = useGrabCursor ? closedHandCursorSelector : arrowCursorSelector;", source);
        Assert.Contains("ObjcMsgSendVoid(cursor, setCursorSelector);", source);
    }

    [Fact]
    public void GrabHandCursorBehavior_IgnoresSecondaryAndMiddlePress_AndClearsGrabOnMoveWhenPrimaryReleased()
    {
        var source = ReadRepositoryFile("Praxis", "Behaviors", "GrabHandCursorBehavior.cs");

        Assert.Contains("if (!IsPrimaryOnlyPointerPressed(e))", source);
        Assert.Contains("private static bool IsPrimaryOnlyPointerPressed(PointerEventArgs e)", source);
        Assert.Contains("private static bool IsAnyPrimaryPointerStillPressed(PointerEventArgs e)", source);
        Assert.Contains("private void OnPointerMoved(object? sender, PointerEventArgs e)", source);
        Assert.Contains("if (!IsAnyPrimaryPointerStillPressed(e))", source);
        Assert.Contains("props.IsLeftButtonPressed", source);
        Assert.Contains("!props.IsRightButtonPressed", source);
        Assert.Contains("!props.IsMiddleButtonPressed", source);
        // Mac path delegates to the shared classifier so secondary/middle detection
        // is not reduced to substring inspection of platform-args text.
        Assert.Contains("PointerButtonClassifier.IsPrimaryOnly(e.PlatformArgs)", source);
        Assert.Contains("PointerButtonClassifier.IsPrimaryPressed(e.PlatformArgs)", source);
    }

    [Fact]
    public void GrabHandCursorBehavior_OnDetachingFrom_ClearsGrabCursor_IfStillGrabbing()
    {
        var source = ReadRepositoryFile("Praxis", "Behaviors", "GrabHandCursorBehavior.cs");

        Assert.Contains("protected override void OnDetachingFrom(View bindable)", source);
        var detachIndex = source.IndexOf("protected override void OnDetachingFrom(View bindable)", StringComparison.Ordinal);
        Assert.True(detachIndex >= 0, "OnDetachingFrom must exist.");

        var detachBody = source[detachIndex..];
        var restoreIndex = detachBody.IndexOf("ClearActiveGrab();", StringComparison.Ordinal);
        var removeIndex = detachBody.IndexOf("bindable.GestureRecognizers.Remove(pointer);", StringComparison.Ordinal);

        Assert.True(restoreIndex >= 0, "OnDetachingFrom must restore the cursor when detaching while grabbing.");
        Assert.True(removeIndex > restoreIndex, "Cursor restore should run before gesture recognizers are removed.");
        Assert.Contains("ReferenceEquals(GetActiveGrabBehavior(), this) && isGrabbing", detachBody);
    }

    [Fact]
    public void MainPage_DraggablePointerMoved_HasMacPrimaryReleaseFallback()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.PointerAndSelection.cs");

        Assert.Contains("private void Draggable_PointerMoved(object? sender, PointerEventArgs e)", source);
        Assert.Contains("#elif MACCATALYST", source);
        Assert.Contains("if (!IsPrimaryPointerPressed(e))", source);
        Assert.Contains("GrabHandCursorBehavior.ClearActiveGrab();", source);
        Assert.Contains("ExecuteDragFromItem(bindable.BindingContext, GestureStatus.Completed, pointerLastDx, pointerLastDy);", source);
    }

    [Fact]
    public void MainPage_DraggablePointerReleased_ClearsMacGrabCursor()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.PointerAndSelection.cs");

        Assert.Contains("private void Draggable_PointerReleased(object? sender, PointerEventArgs e)", source);
        Assert.Contains("GrabHandCursorBehavior.ClearActiveGrab();", source);
        Assert.Contains("ReleaseCapturedPointer();", source);

        var releaseIndex = source.IndexOf("private void Draggable_PointerReleased(object? sender, PointerEventArgs e)", StringComparison.Ordinal);
        var clearIndex = source.IndexOf("GrabHandCursorBehavior.ClearActiveGrab();", releaseIndex, StringComparison.Ordinal);
        var guardIndex = source.IndexOf("if (!pointerDragging || sender is not BindableObject bindable)", releaseIndex, StringComparison.Ordinal);

        Assert.True(clearIndex > releaseIndex, "PointerReleased should clear the grab cursor on macOS.");
        Assert.True(clearIndex < guardIndex, "PointerReleased should clear the grab cursor before the non-mac drag guard returns.");
    }

    [Fact]
    public void MainPage_DraggablePanUpdated_ClearsMacGrabCursor()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.EditorAndInput.cs");

        Assert.Contains("private void Draggable_PanUpdated(object? sender, PanUpdatedEventArgs e)", source);
        Assert.Contains("GrabHandCursorBehavior.ClearActiveGrab();", source);
        Assert.Contains("ExecuteDragFromItem(panDragItem ?? bindable.BindingContext, GestureStatus.Completed, dx, dy);", source);

        var panIndex = source.IndexOf("private void Draggable_PanUpdated(object? sender, PanUpdatedEventArgs e)", StringComparison.Ordinal);
        var clearIndex = source.IndexOf("GrabHandCursorBehavior.ClearActiveGrab();", panIndex, StringComparison.Ordinal);
        var completedIndex = source.IndexOf("ExecuteDragFromItem(panDragItem ?? bindable.BindingContext, GestureStatus.Completed, dx, dy);", panIndex, StringComparison.Ordinal);

        Assert.True(clearIndex > panIndex, "PanUpdated should clear the grab cursor when the drag completes.");
        Assert.True(clearIndex > completedIndex, "PanUpdated should clear the grab cursor after dispatching drag completion.");
    }

    [Fact]
    public void MainPage_PlacementAreaButtons_UseGrabHandCursorBehavior_InsteadOfHoverHand()
    {
        var xaml = ReadRepositoryFile("Praxis", "MainPage.xaml");

        Assert.Equal(1, CountOccurrences(xaml, "<behaviors:GrabHandCursorBehavior />"));
        Assert.Contains("<behaviors:GrabHandCursorBehavior />\n                                        <behaviors:MiddleClickBehavior", xaml);
    }

    [Fact]
    public void MainPage_CommandAndSearchInputs_UseExplicitFocusedUnderline()
    {
        var xaml = ReadRepositoryFile("Praxis", "MainPage.xaml");
        var source = ReadRepositoryFile("Praxis", "MainPage.EditorAndInput.cs");

        Assert.Contains("Focused=\"MainCommandEntry_Focused\"", xaml);
        Assert.Contains("Unfocused=\"MainCommandEntry_Unfocused\"", xaml);
        Assert.Contains("Focused=\"MainSearchEntry_Focused\"", xaml);
        Assert.Contains("Unfocused=\"MainSearchEntry_Unfocused\"", xaml);
        Assert.Contains("x:Name=\"MainCommandFocusUnderline\"", xaml);
        Assert.Contains("x:Name=\"MainSearchFocusUnderline\"", xaml);
        Assert.Contains("SetMainInputFocusUnderline(MainCommandFocusUnderline, focused: true);", source);
        Assert.Contains("SetMainInputFocusUnderline(MainCommandFocusUnderline, focused: false);", source);
        Assert.Contains("SetMainInputFocusUnderline(MainSearchFocusUnderline, focused: true);", source);
        Assert.Contains("SetMainInputFocusUnderline(MainSearchFocusUnderline, focused: false);", source);
        Assert.Contains("underline.Opacity = focused ? 1 : 0;", source);
    }

    [Fact]
    public void WindowsDefaultTextStyles_UseYuGothicUiWithoutChangingIconFonts()
    {
        var styles = ReadRepositoryFile("Praxis", "Resources", "Styles", "Styles.xaml");
        var mainPage = ReadRepositoryFile("Praxis", "MainPage.xaml");

        Assert.Equal(4, CountOccurrences(styles, "FontFamily\" Value=\"{OnPlatform WinUI='Yu Gothic UI'}\""));
        Assert.Contains("<Style TargetType=\"Label\">", styles);
        Assert.Contains("<Style TargetType=\"Entry\" ApplyToDerivedTypes=\"True\">", styles);
        Assert.Contains("<Style TargetType=\"Editor\">", styles);
        Assert.Contains("<Style TargetType=\"Button\">", styles);
        Assert.DoesNotContain("Noto Sans JP", styles);
        Assert.DoesNotContain("WinUI=OpenSansRegular", styles);
        Assert.DoesNotContain("WinUI=OpenSansSemibold", styles);
        Assert.Contains("FontFamily=\"{OnPlatform WinUI='Segoe MDL2 Assets', MacCatalyst=''}\"", mainPage);
    }

    [Fact]
    public void MainPage_DockRegion_UsesCompactBorderlessGlassFootprint()
    {
        var xaml = ReadRepositoryFile("Praxis", "MainPage.xaml");
        var dockSource = ReadRepositoryFile("Praxis", "MainPage.DockAndQuickLook.cs");

        Assert.Contains("x:Name=\"DockRegionBorder\"", xaml);
        Assert.Contains("StrokeThickness=\"0\"", xaml);
        Assert.Contains("Margin=\"{OnPlatform Default='0,-16,0,-14', WinUI='0,-16,0,-14', MacCatalyst='0,-16,0,-14'}\"", xaml);
        Assert.Contains("Padding=\"12,0,12,4\"", xaml);
        Assert.Contains("MinimumHeightRequest=\"{OnPlatform Default=78, WinUI=82, MacCatalyst=82}\"", xaml);
        Assert.Contains("Margin=\"0,0,0,18\"", xaml);
        Assert.Contains("Margin=\"0,0,0,10\"\n                                           VerticalOptions=\"Start\"", xaml);
        Assert.Contains("VerticalOptions=\"Start\"\n                                        Padding=\"0\"", xaml);
        Assert.Contains("x:Name=\"DockScrollBarMask\"", xaml);
        Assert.Contains("private const double MacDockScrollIndicatorBottomOffset = 8;", dockSource);
        Assert.Contains("scrollView.ClipsToBounds = true;", dockSource);
        Assert.Contains("scrollView.ScrollIndicatorInsets = new UIEdgeInsets(0, 0, (nfloat)(-MacDockScrollIndicatorBottomOffset), 0);", dockSource);
    }

    [Fact]
    public void MainPage_ContextMenu_UsesNativeMacKeyCaptureView()
    {
        var xaml = ReadRepositoryFile("Praxis", "MainPage.xaml");
        var fieldsSource = ReadRepositoryFile("Praxis", "MainPage.Fields.MacCatalyst.cs");
        var focusSource = ReadRepositoryFile("Praxis", "MainPage.FocusAndContext.cs");
        var eventSource = ReadRepositoryFile("Praxis", "MainPage.ViewModelEvents.cs");

        Assert.DoesNotContain("x:Name=\"ContextMenuKeyCaptureEntry\"", xaml);
        Assert.Contains("private MacContextMenuKeyCaptureView? macContextMenuKeyCaptureView;", fieldsSource);
        Assert.Contains("private sealed class MacContextMenuKeyCaptureView(Action<string> dispatchShortcut) : UIView", focusSource);
        Assert.Contains("public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)", focusSource);
        Assert.Contains("dispatchShortcut(action);", focusSource);
        Assert.Contains("action = \"PrimaryAction\";", focusSource);
        Assert.Contains("action = \"ContextMenuNext\";", focusSource);
        Assert.Contains("FocusMacContextMenuKeyCaptureView();", eventSource);
        Assert.Contains("RemoveMacContextMenuKeyCaptureView();", eventSource);
        Assert.DoesNotContain("responder.BecomeFirstResponder();\n        }\n        SyncMacContextMenuPseudoFocusFromButton(button);", focusSource, StringComparison.Ordinal);
    }

    [Fact]
    public void MainPage_ContextMenu_UsesMutedOpaquePopupSurface()
    {
        var xaml = ReadRepositoryFile("Praxis", "MainPage.xaml");

        Assert.Contains("x:Name=\"CommandSuggestionPopup\"", xaml);
        Assert.Contains("<Color x:Key=\"MutedPopupLight\">#FFD4DADF</Color>", xaml);
        Assert.Contains("<Color x:Key=\"MutedPopupDark\">#FF303842</Color>", xaml);
        Assert.Contains("BackgroundColor=\"{AppThemeBinding Light={StaticResource MutedPopupLight}, Dark={StaticResource MutedPopupDark}}\"", xaml);
        Assert.Contains("TextColor=\"{AppThemeBinding Light=#111111, Dark=#F5F7FA}\"", xaml);
        Assert.Contains("WidthRequest=\"220\"", xaml);
        Assert.Contains("CornerRadius=\"14\"", xaml);
        Assert.Contains("MacOSBehindWindowBlur=\"False\"", xaml);
        Assert.DoesNotContain("BackgroundColor=\"{AppThemeBinding Light=#52FFFFFF, Dark=#361E2228}\"", xaml);

        var focusSource = ReadRepositoryFile("Praxis", "MainPage.FocusAndContext.cs");
        Assert.Contains("ring.BackgroundColor = focused", focusSource);
        Assert.Contains("ResolveContextActionFocusFillColor(dark)", focusSource);
        Assert.Contains("ResolveContextActionTextColor(focused, dark)", focusSource);
        Assert.Contains("Color.FromArgb(dark ? \"#4F5964\" : \"#B5BEC7\")", focusSource);
        Assert.Contains("Color.FromArgb(dark ? \"#FFFFFF\" : \"#0A0C10\")", focusSource);
    }

    [Fact]
    public void MainPage_WindowsModalTabOrder_LeavesNoteForActionsThenWrapsToGuid()
    {
        var focusSource = ReadRepositoryFile("Praxis", "MainPage.FocusAndContext.cs");
        var modalSource = ReadRepositoryFile("Praxis", "MainPage.ModalEditor.cs");
        var macSource = ReadRepositoryFile("Praxis", "MainPage.MacCatalystBehavior.cs");
        var pageSource = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        var windowsSource = ReadRepositoryFile("Praxis", "MainPage.WindowsInput.cs");
        var eventsSource = ReadRepositoryFile("Praxis", "MainPage.ViewModelEvents.cs");

        Assert.Contains("SetTabStop(ModalInvertThemeCheckBox, false);", focusSource);
        Assert.Contains("ApplyModalEditorThemeTextColors();", pageSource);
        Assert.Contains("ApplyModalEditorThemeTextColors();", eventsSource);
        Assert.Contains("ModalGuidEntry.TextColor = textColor;", modalSource);
        Assert.Contains("ModalButtonTextEntry.TextColor = textColor;", modalSource);
        Assert.Contains("ModalCommandEntry.TextColor = textColor;", modalSource);
        Assert.Contains("ApplyMacEntryVisualState();", modalSource);
        Assert.Contains("textField.TextColor = textColor;", macSource);
        Assert.Contains("textField.TintColor = ResolveMacTextSelectionColor(dark);", macSource);
        Assert.Contains("private static readonly UIColor LightMacTextSelectionColor = UIColor.FromRGB(0x4B, 0x00, 0xD9);", macSource);
        Assert.Contains("private static readonly UIColor DarkMacTextSelectionColor = UIColor.FromRGB(0x35, 0x00, 0xA8);", macSource);
        Assert.Contains("private static UIColor ResolveMacTextInputForeground(bool dark)", macSource);
        Assert.Contains("private static UIColor ResolveMacTextSelectionColor(bool dark)", macSource);
        Assert.Contains("=> dark ? UIColor.White : UIColor.Black;", macSource);
        Assert.Contains("=> dark ? DarkMacTextSelectionColor : LightMacTextSelectionColor;", macSource);
        Assert.Contains("textView.TintColor = ResolveMacTextSelectionColor(dark);", macSource);
        Assert.Contains("EnsureWindowsTextBoxHooks();", pageSource);
        Assert.Contains("EnsureWindowsTextBoxHooks();", eventsSource);
        Assert.Contains("App.RefreshPlatformWindowBackdrops();", eventsSource);
        Assert.Contains("TryMoveWindowsModalEditorFocusFromTextBox(sender, forward: !IsWindowsShiftDown())", windowsSource);
        Assert.Contains("ReferenceEquals(textBox, modalNoteTextBox)", windowsSource);
        Assert.Contains("FocusWindowsModalActionButton(ModalCancelButton);", windowsSource);
        Assert.Contains("FocusWindowsModalActionButton(ModalSaveButton);", windowsSource);
        Assert.Contains("FocusWindowsModalTextBox(modalGuidTextBox, ModalGuidEntry);", windowsSource);
        Assert.Contains("private WindowsModalActionFocusTarget? windowsModalActionFocusTarget;", windowsSource);
        Assert.Contains("windowsModalActionFocusTarget is not null", windowsSource);
        Assert.Contains("TryInvokeWindowsModalAction()", windowsSource);
        Assert.Contains("ApplyWindowsModalActionFocusVisuals();", windowsSource);
        Assert.Contains("button.StrokeThickness = focused ? 2 : 0;", windowsSource);
        Assert.Contains("ResolveWindowsModalActionFocusStrokeColor()", windowsSource);
        Assert.Contains("Color.FromArgb(\"#FF5F6974\")", windowsSource);
        Assert.Contains("Color.FromArgb(\"#FFD5D9DF\")", windowsSource);
        Assert.Contains("ClearWindowsTextSelection(sender);", windowsSource);
        Assert.Contains("textBox.SelectionLength = 0;", windowsSource);
        Assert.Contains("ConfigureWindowsTextSelectionAndCaret(focusedTextBox);", windowsSource);
        Assert.Contains("Praxis.Controls.WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(focusedTextBox);", windowsSource);
        Assert.Contains("slot.SelectionChanged += WindowsTextBox_SelectionChanged;", windowsSource);
        Assert.Contains("slot.SelectionChanged -= WindowsTextBox_SelectionChanged;", windowsSource);
        Assert.Contains("private void WindowsTextBox_SelectionChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)", windowsSource);
        Assert.Contains("textBox.RequestedTheme = IsWindowsTextDarkThemeActive()", windowsSource);
        Assert.Contains("IsWindowsTextDarkThemeActive()", windowsSource);
        Assert.Contains("textBox.Foreground = foregroundBrush;", windowsSource);
        Assert.Contains("textBox.SelectionHighlightColor = selectionBrush;", windowsSource);
        Assert.Contains("textBox.SelectionHighlightColorWhenNotFocused = selectionBrush;", windowsSource);
        Assert.Contains("textBox.Background = chromeBrush;", windowsSource);
        Assert.Contains("textBox.BorderBrush = chromeBrush;", windowsSource);
        Assert.Contains("textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"TextControlBackground\", chromeBrush);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"TextControlBackgroundPointerOver\", chromeBrush);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"TextControlBackgroundFocused\", chromeBrush);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"TextControlBorderBrush\", chromeBrush);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"TextControlBorderBrushPointerOver\", chromeBrush);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"TextControlBorderBrushFocused\", chromeBrush);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"TextControlCaretBrush\", caretBrush);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"TextControlCaretBrushFocused\", caretBrush);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"TextControlSelectionHighlightColor\", selectionBrush);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"TextControlSelectionHighlightForeground\", selectionForegroundBrush);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"TextSelectionHighlightColorThemeBrush\", selectionBrush);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"AccentFillColorSelectedTextBackgroundBrush\", selectionBrush);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"TextOnAccentFillColorSelectedText\", selectionForegroundColor);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"TextOnAccentFillColorSelectedTextBrush\", selectionForegroundBrush);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"SystemColorHighlightTextColor\", selectionForegroundColor);", windowsSource);
        Assert.Contains("TrySetWindowsTextControlResource(textBox, \"SystemColorHighlightTextColorBrush\", selectionForegroundBrush);", windowsSource);
        Assert.Contains("App.RefreshWindowsTextSelectionResources();", windowsSource);
        Assert.Contains("global::Windows.UI.Color.FromArgb(255, 107, 75, 176)", windowsSource);
        Assert.Contains("global::Windows.UI.Color.FromArgb(255, 82, 58, 124)", windowsSource);
        Assert.Contains("global::Windows.UI.Color.FromArgb(255, 0, 0, 0)", windowsSource);
        Assert.Contains("global::Windows.UI.Color.FromArgb(255, 255, 255, 255)", windowsSource);
        Assert.DoesNotContain("global::Windows.UI.Color.FromArgb(255, 245, 247, 250)", windowsSource);
        Assert.DoesNotContain("global::Windows.UI.Color.FromArgb(255, 208, 213, 220)", windowsSource);
        Assert.DoesNotContain("global::Windows.UI.Color.FromArgb(255, 76, 86, 96)", windowsSource);
    }

    [Fact]
    public void MauiThemeService_RefreshesWindowsBackdropAfterApplyingUserTheme()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "MauiThemeService.cs");

        Assert.Contains("Application.Current.UserAppTheme = appTheme;", source);
        Assert.Contains("App.RefreshPlatformWindowBackdrops();", source);
    }

    [Fact]
    public void SqliteAppRepository_Initialization_AssignsSharedConnectionOnlyAfterSuccessfulLoad()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "SqliteAppRepository.cs");

        Assert.Contains("var initializedConnection = new SQLiteAsyncConnection(dbPath);", source);
        Assert.Contains("await EnsureSchemaAsync(initializedConnection);", source);
        Assert.Contains("var entities = await initializedConnection.Table<LauncherButtonEntity>().ToListAsync();", source);
        Assert.Contains("connection = initializedConnection;", source);

        var cacheRebuildIndex = source.IndexOf("RebuildCommandCache();", StringComparison.Ordinal);
        var assignmentIndex = source.IndexOf("connection = initializedConnection;", StringComparison.Ordinal);

        Assert.True(cacheRebuildIndex >= 0, "InitializeCoreAsync should rebuild the command cache before publishing the shared connection.");
        Assert.True(assignmentIndex > cacheRebuildIndex, "connection should only be published after schema and cache initialization succeed.");
    }

    [Fact]
    public void App_CreateWindow_DoesNotCacheFallbackErrorPage()
    {
        var source = ReadRepositoryFile("Praxis", "App.xaml.cs");

        Assert.Contains("if (page is MainPage)", source);
        Assert.Contains("rootPage = page;", source);
        Assert.Contains("Root page resolution fell back to an error page; cache not updated.", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("new Label { Text = safeMessage },", source);
    }

    [Fact]
    public void App_CreateWindow_HandlerChangeFailures_WarningLogRootPageType()
    {
        var source = ReadRepositoryFile("Praxis", "App.xaml.cs");

        Assert.Contains("errorLogger?.Log(ex, \"Window.HandlerChanged\");", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("var platformViewType = window.Handler?.PlatformView?.GetType().Name ?? \"(null)\";", source);
        Assert.Contains("errorLogger?.LogWarning(", source);
        Assert.Contains("\"Window handler activation failed for root page '{page.GetType().Name}' with platformView='{platformViewType}': {safeMessage}\"", source);
        Assert.Contains("\"Window.HandlerChanged\");", source);
    }

    [Fact]
    public void App_CreateWindow_AppliesGlassBackdrop_OnHandlerAndThemeChanges()
    {
        var source = ReadRepositoryFile("Praxis", "App.xaml.cs");

        Assert.Contains("RequestedThemeChanged += (_, _) =>", source);
        Assert.Contains("RefreshWindowsTextSelectionResources();", source);
        Assert.Contains("RefreshWindowBackdrops();", source);
        Assert.Contains("public static void RefreshWindowsTextSelectionResources()", source);
        Assert.Contains("Microsoft.UI.Xaml.Application.Current?.Resources", source);
        Assert.Contains("SetWindowsAppResource(resources, \"TextOnAccentFillColorSelectedText\", selectionForegroundColor);", source);
        Assert.Contains("SetWindowsAppResource(resources, \"TextOnAccentFillColorSelectedTextBrush\", selectionForegroundBrush);", source);
        Assert.Contains("SetWindowsAppResource(resources, \"TextControlSelectionHighlightColor\", selectionBackgroundBrush);", source);
        Assert.Contains("SetWindowsAppResource(resources, \"TextSelectionHighlightColorThemeBrush\", selectionBackgroundBrush);", source);
        Assert.Contains("SetWindowsAppResource(resources, \"AccentFillColorSelectedTextBackgroundBrush\", selectionBackgroundBrush);", source);
        var windowsXaml = ReadRepositoryFile("Praxis", "Platforms", "Windows", "App.xaml");
        Assert.Contains("<Color x:Key=\"TextOnAccentFillColorSelectedText\">#FFFFFFFF</Color>", windowsXaml);
        Assert.Contains("<SolidColorBrush x:Key=\"TextControlSelectionHighlightColor\" Color=\"#FF6B4BB0\" />", windowsXaml);
        Assert.Contains("<SolidColorBrush x:Key=\"TextControlSelectionHighlightColor\" Color=\"#FF523A7C\" />", windowsXaml);
        Assert.Contains("page.BackgroundColor = Colors.Transparent;", source);
        Assert.Contains("#if WINDOWS", source);
        Assert.Contains("Title = string.Empty,", source);
        Assert.Contains("window.HandlerChanged += (_, _) => ApplyPlatformWindowBackdrop(window);", source);
        Assert.Contains("private static void RefreshWindowBackdrops()", source);
        Assert.Contains("public static void RefreshPlatformWindowBackdrops()", source);
        Assert.Contains("foreach (var window in windows)", source);
        Assert.Contains("static partial void ApplyPlatformWindowBackdrop(Window window);", source);
    }

    [Fact]
    public void WindowsGlassBackdrop_UsesNativeAcrylicAndTransparentChrome()
    {
        var source = ReadRepositoryFile("Praxis", "App.WindowsBackdrop.cs");

        Assert.Contains("static partial void ApplyPlatformWindowBackdrop(Microsoft.Maui.Controls.Window window)", source);
        Assert.Contains("ApplyWindowsDesktopAcrylicController(nativeWindow)", source);
        Assert.Contains("DesktopAcrylicController.IsSupported()", source);
        Assert.Contains("WindowsDesktopAcrylicStates.GetValue(nativeWindow, CreateWindowsDesktopAcrylicState)", source);
        Assert.Contains("nativeWindow.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>()", source);
        Assert.Contains("state.Configuration.IsInputActive = true;", source);
        Assert.Contains("state.Controller.TintOpacity = isDark ? 0.04f : 0.02f;", source);
        Assert.Contains("state.Controller.LuminosityOpacity = isDark ? 0.18f : 0.12f;", source);
        Assert.DoesNotContain("RemoveSystemBackdropTarget", source);
        Assert.Contains("QueueWindowsBackdropRefresh(nativeWindow);", source);
        Assert.Contains("TryRefreshWindowsBackdrop(nativeWindow, \"queued activation/resize refresh\")", source);
        Assert.DoesNotContain("DesktopAcrylicBackdrop", source);
        Assert.Contains("ApplyWindowsRootTransparency(nativeWindow);", source);
        Assert.Contains("ApplyWindowsContentChrome(nativeWindow);", source);
        Assert.Contains("ApplyWindowsChromeTint(nativeWindow);", source);
        Assert.Contains("EnsureWindowsCustomChrome(nativeWindow);", source);
        Assert.Contains("PraxisWindowsCustomChromeRoot", source);
        Assert.Contains("CreateWindowsCustomTitleBar(nativeWindow)", source);
        Assert.Contains("AttachWindowsBackdropActivationRefresh(nativeWindow);", source);
        Assert.Contains("AttachWindowsBackdropSizeRefresh(nativeWindow);", source);
        Assert.Contains("WindowsWindowOnSizeChanged", source);
        Assert.Contains("Failed to refresh Windows backdrop after resize", source);
        Assert.Contains("RefreshWindowsBackdropAfterActivation(nativeWindow);", source);
        Assert.Contains("RefreshWindowsBackdropAfterResize(nativeWindow);", source);
        Assert.Contains("Failed to refresh Windows backdrop after activation", source);
        Assert.Contains("ResolveWindowsChromeTintColor()", source);
        Assert.Contains("chromeRoot.Background = tintBrush;", source);
        Assert.Contains("ResolveWindowsAcrylicGradientColor()", source);
        Assert.Contains("global::Windows.UI.Color.FromArgb(96, 255, 255, 255)", source);
        Assert.Contains("global::Windows.UI.Color.FromArgb(96, 0, 0, 0)", source);
        Assert.Contains("return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;", source);
        Assert.Contains("nativeWindow.Title = string.Empty;", source);
        Assert.Contains("nativeWindow.AppWindow.Title = string.Empty;", source);
        Assert.Contains("var color = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);", source);
        Assert.Contains("Height = 36,", source);
        Assert.Contains("titleBar.BackgroundColor = transparent;", source);
        Assert.Contains("titleBar.Background = transparentBrush;", source);
        Assert.Contains("ResolveWindowsCaptionForegroundColor()", source);
        Assert.Contains("global::Windows.UI.Color.FromArgb(238, 245, 247, 250)", source);
        Assert.Contains("ResolveWindowsCaptionHoverBackgroundColor()", source);
        Assert.Contains("global::Windows.UI.Color.FromArgb(30, 0, 0, 0)", source);
        Assert.Contains("global::Windows.UI.Color.FromArgb(48, 0, 0, 0)", source);
        Assert.Contains("titleBar.ButtonHoverBackgroundColor = captionHoverBackground;", source);
        Assert.Contains("titleBar.ButtonPressedBackgroundColor = captionPressedBackground;", source);
        Assert.Contains("titleBar.ButtonPressedForegroundColor = captionPressedForeground;", source);
        Assert.DoesNotContain("Text = \"Praxis\",", source);
        Assert.Contains("CreateWindowsCaptionButton", source);
        Assert.Contains("windowsMaximizeRestoreIcon", source);
        Assert.Contains("UseSystemFocusVisuals = false,", source);
        Assert.Contains("IsTabStop = false,", source);
        Assert.Contains("Foreground = foregroundBrush", source);
        Assert.Contains("icon.Foreground = foregroundBrush;", source);
        Assert.Contains("PointerEntered", source);
        Assert.Contains("PointerPressed", source);
        Assert.Contains("PointerReleased", source);
        Assert.Contains("PointerExited", source);
        Assert.Contains("PointerCanceled", source);
        Assert.Contains("PointerCaptureLost", source);
        Assert.Contains("ApplyWindowsCaptionButtonResources(button);", source);
        Assert.Contains("button.Resources[\"ButtonBackgroundPointerOver\"] = hoverBackground;", source);
        Assert.Contains("button.Resources[\"ButtonForegroundPressed\"] = pressedForeground;", source);
        Assert.Contains("UpdateWindowsMaximizeRestoreIcon(nativeWindow);", source);
        Assert.Contains("titleBar.ForegroundColor = captionForeground;", source);
        Assert.Contains("titleBar.ButtonForegroundColor = captionForeground;", source);
        Assert.Contains("UpdateWindowsCaptionButtonForegrounds(titleBar);", source);
        Assert.Contains("element is Microsoft.UI.Xaml.Controls.Grid { Name: WindowsCustomChromeRootName }", source);
        Assert.Contains("nativeWindow.ExtendsContentIntoTitleBar = true;", source);
        Assert.Contains("nativeWindow.SetTitleBar(titleBar);", source);
        Assert.Contains("presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false);", source);
        Assert.Contains("ApplyWindowsTitleBarTransparency(nativeWindow);", source);
        Assert.Contains("EnableWindowsQuickAccessCaptionStyle(hwnd);", source);
        Assert.Contains("style.ToInt64() | WS_CAPTION", source);
        Assert.DoesNotContain("& ~WS_CAPTION", source);
        Assert.DoesNotContain("WS_THICKFRAME", source);
        Assert.DoesNotContain("ExtendWindowsFrameIntoClientArea(hwnd);", source);
        Assert.DoesNotContain("EnableWindowsDwmBlurBehind(hwnd);", source);
        Assert.Contains("LogWindowsBackdropDiagnostics(rootElement);", source);
        Assert.Contains("AttachWindowsRootTransparencyRefresh(nativeWindow, rootElement);", source);
        Assert.Contains("EnsureWindowsResizeEraseSuppression(nativeWindow, hwnd);", source);
        Assert.Contains("WindowsBackdropWindowsByHwnd[hwnd] = new WeakReference<Microsoft.UI.Xaml.Window>(nativeWindow);", source);
        Assert.Contains("SetWindowSubclass(hwnd, WindowsBackdropSubclassProc, WindowsBackdropSubclassId, UIntPtr.Zero)", source);
        Assert.Contains("message == WM_ERASEBKGND", source);
        Assert.Contains("ResizeWindowsChromeRootToClient(hwnd);", source);
        Assert.Contains("GetClientRect(hwnd, out var rect)", source);
        Assert.Contains("SetWindowsElementSize(rootElement, width, height);", source);
        Assert.Contains("SetWindowsElementSize(originalContent, width, height);", source);
        Assert.Contains("rootElement.Measure(new global::Windows.Foundation.Size(width, height));", source);
        Assert.Contains("rootElement.Arrange(new global::Windows.Foundation.Rect(0, 0, width, height));", source);
        Assert.Contains("rootElement.UpdateLayout();", source);
        Assert.Contains("GetDpiForWindow(hwnd)", source);
        Assert.Contains("FillWindowsResizeFallbackBackground(hwnd, wParam);", source);
        Assert.Contains("return new IntPtr(1);", source);
        Assert.Contains("message is WM_SIZE or WM_SIZING or WM_WINDOWPOSCHANGING or WM_WINDOWPOSCHANGED", source);
        Assert.Contains("ApplyWindowsAcrylicBackdrop(hwnd);", source);
        Assert.Contains("InvalidateRect(hwnd, IntPtr.Zero, false);", source);
        Assert.Contains("ResolveWindowsResizeFallbackColor()", source);
        Assert.Contains("GetClientRect(hwnd, out var rect)", source);
        Assert.Contains("CreateSolidBrush(ToColorRef(ResolveWindowsResizeFallbackColor()))", source);
        Assert.Contains("FillRect(hdc, ref rect, brush);", source);
        Assert.Contains("DeleteObject(brush);", source);
        Assert.Contains("WindowsBackdropWindowsByHwnd.TryRemove(hwnd, out _);", source);
        Assert.Contains("RemoveWindowSubclass(hwnd, WindowsBackdropSubclassProc, subclassId);", source);
        Assert.Contains("DefSubclassProc(hwnd, message, wParam, lParam);", source);
        Assert.Contains("ResizeEraseSuppressionHwnd", source);
        Assert.Contains("frameworkElement.Loaded += WindowsRootElementOnLoaded;", source);
        Assert.Contains("frameworkElement.SizeChanged += WindowsRootElementOnSizeChanged;", source);
        Assert.Contains("RefreshWindowsRootTransparency(rootElement);", source);
        Assert.Contains("ClearWindowsOpaqueWrapperBackgrounds(rootElement)", source);
        Assert.Contains("nativeWindow.DispatcherQueue.TryEnqueue(() => RefreshWindowsRootTransparency(rootElement))", source);
        Assert.Contains("var coversRoot = CoversMostOfWindowsRoot(element, rootWidth, rootHeight);", source);
        Assert.Contains("(coversRoot || IsWindowsBackdropBlockingBrush", source);
        Assert.Contains("IsWindowsBackdropBlockingBrush", source);
        Assert.Contains("CoversMostOfWindowsRoot(element, rootWidth, rootHeight)", source);
        Assert.Contains("color.A >= 240 && max - min <= 36 && min >= 218;", source);
        Assert.Contains("WindowsBackdrop visual tree:", source);
        Assert.Contains("AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND", source);
        Assert.Contains("AccentFlags = 0,", source);
        Assert.Contains("GradientColor = gradientColor,", source);
        Assert.Contains("var result = SetWindowCompositionAttribute(hwnd, ref data);", source);
        Assert.Contains("\"SetWindowCompositionAttribute did not apply Windows acrylic blur.\"", source);
        Assert.DoesNotContain("SetLayeredWindowAttributes", source);
        Assert.DoesNotContain("WS_EX_LAYERED", source);
        Assert.DoesNotContain("ApplyWindowsDwmSystemBackdrop(hwnd);", source);
        Assert.DoesNotContain("DWM_SYSTEMBACKDROP_TYPE", source);
        Assert.DoesNotContain("DwmEnableBlurBehindWindow", source);
        Assert.DoesNotContain("DWM_BB_ENABLE", source);
        Assert.Contains("GetWindowLongPtr(hwnd, GWL_STYLE)", source);
        Assert.Contains("SetWindowLongPtr(hwnd, GWL_STYLE, updatedStyle);", source);
        Assert.DoesNotContain("DwmExtendFrameIntoClientArea", source);
        Assert.Contains("DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE", source);
    }

    [Fact]
    public void MacGlassBackdrop_UsesNativeMaterialBlurAndRoundedClip()
    {
        var appSource = ReadRepositoryFile("Praxis", "App.MacBackdrop.cs");
        var behaviorSource = ReadRepositoryFile("Praxis", "Behaviors", "MacGlassBackdropBehavior.cs");
        var frameSource = ReadRepositoryFile("Praxis", "Controls", "MaterialFrame.cs");

        Assert.Contains("static partial void ApplyPlatformWindowBackdrop(Microsoft.Maui.Controls.Window window)", appSource);
        Assert.Contains("TrySetBool(nativeMacWindow, \"opaque\", false);", appSource);
        Assert.Contains("titlebar.TitleVisibility = UITitlebarTitleVisibility.Hidden;", appSource);
        Assert.Contains("ClearMacViewTree(nativeWindow);", appSource);
        Assert.Contains("nativeWindow.Layer.Opaque = false;", appSource);
        Assert.Contains("nativeWindow.Layer.BorderWidth = 0f;", appSource);
        Assert.Contains("view.Layer.Opaque = false;", appSource);
        Assert.Contains("view.Layer.BorderWidth = 0f;", appSource);
        Assert.Contains("TrySetBool(target, \"opaque\", false);", appSource);
        Assert.Contains("layer.Opaque = false;", appSource);
        Assert.Contains("ClearNativeWindowChrome(nativeMacWindow, \"contentView\");", appSource);
        Assert.Contains("if (key is \"contentView\" or \"frameView\")", appSource);
        Assert.Contains("ClearNativeObjectSurface(chromeObject);", appSource);
        Assert.Contains("MaterialFrame.MacNativeHostTag", appSource);
        Assert.Contains("EnsureNativeNsDummyRootGlass(", appSource);
        Assert.Contains("nativeWindow.TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark", appSource);
        Assert.Contains("Class.GetHandle(\"NSVisualEffectView\")", appSource);
        Assert.Contains("Class.GetHandle(\"NSView\")", appSource);
        Assert.Contains("MacNativeRootGlassAutoresizingMask = 18", appSource);
        Assert.Contains("MacNativeRootGlassOverscan = 1d", appSource);
        Assert.Contains("var hostView = ResolveNativeRootGlassHostView(contentView);", appSource);
        Assert.Contains("RemoveStaleNativeRootGlass(contentView, hostView);", appSource);
        Assert.Contains("var frame = ExpandNativeRootGlassFrame(ResolveNativeContentBounds(hostView, uiBounds));", appSource);
        Assert.Contains("TrySetInt(dummyRoot, \"autoresizingMask\", MacNativeRootGlassAutoresizingMask);", appSource);
        Assert.Contains("private static CGRect ResolveNativeContentBounds(NSObject contentView, CGRect fallbackBounds)", appSource);
        Assert.Contains("private static NSObject ResolveNativeRootGlassHostView(NSObject contentView)", appSource);
        Assert.Contains("private static void RemoveStaleNativeRootGlass(NSObject contentView, NSObject hostView)", appSource);
        Assert.Contains("private static CGRect ExpandNativeRootGlassFrame(CGRect frame)", appSource);
        Assert.Contains("contentView.ValueForKey(new NSString(\"bounds\")) is NSValue value", appSource);
        Assert.Contains("ClearNativeWindowChrome(nativeMacWindow, \"frameView\");", appSource);
        Assert.Contains("ClearNativeFrameChrome(nativeMacWindow);", appSource);
        Assert.Contains("MacNativePopoverMaterial = 6", appSource);
        Assert.Contains("MacNativeRootGlassLightAlpha = 1d", appSource);
        Assert.Contains("MacNativeRootGlassDarkAlpha = 1d", appSource);
        Assert.Contains("TrySetInt(dummyRoot, \"material\", MacNativePopoverMaterial);", appSource);
        Assert.Contains("TrySetBool(dummyRoot, \"emphasized\", true);", appSource);
        Assert.Contains("var rootGlassAlpha = isDark ? MacNativeRootGlassDarkAlpha : MacNativeRootGlassLightAlpha;", appSource);
        Assert.Contains("TrySetDouble(dummyRoot, \"alphaValue\", rootGlassAlpha);", appSource);
        Assert.Contains("TrySendDouble(dummyRoot, \"setAlphaValue:\", rootGlassAlpha);", appSource);
        Assert.Contains("private static void TrySetDouble(NSObject target, string key, double value)", appSource);
        Assert.Contains("private static void TrySendDouble(NSObject target, string selectorName, double value)", appSource);
        Assert.Contains("private static extern void ObjcMsgSendDouble(IntPtr receiver, IntPtr selector, double value);", appSource);
        Assert.Contains("layer.CornerRadius = 0f;", appSource);
        Assert.Contains("layer.MasksToBounds = false;", appSource);
        Assert.Contains("layer.BorderWidth = 0f;", appSource);
        Assert.Contains("ObjcMsgSendCGRect(allocated, SelRegisterName(\"initWithFrame:\"), frame);", appSource);
        Assert.Contains("MacNativeSubviewBelow = -1", appSource);
        Assert.Contains("MacTexturedBackgroundMask = (nint)(1 << 8)", appSource);
        Assert.Contains("styleMaskValue.Int64Value | MacFullSizeContentViewMask | MacTexturedBackgroundMask", appSource);
        Assert.Contains("ObjcMsgSendNInt(nativeWindow.Handle, SelRegisterName(\"setStyleMask:\"), (nint)updated);", appSource);
        Assert.Contains("private static void RemoveNativeSubview(NSObject view)", appSource);
        Assert.Contains("RemoveNativeSubview(dummyRoot);", appSource);
        Assert.Contains("ObjcMsgSendVoid(view.Handle, SelRegisterName(\"removeFromSuperview\"));", appSource);
        Assert.Contains("SelRegisterName(\"addSubview:positioned:relativeTo:\")", appSource);
        Assert.Contains("ObjcMsgSendAddSubviewPositioned(", appSource);
        Assert.DoesNotContain("nativeWindow.BringSubviewToFront(dummyRoot);", appSource);
        Assert.Contains("new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial))", behaviorSource);
        Assert.Contains("backdropView.Effect = UIBlurEffect.FromStyle(ResolveBackdropStyle());", behaviorSource);
        Assert.Contains("return UIBlurEffectStyle.SystemChromeMaterial;", behaviorSource);
        Assert.Contains("platformView.Layer.CornerRadius = (nfloat)cornerRadius;", behaviorSource);
        Assert.Contains("backdropView.Alpha = GetBackdropOpacity();", behaviorSource);
        Assert.Contains("backdropView.ContentView.BackgroundColor = ResolveBackdropTintColor();", behaviorSource);
        Assert.Contains("attachedView?.BackgroundColor?.ToPlatform() ?? UIColor.Clear", behaviorSource);
        Assert.Contains("public sealed class MaterialFrame : Border", frameSource);
        Assert.Contains("internal const nint MacNativeHostTag = 0x50475846;", frameSource);
        Assert.Contains("platformView.Tag = MacNativeHostTag;", frameSource);
        Assert.Contains("MacOSBehindWindowBlurProperty", frameSource);
        Assert.Contains("MacOSBackdropOpacityProperty", frameSource);
    }

    [Fact]
    public void MainPage_MacOnAppearing_ReappliesGlassBackdropAfterXamlLoads()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        var themeSource = ReadRepositoryFile("Praxis", "MainPage.StatusAndTheme.cs");

        Assert.Contains("App.RefreshMacWindowBackdropForConnectedScenes();", source);
        Assert.Contains("ScheduleMacRootTransparencyRefresh();", source);
        Assert.Contains("ForceTransparentRootBackground();", source);
        Assert.Contains("if (initialized)\n        {\n#if MACCATALYST\n            ApplyMacVisualTuning();\n            StartMacMiddleButtonPolling();\n            ScheduleMainCommandFocusAfterActivation(\"MainPage.OnAppearing\");", source);
        Assert.Contains("RootGrid.BackgroundColor = Colors.Transparent;", themeSource);
        Assert.Contains("RootGrid.Opacity = 1;", themeSource);
        Assert.DoesNotContain("DummyRootGlassFrame", themeSource);
    }

    [Fact]
    public void App_FlushFailures_AreWarningLogged_DuringUnhandledExceptionAndProcessExit()
    {
        var source = ReadRepositoryFile("Praxis", "App.xaml.cs");

        Assert.Contains("TryFlushLogs(TimeSpan.FromSeconds(2), \"AppDomain.UnhandledException\");", source);
        Assert.Contains("TryFlushLogs(TimeSpan.FromSeconds(3), \"App.ProcessExit\");", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("CrashFileLogger.WriteException(context, ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(context, $\"Log flush failed: {safeMessage}\");", source);
    }

    [Fact]
    public void App_GlobalExceptionHandlers_AreRegisteredOnlyOnce()
    {
        var source = ReadRepositoryFile("Praxis", "App.xaml.cs");

        Assert.Contains("private static int globalExceptionHandlersRegistered;", source);
        Assert.Contains("if (Interlocked.Exchange(ref globalExceptionHandlersRegistered, 1) == 0)", source);
        Assert.Contains("var safePayload = CrashFileLogger.SafeObjectDescription(e.ExceptionObject);", source);
    }

    [Fact]
    public void App_StaticEventDispatchers_UseSharedLoggedRaiseHelpers()
    {
        var source = ReadRepositoryFile("Praxis", "App.xaml.cs");

        Assert.Contains("private static void TryRaise(Action? handler, string context)", source);
        Assert.Contains("private static void TryRaise<T>(Action<T>? handler, T argument, string context)", source);
        Assert.Contains("TryRaise(MacApplicationDeactivating, nameof(RaiseMacApplicationDeactivating));", source);
        Assert.Contains("TryRaise(MacApplicationActivated, nameof(RaiseMacApplicationActivated));", source);
        Assert.Contains("TryRaise(ThemeShortcutRequested, mode, nameof(RaiseThemeShortcut));", source);
        Assert.Contains("TryRaise(EditorShortcutRequested, action, nameof(RaiseEditorShortcut));", source);
        Assert.Contains("TryRaise(CommandInputShortcutRequested, action, nameof(RaiseCommandInputShortcut));", source);
        Assert.Contains("TryRaise(HistoryShortcutRequested, action, nameof(RaiseHistoryShortcut));", source);
        Assert.Contains("TryRaise(MiddleMouseClickRequested, nameof(RaiseMiddleMouseClick));", source);
        Assert.Equal(2, CountOccurrences(source, "errorLogger?.Log(ex, context);"));
    }

    [Fact]
    public void FileStateSyncNotifier_ReadRetryExhaustion_IsWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "FileStateSyncNotifier.cs");

        Assert.Contains("Exception? readFailure = null;", source);
        Assert.Contains("private static string BuildSyncWarningMessage(string prefix, Exception ex)", source);
        Assert.Contains("return $\"{prefix} ({ex.GetType().Name}) {safeMessage}\";", source);
        Assert.Contains("private static string NormalizePayloadForLog(string payload)", source);
        Assert.Contains("var normalizedPayload = NormalizePayloadForLog(payload);", source);
        Assert.Contains("var normalizedSource = NormalizePayloadForLog(source);", source);
        Assert.Contains("BuildSyncWarningMessage($\"Failed to read sync payload '{normalizedSignalPath}' after retries:\", readFailure)", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FileStateSyncNotifier), BuildSyncWarningMessage(\"Unexpected sync publish failure:\", ex));", source);
        Assert.Contains("var warningMessage = BuildMalformedPayloadWarning(signalPath, payload);", source);
        Assert.Contains("private static string BuildMalformedPayloadWarning(string signalPath, string payload)", source);
        Assert.Contains("var normalizedSignalPath = NormalizePayloadForLog(signalPath);", source);
        Assert.Contains("var normalizedPayload = NormalizePayloadForLog(payload);", source);
        Assert.Contains("return $\"Ignored malformed sync payload from '{normalizedSignalPath}': \\\"{normalizedPayload}\\\"\";", source);
        Assert.Contains("CrashFileLogger.WriteInfo(nameof(FileStateSyncNotifier), $\"Signal observed. Source={normalizedSource} TimestampUtc={timestamp:O}\");", source);
    }

    [Fact]
    public void FileStateSyncNotifier_WriteFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "FileStateSyncNotifier.cs");

        Assert.Contains("var normalizedSignalPath = NormalizePayloadForLog(signalPath);", source);
        Assert.Contains("CrashFileLogger.WriteInfo(nameof(FileStateSyncNotifier), $\"Signal written. Source={instanceId} Path={normalizedSignalPath}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FileStateSyncNotifier), BuildSyncWarningMessage($\"Failed to write sync payload '{normalizedSignalPath}':\", ex));", source);
    }

    [Fact]
    public void FileStateSyncNotifier_IgnoresNotifyRequestsAfterDispose()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "FileStateSyncNotifier.cs");
        Assert.Contains("if (disposed)", source);
    }

    [Fact]
    public void FileStateSyncNotifier_Dispose_DisablesWatcherBeforeReleasingIt()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "FileStateSyncNotifier.cs");
        Assert.Contains("watcher.EnableRaisingEvents = false;", source);
    }

    [Fact]
    public void CommandExecutor_ExpandsHomePathBeforeCheckingToolUsability()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "CommandExecutor.cs");
        Assert.Contains("var normalizedTool = ExpandHomePath(NormalizeToolPath(tool));", source);
        Assert.Contains("private static string NormalizeTargetForLog(string value)", source);
        Assert.Contains("var normalizedToolForLog = NormalizeTargetForLog(tool);", source);
        Assert.Contains("var normalizedUrlForLog = NormalizeTargetForLog(url);", source);
        Assert.Contains("var normalizedArgumentsForLog = NormalizeTargetForLog(arguments);", source);
        Assert.Contains("var normalizedExpandedForLog = NormalizeTargetForLog(expanded);", source);
        Assert.Contains("var pathRooted = Path.IsPathRooted(expanded);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandExecutor), $\"Path not found for '{normalizedExpandedForLog}' while rooted={pathRooted}.\");", source);
        Assert.Contains("private static string BuildFailureMessage(string prefix, Exception ex)", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
    }

    [Fact]
    public void CommandExecutor_FailureBreadcrumbs_ArePresent_ForProcessAndResolutionFailures()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "CommandExecutor.cs");

        Assert.Contains("var warningMessage = BuildFailureMessage($\"Launch target resolution failed for '{normalizedArgumentsForLog}':\", ex);", source);
        Assert.Contains("var resultMessage = BuildFailureMessage(\"Launch target resolution failed:\", ex);", source);
        Assert.Contains("var failureMessage = BuildFailureMessage(failurePrefix, ex);", source);
        Assert.Contains("return Task.FromResult(StartProcess(psi, \"Executed.\", $\"Process launch failed for tool '{normalizedToolForLog}'.\"));", source);
        Assert.Contains("var failureMessage = BuildNoProcessHandleMessage(failurePrefix, startInfo.FileName, startInfo.UseShellExecute);", source);
        Assert.Contains("private static string BuildNoProcessHandleMessage(string failurePrefix, string fileName, bool useShellExecute)", source);
        Assert.Contains("var normalizedFileName = NormalizeTargetForLog(fileName);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandExecutor), warningMessage);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandExecutor), failureMessage);", source);
        Assert.Contains("return $\"{failurePrefix} No process handle was returned for '{normalizedFileName}' while useShellExecute={useShellExecute}.\";", source);
    }

    [Fact]
    public void DbErrorLogger_WritesCrashFileBeforeEnqueueingDatabaseWrites()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "DbErrorLogger.cs");

        AssertMethodContainsInOrder(source,
            "public void Log(Exception exception, string context)",
            "var normalizedContext = CrashFileLogger.NormalizeContext(context);",
            "CrashFileLogger.WriteException($\"ERROR [{normalizedContext}]\", exception);",
            "pendingWrites.Enqueue(entry);");
        AssertMethodContainsInOrder(source,
            "public void LogWarning(string message, string context)",
            "var normalizedContext = CrashFileLogger.NormalizeContext(context);",
            "CrashFileLogger.WriteWarning(normalizedContext, normalizedMessage);",
            "pendingWrites.Enqueue(entry);");
        Assert.Contains("var normalizedMessage = NormalizeMessagePayload(message);", source);
        AssertMethodContainsInOrder(source,
            "public void LogInfo(string message, string context)",
            "var normalizedContext = CrashFileLogger.NormalizeContext(context);",
            "CrashFileLogger.WriteInfo(normalizedContext, normalizedMessage);",
            "pendingWrites.Enqueue(entry);");
        Assert.Contains("var normalizedMessage = NormalizeMessagePayload(message);", source);
    }

    [Fact]
    public void DbErrorLogger_DrainFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "DbErrorLogger.cs");

        Assert.Equal(4, CountOccurrences(source, "var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);"));
        Assert.Contains("CrashFileLogger.WriteException(nameof(DbErrorLogger), ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(DbErrorLogger), $\"Drain loop failed unexpectedly ({ex.GetType().Name}): {safeMessage}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(DbErrorLogger), $\"Flush failed unexpectedly ({ex.GetType().Name}): {safeMessage}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(DbErrorLogger), $\"Failed to purge old error logs after persisting '{entry.Context}' ({ex.GetType().Name}): {safeMessage}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(DbErrorLogger), $\"Failed to persist {entry.Level} log for '{entry.Context}': {safeMessage}\");", source);
    }

    [Fact]
    public void WindowsCommandEntryHandler_DisablesInputScopeAfterCompatibilityException()
    {
        var source = ReadRepositoryFile("Praxis", "Platforms", "Windows", "Handlers", "CommandEntryHandler.cs");
        var searchSource = ReadRepositoryFile("Praxis", "Platforms", "Windows", "Handlers", "SearchEntryHandler.cs");
        var mauiProgramSource = ReadRepositoryFile("Praxis", "MauiProgram.cs");

        Assert.Contains("WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(platformView);", source);
        Assert.Contains("WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(textBox);", source);
        Assert.Contains("private void PlatformView_LostFocus(object sender, RoutedEventArgs e)", source);
        Assert.Contains("platformView.SelectionChanged += PlatformView_SelectionChanged;", source);
        Assert.Contains("platformView.SelectionChanged -= PlatformView_SelectionChanged;", source);
        Assert.Contains("private static void PlatformView_SelectionChanged(object sender, RoutedEventArgs e)", source);
        Assert.Contains("textBox.RequestedTheme = IsInputDarkThemeActive()", source);
        Assert.Contains("Application.Current?.UserAppTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark", source);
        Assert.Contains("global::Windows.UI.Color.FromArgb(255, 0, 0, 0)", source);
        Assert.Contains("global::Windows.UI.Color.FromArgb(255, 107, 75, 176)", source);
        Assert.Contains("global::Windows.UI.Color.FromArgb(255, 82, 58, 124)", source);
        Assert.DoesNotContain("global::Windows.UI.Color.FromArgb(255, 76, 86, 96)", source);
        Assert.Contains("Praxis.App.RefreshWindowsTextSelectionResources();", source);
        Assert.Contains("TextControlForegroundFocused", source);
        Assert.Contains("TextControlForegroundPointerOver", source);
        Assert.Contains("textBox.Background = chrome;", source);
        Assert.Contains("textBox.BorderBrush = chrome;", source);
        Assert.Contains("textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"TextControlBackground\", chrome);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"TextControlBackgroundPointerOver\", chrome);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"TextControlBackgroundFocused\", chrome);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"TextControlBorderBrush\", chrome);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"TextControlBorderBrushPointerOver\", chrome);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"TextControlBorderBrushFocused\", chrome);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"TextControlSelectionHighlightColor\", selection);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"TextControlSelectionHighlightForeground\", selectionForeground);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"TextSelectionHighlightColorThemeBrush\", selection);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"AccentFillColorSelectedTextBackgroundBrush\", selection);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"TextOnAccentFillColorSelectedText\", selectionForegroundColor);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"TextOnAccentFillColorSelectedTextBrush\", selectionForeground);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"SystemColorHighlightTextColor\", selectionForegroundColor);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"SystemColorHighlightTextColorBrush\", selectionForeground);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"TextControlCaretBrush\", caret);", source);
        Assert.Contains("TrySetTextControlResource(textBox, \"TextControlCaretBrushFocused\", caret);", source);
        Assert.Contains("return global::Windows.UI.Color.FromArgb(255, 255, 255, 255);", source);
        Assert.Contains("public class SearchEntryHandler : EntryHandler", searchSource);
        Assert.Contains("WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(platformView);", searchSource);
        Assert.Contains("WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(textBox);", searchSource);
        Assert.Contains("platformView.LostFocus += PlatformView_LostFocus;", searchSource);
        Assert.Contains("platformView.LostFocus -= PlatformView_LostFocus;", searchSource);
        Assert.Contains("platformView.SelectionChanged += PlatformView_SelectionChanged;", searchSource);
        Assert.Contains("platformView.SelectionChanged -= PlatformView_SelectionChanged;", searchSource);
        Assert.Contains("private static void PlatformView_LostFocus(object sender, RoutedEventArgs e)", searchSource);
        Assert.Contains("private static void PlatformView_SelectionChanged(object sender, RoutedEventArgs e)", searchSource);
        Assert.Contains("handlers.AddHandler(typeof(SearchEntry), typeof(SearchEntryHandler));", mauiProgramSource);
        Assert.Contains("catch (Exception ex) when (WindowsInputScopeCompatibilityPolicy.ShouldDisableInputScopeOnException(ex))", source);
        Assert.Contains("catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)", source);
        Assert.Contains("inputScopeUnsupported = true;", source);
        Assert.Equal(2, CountOccurrences(source, "var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);"));
        Assert.Equal(2, CountOccurrences(source, "var enforceAsciiInput = (VirtualView as CommandEntry)?.EnforceAsciiInput ?? false;"));
        Assert.Equal(2, CountOccurrences(source, "var textBoxType = textBox.GetType().Name;"));
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandEntryHandler), $\"InputScope assignment disabled after compatibility failure while enforceAsciiInput={enforceAsciiInput} textBoxType={textBoxType}: {safeMessage}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandEntryHandler), $\"InputScope assignment failed unexpectedly while enforceAsciiInput={enforceAsciiInput} textBoxType={textBoxType}: {safeMessage}\");", source);
        Assert.Contains("catch", source);
        Assert.Contains("return false;", source);
    }

    [Fact]
    public void MainPage_WindowsReflectionAndFocusFallbackFailures_AreWarningLogged()
    {
        var focusSource = ReadRepositoryFile("Praxis", "MainPage.FocusAndContext.cs");
        var pointerSource = ReadRepositoryFile("Praxis", "MainPage.PointerAndSelection.cs");
        var layoutSource = ReadRepositoryFile("Praxis", "MainPage.LayoutUtilities.cs");

        Assert.Contains("var controlType = control.GetType().Name;", focusSource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", focusSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(DisableWindowsSystemFocusVisual), $\"Failed to disable UseSystemFocusVisuals on {controlType}: {safeMessage}\");", focusSource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", pointerSource);
        Assert.Contains("var shouldSelectAll = modalPrimaryFieldSelectAllPending;", pointerSource);
        Assert.Contains("var modalVisible = EditorOverlay.IsVisible;", pointerSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FocusModalPrimaryEditorField), $\"Failed to focus modal ButtonText entry while shouldSelectAll={shouldSelectAll} modalVisible={modalVisible}: {safeMessage}\");", pointerSource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", layoutSource);
        Assert.Contains("var targetType = platformView.GetType().Name;", layoutSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(SetTabStop), $\"Failed to set IsTabStop={isTabStop} on {targetType}: {safeMessage}\");", layoutSource);
    }

    [Fact]
    public void WindowsStartupLog_UsesNormalizedAppStorageRoot_AndGuardsDuplicateHookRegistration()
    {
        var source = ReadRepositoryFile("Praxis", "Platforms", "Windows", "App.xaml.cs");

        Assert.Contains("AppStoragePaths.WindowsLocalAppDataRoot", source);
        Assert.Contains("private static bool globalExceptionLoggingHooked;", source);
        Assert.Contains("if (globalExceptionLoggingHooked)", source);
        Assert.Contains("var safePayload = CrashFileLogger.SafeObjectDescription(e.ExceptionObject);", source);
        Assert.Contains("var payloadType = e.ExceptionObject?.GetType().FullName ?? \"null\";", source);
        Assert.Contains("var payload = $\"Non-Exception object thrown (IsTerminating={e.IsTerminating}, Type={payloadType}): {safePayload}\";", source);
        Assert.Contains("var content = BuildStartupExceptionLogContent(source, exception);", source);
        Assert.Contains("var content = BuildStartupMessageLogContent(source, message);", source);
        Assert.Contains("sb.Append(CrashFileLogger.FormatExceptionPayload(exception));", source);
        Assert.Contains("private static string BuildStartupMessageLogContent(string source, string message)", source);
        Assert.Contains("AppendStartupLogContent(content);", source);
        Assert.DoesNotContain("exception.ToString()", source, StringComparison.Ordinal);
        Assert.Equal(5, CountOccurrences(source, "SecondaryFailureLogger.ReportStartupLogFailure("));
        Assert.Contains("SecondaryFailureLogger.ReportStartupLogFailure(", source);
        Assert.Contains("\"Failed to create startup log directory\"", source);
        Assert.Contains("\"Failed to append startup log\"", source);
        Assert.Contains("\"Failed to build startup log payload for\"", source);
        Assert.DoesNotContain("CrashFileLogger.WriteWarning(nameof(App), $\"Failed to append startup log", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SecondaryFailureLogger_PreservesOriginalStartupMessages_EvenWhenWhitespaceOnly()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "SecondaryFailureLogger.cs");

        Assert.Contains("private static string NormalizePathForLog(string path)", source);
        Assert.Contains("private static string NormalizeOperationForLog(string value)", source);
        Assert.Contains("var normalizedTargetPath = NormalizePathForLog(targetPath);", source);
        Assert.Contains("var normalizedOperation = NormalizeOperationForLog(operationDescription);", source);
        Assert.Contains("var warningMessage = $\"{normalizedOperation} '{normalizedTargetPath}': {safeMessage}\";", source);
        Assert.Contains("else if (originalMessage is not null)", source);
        Assert.Contains("CrashFileLogger.NormalizeMessagePayload(originalMessage)", source);
        Assert.DoesNotContain("else if (!string.IsNullOrWhiteSpace(originalMessage))", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MacAppDelegate_GuardsDuplicateGlobalExceptionHookRegistration()
    {
        var source = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "AppDelegate.cs");

        Assert.Contains("private static bool globalExceptionLoggingHooked;", source);
        Assert.Contains("if (globalExceptionLoggingHooked)", source);
        Assert.Contains("globalExceptionLoggingHooked = true;", source);
        Assert.Contains("var safePayload = CrashFileLogger.SafeObjectDescription(e.ExceptionObject);", source);
        Assert.Contains("var payloadType = e.ExceptionObject?.GetType().FullName ?? \"null\";", source);
        Assert.Equal(2, CountOccurrences(source, "var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);"));
        Assert.Contains("\"Non-Exception object thrown (IsTerminating={e.IsTerminating}, Type={payloadType}): {safePayload}\"", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(AppDelegate), $\"Failed to hook MarshalManagedException: {safeMessage}\");", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(AppDelegate), $\"Failed to prioritize key command '{selectorName}': {safeMessage}\");", source);
    }

    [Fact]
    public void MainViewModel_ExternalThemeSync_DispatchFailuresAreLogged()
    {
        var source = ReadRepositoryFile("Praxis", "ViewModels", "MainViewModel.cs");

        Assert.Contains("var currentThemeForLog = SelectedTheme;", source);
        Assert.Contains("errorLogger.Log(ex, nameof(SyncThemeFromExternalChangeAsync));", source);
        Assert.Contains("BuildSafeWarningMessage($\"External theme sync dispatch failed for theme {latestTheme}\", ex)", source);
        Assert.Contains("BuildSafeWarningMessage($\"External theme sync failed for theme {currentThemeForLog}\", ex)", source);
        Assert.Contains("BuildSafeWarningMessage(\"External reload failed\", ex)", source);
        Assert.Contains("TaskCreationOptions.RunContinuationsAsynchronously", source);
        Assert.Contains("private static string BuildSafeWarningMessage(string prefix, Exception ex)", source);
        Assert.Contains("private static string BuildSafeWarningMessage(Func<Exception, string> warningFactory, Exception ex)", source);
    }

    [Fact]
    public void MainViewModel_CommandSuggestionDispatchFailures_AreLogged()
    {
        var source = ReadRepositoryFile("Praxis", "ViewModels", "MainViewModel.CommandSuggestions.cs");

        Assert.Contains("errorLogger.Log(ex, nameof(DebouncedRefreshCommandSuggestionsAsync));", source);
        Assert.Contains("errorLogger.Log(ex, nameof(RefreshCommandSuggestionsOnMainThread));", source);
        Assert.Contains("var commandInputLength = CommandInput?.Length ?? 0;", source);
        Assert.Contains("BuildSafeWarningMessage($\"Debounced command suggestion refresh failed for input length {commandInputLength}\", ex)", source);
        Assert.Contains("BuildSafeWarningMessage($\"Command suggestion close dispatch failed for input length {commandInputLength}\", dispatchEx)", source);
        Assert.Contains("var valueLength = value?.Length ?? 0;", source);
        Assert.Contains("BuildSafeWarningMessage($\"Command suggestion refresh dispatch failed for input length {valueLength}\", ex)", source);
        Assert.Contains("BuildSafeWarningMessage($\"Command suggestion refresh failed for input length {value?.Length ?? 0}\", ex)", source);
        Assert.Contains("BuildSafeWarningMessage($\"Command lookup fallback failed for input length {cmd.Length}\", ex)", source);
        Assert.Contains("catch (OperationCanceledException) when (token.IsCancellationRequested)", source);
    }

    [Fact]
    public void MainViewModel_ActionsWarningHelpers_UseSafeWarningMessageBuilder()
    {
        var source = ReadRepositoryFile("Praxis", "ViewModels", "MainViewModel.Actions.cs");

        Assert.Contains("private async Task<string> TryGetClipboardTextAsync(string context, string operation)", source);
        Assert.Contains("async () => await clipboardService.GetTextAsync() ?? string.Empty,", source);
        Assert.Contains("BuildSafeWarningMessage(\"Conflict resolution callback failed\", ex)", source);
        Assert.Contains("ex => BuildSafeWarningMessage($\"{operation} failed\", ex)", source);
        Assert.Contains("ex => BuildSafeWarningMessage($\"{operation} completed locally, but window sync notification failed\", ex)", source);
        Assert.Contains("ex => BuildSafeWarningMessage($\"{operation} applied locally, but theme persistence failed\", ex)", source);
        Assert.Contains("ex => BuildSafeWarningMessage($\"{operation} completed locally, but dock persistence failed\", ex)", source);
        Assert.Equal(2, CountOccurrences(source, "errorLogger.LogWarning(BuildSafeWarningMessage(warningFactory, ex), context);"));
    }

    [Fact]
    public void AppStoragePaths_LegacyMigrationFailures_AreWarningLogged_AndSkipped()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "AppStoragePaths.cs");

        Assert.Contains("private static string NormalizePathForLog(string path)", source);
        Assert.Contains("var normalizedSourcePath = NormalizePathForLog(sourcePath);", source);
        Assert.Contains("var normalizedLeft = NormalizePathForLog(left);", source);
        Assert.Contains("var normalizedRight = NormalizePathForLog(right);", source);
        Assert.Contains("private static string BuildSafeWarningMessage(string prefix, Exception ex)", source);
        Assert.Contains("=> $\"{prefix} ({ex.GetType().Name}): {CrashFileLogger.SafeExceptionMessage(ex)}\";", source);
        Assert.Equal(2, CountOccurrences(source, "CrashFileLogger.WriteWarning(nameof(AppStoragePaths), BuildSafeWarningMessage($\"Legacy database migration failed from '{normalizedSourcePath}'\", ex));"));
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(AppStoragePaths), BuildSafeWarningMessage($\"Ignoring invalid migration path comparison between '{normalizedLeft}' and '{normalizedRight}'\", ex));", source);
    }

    [Fact]
    public void FileAppConfigService_FallsBackOnUnauthorizedAccess()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "FileAppConfigService.cs");
        Assert.Contains("catch (UnauthorizedAccessException ex)", source);
        Assert.Contains("private static string NormalizePathForLog(string path)", source);
        Assert.Contains("var normalizedPath = NormalizePathForLog(path);", source);
        Assert.Contains("private static void WriteSkippedConfigWarning(string path, Exception ex)", source);
        Assert.Contains("var exceptionType = ex.GetType().Name;", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FileAppConfigService), $\"Skipping config '{normalizedPath}' after {exceptionType}: {safeMessage}\");", source);
        Assert.Contains("var normalizedTheme = NormalizeThemeForLog(config?.Theme);", source);
        Assert.Contains("$\"Skipping config '{normalizedPath}' because it does not specify a valid theme. Value='{normalizedTheme}'.\"", source);
    }

    [Fact]
    public void MauiThemeService_SkipsNoOpApplies_AndCrashLogsDispatchFailures()
    {
        var source = ReadRepositoryFile("Praxis", "Services", "MauiThemeService.cs");

        Assert.Contains("if (Application.Current.UserAppTheme == appTheme)", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("var currentTheme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MauiThemeService), $\"ApplyMacWindowStyle dispatch failed for theme '{appTheme}' while currentTheme='{currentTheme}': {safeMessage}\");", source);
    }

    [Fact]
    public void MacProgram_RelayFailures_AreWarningLogged_AndNullProcessIsRejected()
    {
        var source = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "Program.cs");

        Assert.Contains("private static string NormalizePathForLog(string path)", source);
        Assert.Equal(2, CountOccurrences(source, "var normalizedBundlePath = NormalizePathForLog(bundlePath);"));
        Assert.Equal(2, CountOccurrences(source, "var normalizedRelayExecutable = NormalizePathForLog(relayExecutable);"));
        Assert.Contains("var process = Process.Start(startInfo);", source);
        Assert.Contains("if (process is null)", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(Program), $\"LaunchServices relay returned no process for bundle '{normalizedBundlePath}' via '{normalizedRelayExecutable}'.\");", source);
        Assert.Contains("var normalizedRelayArg = NormalizePathForLog(OpenRelayArg);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(Program), $\"LaunchServices relay failed for bundle '{normalizedBundlePath}' via '{normalizedRelayExecutable}' with relayArg='{normalizedRelayArg}': {safeMessage}\");", source);
    }

    [Fact]
    public void MacHandlers_KeyInputResolutionFailures_AreWarningLogged_AndFallBack()
    {
        var macEntrySource = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "Handlers", "MacEntryHandler.cs");
        var macEditorSource = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "Handlers", "MacEditorHandler.cs");
        var commandEntrySource = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "Handlers", "CommandEntryHandler.cs");

        Assert.Contains("return TryResolveKeyInput(inputName, fallback) ?? fallback;", macEntrySource);
        Assert.Contains("var normalizedInputName = CrashFileLogger.NormalizeMessagePayload(inputName);", macEntrySource);
        Assert.Contains("var normalizedFallback = DescribeKeyInputFallbackForLog(fallbackForLog);", macEntrySource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", macEntrySource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MacEntryHandler), $\"Failed to resolve UIKeyCommand input '{normalizedInputName}' with fallback '{normalizedFallback}': {safeMessage}\");", macEntrySource);
        Assert.Contains("\"\\t\" => \"Tab\",", macEntrySource);
        Assert.Contains("\"\\u001B\" => \"Escape\",", macEntrySource);
        Assert.Contains("\"\\r\" => \"Return\",", macEntrySource);
        Assert.Contains("\"\\uF700\" => \"UpArrow\",", macEntrySource);
        Assert.Contains("\"\\uF701\" => \"DownArrow\",", macEntrySource);
        Assert.Contains("\"\\uF702\" => \"LeftArrow\",", macEntrySource);
        Assert.Contains("\"\\uF703\" => \"RightArrow\",", macEntrySource);

        Assert.Contains("return TryResolveKeyInput(inputName, fallback) ?? fallback;", macEditorSource);
        Assert.Contains("var normalizedInputName = CrashFileLogger.NormalizeMessagePayload(inputName);", macEditorSource);
        Assert.Contains("var normalizedFallback = DescribeKeyInputFallbackForLog(fallbackForLog);", macEditorSource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", macEditorSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MacEditorHandler), $\"Failed to resolve UIKeyCommand input '{normalizedInputName}' with fallback '{normalizedFallback}': {safeMessage}\");", macEditorSource);
        Assert.Contains("\"\\t\" => \"Tab\",", macEditorSource);
        Assert.Contains("\"\\u001B\" => \"Escape\",", macEditorSource);
        Assert.Contains("\"\\uF702\" => \"LeftArrow\",", macEditorSource);
        Assert.Contains("\"\\uF703\" => \"RightArrow\",", macEditorSource);

        Assert.Contains("return TryResolveKeyInput(inputName, fallback) ?? fallback;", commandEntrySource);
        Assert.Contains("var normalizedInputName = CrashFileLogger.NormalizeMessagePayload(inputName);", commandEntrySource);
        Assert.Contains("var normalizedFallback = DescribeKeyInputFallbackForLog(fallbackForLog);", commandEntrySource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", commandEntrySource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(CommandEntryHandler), $\"Failed to resolve UIKeyCommand input '{normalizedInputName}' with fallback '{normalizedFallback}': {safeMessage}\");", commandEntrySource);
    }

    [Fact]
    public void MacGlassEntries_KeepOnlyFocusedUnderlineForGlassChrome()
    {
        var macEntrySource = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "Handlers", "MacEntryHandler.cs");
        var commandEntrySource = ReadRepositoryFile("Praxis", "Platforms", "MacCatalyst", "Handlers", "CommandEntryHandler.cs");
        var macPageSource = ReadRepositoryFile("Praxis", "MainPage.MacCatalystBehavior.cs");

        Assert.Contains("private bool glassFieldVisual;", macEntrySource);
        Assert.Contains("borderLayer.StrokeColor = glassFieldVisual ? TransparentBorderColor : borderColor;", macEntrySource);
        Assert.Contains("focusBorderLayer.Hidden = !(IsFirstResponder || pseudoFocused);", macEntrySource);
        Assert.Contains("ApplyGlassFieldBackground(dark);", macEntrySource);
        Assert.Contains("private UIView? glassBackdropView;", macEntrySource);
        Assert.Contains("glassBackdropView ??= new UIView", macEntrySource);
        Assert.Contains("effectView.Effect = null;", macEntrySource);
        Assert.DoesNotContain("UIBlurEffect.FromStyle", macEntrySource);
        Assert.Contains("Layer.BackgroundColor = UIColor.Clear.CGColor;", macEntrySource);
        Assert.Contains("LightPlaceholderColor = UIColor.FromRGBA(0, 0, 0, 0.58f);", macEntrySource);
        Assert.Contains("DarkPlaceholderColor = UIColor.FromRGBA(196, 212, 224, 0.42f);", macEntrySource);
        Assert.Contains("LightTextColor = UIColor.Black;", macEntrySource);
        Assert.Contains("DarkTextColor = UIColor.White;", macEntrySource);
        Assert.Contains("LightTextSelectionColor = UIColor.FromRGB(0x4B, 0x00, 0xD9);", macEntrySource);
        Assert.Contains("DarkTextSelectionColor = UIColor.FromRGB(0x35, 0x00, 0xA8);", macEntrySource);
        Assert.Contains("var selectionColor = dark ? DarkTextSelectionColor : LightTextSelectionColor;", macEntrySource);
        Assert.Contains("TintColor = selectionColor;", macEntrySource);
        Assert.Contains("ContentScaleFactor = UIScreen.MainScreen.Scale;", macEntrySource);
        Assert.Contains("Layer.ContentsScale = UIScreen.MainScreen.Scale;", macEntrySource);
        Assert.Contains("Background = TransparentFieldImage;", macEntrySource);
        Assert.Contains("Layer.MasksToBounds = false;", macEntrySource);
        Assert.Contains("private static UIImage CreateTransparentFieldImage()", macEntrySource);
        Assert.Contains("var renderer = new UIGraphicsImageRenderer(new CGSize(1, 1));", macEntrySource);
        Assert.Contains("renderer.CreateImage(_ => { });", macEntrySource);
        Assert.Contains("public override CGRect BorderRect(CGRect forBounds)", macEntrySource);
        Assert.Contains("public override void Draw(CGRect rect)", macEntrySource);
        Assert.DoesNotContain("UIGraphics.GetCurrentContext()?.ClearRect(rect);", macEntrySource);
        Assert.DoesNotContain("currentGlassFieldBackground", macEntrySource);
        Assert.Contains("public override void MovedToSuperview()", macEntrySource);
        Assert.Contains("public override void MovedToWindow()", macEntrySource);
        Assert.Contains("ClearNativeGlassHostBackground(this, glassBackdropView);", macEntrySource);
        Assert.Contains("ClearNativeGlassSubviewBackground(subview, preservedBackdrop);", macEntrySource);
        Assert.Contains("ClearNativeGlassHostLayers();", macEntrySource);
        Assert.Contains("ClearNativeGlassWrapperBackgrounds();", macEntrySource);
        Assert.Contains("private bool IsLikelyNativeInputWrapper(UIView view)", macEntrySource);
        Assert.Contains("BorderStyle = UITextBorderStyle.None;", macEntrySource);
        Assert.Contains("AttributedPlaceholder = new NSAttributedString(", macEntrySource);
        Assert.Contains("var placeholderColor = glassFieldVisual ? UIColor.Clear", macEntrySource);
        Assert.Contains("Font = placeholderFont,", macEntrySource);
        Assert.Contains("UIFontWeight.Regular", macEntrySource);
        Assert.Contains("public void SetGlassFieldVisual(bool enabled)", macEntrySource);
        AssertMethodContainsInOrder(commandEntrySource,
            "internal void TryApplyNativeActivationFocus()",
            "SelectAllText();",
            "ApplyFocusVisualState();",
            "RefreshInputSourceEnforcementState();");
        Assert.Contains("macEntryTextField.SetGlassFieldVisual(IsGlassEntryVisual(entry));", macPageSource);
        Assert.Contains("var dark = IsMacGlassDarkThemeActive();", macPageSource);
        Assert.Contains("ThemeMode.Dark => true,", macPageSource);
        Assert.Contains("ThemeMode.Light => false,", macPageSource);
        Assert.Contains("ScheduleMacRootTransparencyRefresh();", macPageSource);
        Assert.Contains("private void ApplyMacRootTransparency()", macPageSource);
        Assert.Contains("App.RefreshMacWindowBackdropForConnectedScenes();", macPageSource);
        Assert.Contains("private void RefreshMacWindowBackdropAndRootTransparency()", macPageSource);
        Assert.Contains("ClearMacLargeContainerBackgrounds(pageView, rootSize, 0);", macPageSource);
        Assert.Contains("private const nint MacMaterialFrameBackdropTag = 0x50475842;", macPageSource);
        Assert.Contains("if (view is UIVisualEffectView effectView)", macPageSource);
        Assert.Contains("effectView.Effect = null;", macPageSource);
        Assert.Contains("view is UIControl or UILabel or UITextField or UITextView or UIImageView", macPageSource);
        Assert.Contains("private static bool IsIntentionalMacGlassBackdrop(UIView view)", macPageSource);
        Assert.Contains("ApplyMacModalTextEditorVisualState(textView);", macPageSource);
        Assert.Contains("EnsureMacModalTextEditorGlassBackdrop(textView);", macPageSource);
        Assert.Contains("ClearMacGlassTextEditorBackgrounds(textView, backdropView);", macPageSource);
        Assert.Contains("ClearMacGlassTextEditorSubviewBackground(subview, preservedBackdrop);", macPageSource);
        Assert.Contains("ClearMacGlassTextEditorLayers(textView, backdropView);", macPageSource);
        Assert.Contains("ClearMacGlassTextEditorWrapperBackgrounds(textView);", macPageSource);
        Assert.Contains("textView.BackgroundColor = UIColor.Clear;", macPageSource);
        Assert.Contains("textView.Layer.BackgroundColor = UIColor.Clear.CGColor;", macPageSource);
        Assert.Contains("private static bool IsLikelyMacGlassTextEditorWrapper(UITextView textView, UIView view)", macPageSource);
        Assert.Contains("private static void ApplyMacCrispTextRendering(UIView view, nfloat scale)", macPageSource);
        Assert.Contains("view.Layer.ShouldRasterize = false;", macPageSource);
        Assert.Contains("label.Font = UIFont.SystemFontOfSize(label.Font.PointSize, UIFontWeight.Medium);", macPageSource);
        Assert.Contains("textField.Font = UIFont.SystemFontOfSize(textField.Font.PointSize, UIFontWeight.Medium);", macPageSource);
        Assert.Contains("private void ApplyMacModalButtonVisualState()", macPageSource);
        Assert.Contains("private void ScheduleMacModalButtonVisualStateRefresh()", macPageSource);
        Assert.Contains("Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(16), ApplyMacModalButtonVisualState);", macPageSource);
        Assert.Contains("Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(120), ApplyMacModalButtonVisualState);", macPageSource);
        Assert.Contains("ApplyMacGlassButtonVisual(CopyGuidButton);", macPageSource);
        Assert.Contains("ApplyMacGlassButtonVisual(ModalSaveButton);", macPageSource);
        Assert.Contains("ScheduleMacModalButtonVisualStateRefresh();", ReadRepositoryFile("Praxis", "MainPage.ModalEditor.cs"));
        Assert.Contains("ScheduleMacModalButtonVisualStateRefresh();", ReadRepositoryFile("Praxis", "MainPage.ViewModelEvents.cs"));
        Assert.Contains("private void ApplyMacGlassButtonVisual(Border button)", macPageSource);
        Assert.Contains("button.BackgroundColor = dark ? Color.FromArgb(\"#9A53606B\") : Color.FromArgb(\"#8AFFFFFF\");", macPageSource);
        Assert.Contains("button.Stroke = new SolidColorBrush(Colors.Transparent);", macPageSource);
        Assert.Contains("button.StrokeThickness = 0;", macPageSource);
        Assert.DoesNotContain("nativeButton.Configuration = null;", macPageSource);
        Assert.DoesNotContain("ClearMacGlassButtonSubviews(nativeButton);", macPageSource);
        Assert.Contains("titleLabel.Font = UIFont.SystemFontOfSize(titleLabel.Font.PointSize, UIFontWeight.Medium);", macPageSource);
        Assert.Contains("private bool IsGlassEntryVisual(Entry entry)", macPageSource);
        Assert.Contains("ReferenceEquals(entry, MainCommandEntry) ||", macPageSource);
        Assert.Contains("ReferenceEquals(entry, MainSearchEntry) ||", macPageSource);
        Assert.Contains("ReferenceEquals(entry, ModalGuidEntry) ||", macPageSource);
        Assert.Contains("ReferenceEquals(entry, ModalButtonTextEntry) ||", macPageSource);
        Assert.Contains("ReferenceEquals(entry, ModalCommandEntry) ||", macPageSource);
        Assert.Contains("ReferenceEquals(entry, ModalToolEntry) ||", macPageSource);
        Assert.Contains("ReferenceEquals(entry, ModalArgumentsEntry);", macPageSource);
    }

    [Fact]
    public void MacMiddleClickAndKeyCommandFallbackFailures_AreWarningLogged()
    {
        var behaviorSource = ReadRepositoryFile("Praxis", "Behaviors", "MiddleClickBehavior.cs");
        var macSource = ReadRepositoryFile("Praxis", "MainPage.MacCatalystBehavior.cs");

        Assert.Equal(2, CountOccurrences(behaviorSource, "var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);"));
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MiddleClickBehavior), $\"Failed to set buttonMaskRequired={mask}: {safeMessage}\");", behaviorSource);
        Assert.Contains("var isContextMenuOpen = IsContextMenuCurrentlyOpen();", behaviorSource);
        Assert.Contains("var hasCommand = Command is not null;", behaviorSource);
        Assert.Contains("var associatedObjectType = attachedView?.GetType().Name ?? \"(null)\";", behaviorSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(MiddleClickBehavior), $\"Deferred middle-click execution failed while contextMenuOpen={isContextMenuOpen} hasCommand={hasCommand} associatedObjectType={associatedObjectType}: {safeMessage}\");", behaviorSource);
        Assert.Equal(3, CountOccurrences(macSource, "var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);"));
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(TryCreateMacEditorKeyCommand), $\"Failed to create Mac editor key command '{selectorName}' for input '{keyInput}': {safeMessage}\");", macSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(TrySetMacPlacementButtonMaskRequired), $\"Failed to set placement buttonMaskRequired={mask}: {safeMessage}\");", macSource);
        Assert.Contains("var isActive = App.IsMacApplicationActive();", macSource);
        Assert.Contains("var activationSuppressed = App.IsActivationSuppressionActive();", macSource);
        Assert.Contains("var pointerKnown = macLastActivePage is not null &&", macSource);
        Assert.Contains("(page.lastPointerOnRoot is not null || page.macPlacementHoverRootPoint is not null);", macSource);
        Assert.Contains("if (CGEventSource.GetButtonState(CGEventSourceStateID.HidSystem, button))", macSource);
        Assert.Contains("if (CGEventSource.GetButtonState(CGEventSourceStateID.CombinedSession, button))", macSource);
        Assert.Contains("return true;", macSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(IsMacMouseButtonCurrentlyDown), $\"Failed to query mouse button state from CoreGraphics for button={button} while isActive={isActive} activationSuppressed={activationSuppressed} pointerKnown={pointerKnown}: {safeMessage}\");", macSource);
    }

    [Fact]
    public void MainPage_MacPlacementScroll_UsesNativeRecognizersForEmptySpaceActions()
    {
        var fieldsSource = ReadRepositoryFile("Praxis", "MainPage.Fields.MacCatalyst.cs");
        var macSource = ReadRepositoryFile("Praxis", "MainPage.MacCatalystBehavior.cs");
        var mainPageSource = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        var pointerSource = ReadRepositoryFile("Praxis", "MainPage.PointerAndSelection.cs");
        var editorSource = ReadRepositoryFile("Praxis", "MainPage.EditorAndInput.cs");
        var interactionSource = ReadRepositoryFile("Praxis", "MainPage.InteractionState.cs");

        Assert.Contains("private UIView? macPlacementGestureNativeView;", fieldsSource);
        Assert.Contains("private UIHoverGestureRecognizer? macPlacementHoverRecognizer;", fieldsSource);
        Assert.Contains("private UILongPressGestureRecognizer? macPlacementPrimarySelectionRecognizer;", fieldsSource);
        Assert.Contains("private UILongPressGestureRecognizer? macPlacementSecondaryCreateRecognizer;", fieldsSource);
        Assert.Contains("private bool macPlacementNativeSelectionActive;", fieldsSource);
        Assert.Contains("private bool macPlacementNativeSelectionIgnored;", fieldsSource);
        Assert.Contains("private Point? macPlacementNativeSelectionStartRootPoint;", fieldsSource);
        Assert.Contains("private bool macPlacementPollingSelectionActive;", fieldsSource);
        Assert.Contains("private long macPlacementDeferredPrimaryDownId;", fieldsSource);
        Assert.Contains("private DateTimeOffset? macPlacementPrimaryReleaseStartedAtUtc;", fieldsSource);
        Assert.Contains("private MacAppKitRootPointKind? macPlacementPollingAppKitRootPointKind;", fieldsSource);
        Assert.Contains("private Point? macPlacementPollingStartRootPoint;", fieldsSource);
        Assert.Contains("private Point? macPlacementPollingRawStartScreen;", fieldsSource);
        Assert.Contains("private MacPointerScreenPointKind? macPlacementPollingScreenPointKind;", fieldsSource);
        Assert.Contains("private Point? macPlacementPollingAnchorViewport;", fieldsSource);
        Assert.Contains("private UIView? macPlacementSelectionOverlayView;", fieldsSource);
        Assert.Contains("private int macPlacementSelectionOverlayFadeRevision;", fieldsSource);
        Assert.Contains("private Guid? macPlacementPollingCommandSelectionItemId;", fieldsSource);
        Assert.Contains("private Point? macPlacementHoverRootPoint;", fieldsSource);
        Assert.Contains("private DateTimeOffset macPlacementHoverRootPointUpdatedAtUtc;", fieldsSource);
        Assert.Contains("private readonly UIGestureRecognizerDelegate macPlacementGestureDelegate = new MacPlacementGestureDelegate();", fieldsSource);
        Assert.Contains("private static readonly bool MacPlacementPrimarySelectionUsesPolling = false;", macSource);
        Assert.Contains("private static readonly TimeSpan MacPlacementRecentHoverWindow = TimeSpan.FromMilliseconds(250);", macSource);
        Assert.Contains("private static readonly TimeSpan MacPlacementPrimaryReleaseGraceWindow = TimeSpan.FromMilliseconds(80);", macSource);
        Assert.Contains("ScheduleMacPlacementCanvasNativeGesturesRefresh();", macSource);
        Assert.Contains("RootGrid.HandlerChanged += (_, _) => ScheduleMacPlacementCanvasNativeGesturesRefresh();", mainPageSource);
        Assert.Contains("PlacementSurface.HandlerChanged += (_, _) => EnsureMacPlacementCanvasNativeGestures();", mainPageSource);
        Assert.Contains("PlacementScroll.HandlerChanged += (_, _) => EnsureMacPlacementCanvasNativeGestures();", mainPageSource);
        Assert.Contains("DetachMacPlacementCanvasNativeGestures();", mainPageSource);
        Assert.Contains("ResolveMacPlacementGestureNativeView() is not UIView nativeView", macSource);
        Assert.Contains("RootGrid.Handler?.PlatformView is UIView rootView", macSource);
        Assert.Contains("rootView.Bounds.Width > 0", macSource);
        Assert.Contains("return rootView;", macSource);
        Assert.Contains("nativeView.Bounds.Width <= 0", macSource);
        Assert.Contains("PlacementScroll.Width <= 0", macSource);
        Assert.Contains("nativeView.UserInteractionEnabled = true;", macSource);
        Assert.Contains("macPlacementHoverRecognizer = CreateMacPlacementHoverRecognizer();", macSource);
        Assert.Contains("nativeView.AddGestureRecognizer(macPlacementHoverRecognizer);", macSource);
        Assert.Contains("new UIHoverGestureRecognizer(HandleMacPlacementHoverRecognizer)", macSource);
        Assert.Contains("macPlacementHoverRootPoint = new Point(location.X, location.Y);", macSource);
        Assert.Contains("macPlacementHoverRootPointUpdatedAtUtc = DateTimeOffset.UtcNow;", macSource);
        Assert.Contains("lastPointerOnRoot = macPlacementHoverRootPoint;", macSource);
        Assert.Contains("SyncDockScrollBarVisibility(macPlacementHoverRootPoint.Value);", macSource);
        Assert.Contains("var rawPrimaryDown = IsMacMouseButtonCurrentlyDown(CGMouseButton.Left);", macSource);
        Assert.Contains("var primaryDown = rawPrimaryDown;", macSource);
        Assert.Contains("macPlacementPrimaryReleaseStartedAtUtc ??= now;", macSource);
        Assert.Contains("now - macPlacementPrimaryReleaseStartedAtUtc.Value < MacPlacementPrimaryReleaseGraceWindow", macSource);
        Assert.Contains("var secondaryDown = IsMacMouseButtonCurrentlyDown(CGMouseButton.Right);", macSource);
        Assert.Contains("var middleDown = IsMacMouseButtonCurrentlyDown(CGMouseButton.Center);", macSource);
        Assert.Contains("ScheduleMacPlacementPollingSelectionStart();", macSource);
        Assert.Contains("Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(16), () =>", macSource);
        Assert.Contains("!IsMacMouseButtonCurrentlyDown(CGMouseButton.Left)", macSource);
        Assert.Contains("TryBeginMacPlacementPollingSelection();", macSource);
        Assert.Contains("ContinueMacPlacementPollingSelection();", macSource);
        Assert.Contains("CompleteMacPlacementPollingSelection();", macSource);
        Assert.Contains("TryHandleMacPlacementPollingSecondaryCreate();", macSource);
        Assert.Contains("TryHandleMacPlacementPollingButtonPress(rootPoint)", macSource);
        Assert.Contains("TryGetPlacementButtonAtRootPoint(rootPoint)", macSource);
        Assert.Contains("IsMacSelectionModifierCurrentlyDown()", macSource);
        Assert.Contains("CGEventSource.GetFlagsState(CGEventSourceStateID.HidSystem)", macSource);
        Assert.Contains("viewModel.ToggleSelection(hit);", macSource);
        Assert.Contains("suppressTapExecuteForItemId = hit.Id;", macSource);
        Assert.Contains("TryGetMacCurrentPlacementRootPointer(out var rootPoint)", macSource);
        Assert.Contains("TryGetMacPlacementHoverRootPoint(out rootPoint)", macSource);
        Assert.Contains("macPlacementHoverRecognizer.LocationInView(macPlacementGestureNativeView)", macSource);
        Assert.Contains("macPlacementHoverRecognizer.State == UIGestureRecognizerState.Began ||", macSource);
        Assert.DoesNotContain("case UIGestureRecognizerState.Failed:\n                macPlacementHoverRootPoint = null;", macSource);
        Assert.Contains("FormatMacPlacementPointerDiagnostic()", macSource);
        Assert.Contains("TryGetMacCurrentRootPointer(out var pointer)", macSource);
        Assert.Contains("lastPointerOnRoot is Point cachedRootPoint && IsMacRootPointInsideRoot(cachedRootPoint)", macSource);
        Assert.Contains("TryGetMacCurrentRootPointerFromCoreGraphics(out rootPoint, useCachedDistance)", macSource);
        Assert.Contains("TryGetMacCurrentRootPointerFromAppKit(out rootPoint, useCachedDistance)", macSource);
        Assert.Contains("Point? bestRootPoint = null;", macSource);
        Assert.Contains("using var currentEvent = new CGEvent((CGEventSource?)null);", macSource);
        Assert.Contains("var windowPoint = nativeWindow.ConvertPointFromWindow(screenPoint, null);", macSource);
        Assert.Contains("var localPoint = rootView.ConvertPointFromView(windowPoint, nativeWindow);", macSource);
        Assert.Contains("var distance = Math.Pow(candidate.Point.X - cachedRootPoint.X, 2) + Math.Pow(candidate.Point.Y - cachedRootPoint.Y, 2);", macSource);
        Assert.Contains("ObjcGetClass(\"NSEvent\")", macSource);
        Assert.Contains("SelRegisterName(\"mouseLocation\")", macSource);
        Assert.Contains("SelRegisterName(\"convertPointFromScreen:\")", macSource);
        Assert.Contains("SelRegisterName(\"frame\")", macSource);
        Assert.Contains("MacPlacementWindowMatchesUiWindow(nsWindow, nativeWindow)", macSource);
        Assert.Contains("TryIsMacCurrentPointerInsidePlacementWindow(out var pointerInsidePlacementWindow)", macSource);
        Assert.Contains("!pointerInsidePlacementWindow", macSource);
        Assert.Contains("MacAppKitWindowContainsScreenPoint(nsWindow, screenPoint)", macSource);
        Assert.Contains("private static bool MacAppKitWindowContainsScreenPoint(NSObject nsWindow, CGPoint screenPoint)", macSource);
        Assert.Contains("EnumerateMacAppKitRootPointCandidatesWithKinds(windowPoint, contentFrame, rootFrame, nativeWindowHeight)", macSource);
        Assert.Contains("private bool IsMacRootPointInsideRoot(Point rootPoint)", macSource);
        Assert.Contains("TryGetMacPlacementRootCanvasPoint(rootPoint, allowOutside: false", macSource);
        Assert.Contains("TryGetMacPlacementRootCanvasPoint(rootPoint, allowOutside, out canvasPoint, out viewportPoint)", macSource);
        Assert.Contains("allowCachedFallback: false", macSource);
        Assert.Contains("useCachedDistance: false", macSource);
        Assert.Contains("TryGetMacPlacementPollingRootPoint(out var rootPoint)", macSource);
        Assert.Contains("TryGetRecentMacPlacementHoverRootPoint(out rootPoint)", macSource);
        Assert.Contains("DateTimeOffset.UtcNow - macPlacementHoverRootPointUpdatedAtUtc <= MacPlacementRecentHoverWindow", macSource);
        Assert.Contains("ClearMacPlacementHoverRootPoint();", macSource);
        Assert.Contains("TryGetMacPlacementPollingRunningPoint(out var canvasPoint, out var viewportPoint, out var rootPoint)", macSource);
        Assert.Contains("TryGetMacPlacementPollingCurrentRootPoint(out var currentRootPoint)", macSource);
        Assert.Contains("TryGetMacCurrentRootPointerFromAppKit(out rootPoint, useCachedDistance: true)", macSource);
        Assert.Contains("TryGetMacCurrentRootPointerFromCoreGraphics(out rootPoint, useCachedDistance: true)", macSource);
        Assert.Contains("TryGetMacPlacementPrimaryRecognizerRootPoint(out var rootPoint)", macSource);
        Assert.Contains("new UILongPressGestureRecognizer(HandleMacPlacementPrimarySelectionRecognizer)", macSource);
        Assert.Contains("NumberOfTouchesRequired = 1,", macSource);
        Assert.Contains("MinimumPressDuration = 0,", macSource);
        Assert.Contains("AllowableMovement = nfloat.MaxValue,", macSource);
        Assert.Contains("private enum MacAppKitRootPointKind", macSource);
        Assert.Contains("private enum MacPointerScreenPointKind", macSource);
        Assert.Contains("TryGetMacCurrentRootPointerFromAppKit(rootPoint, out _, out var appKitRootPointKind)", macSource);
        Assert.Contains("macPlacementPollingAppKitRootPointKind = hasAppKitStartRoot ? appKitRootPointKind : null;", macSource);
        Assert.Contains("macPlacementPollingStartRootPoint = rootPoint;", macSource);
        Assert.Contains("SelectionRect.IsVisible = false;", macSource);
        Assert.Contains("UpdateMacPlacementNativeSelectionOverlay(rootPoint, rootPoint);", macSource);
        Assert.Contains("TryGetMacPlacementPrimaryRecognizerRunningRootPoint(out var rootPoint)", macSource);
        Assert.Contains("macPlacementPollingAppKitRootPointKind is MacAppKitRootPointKind appKitRootPointKind", macSource);
        Assert.Contains("TryGetMacCurrentRootPointerFromAppKit(appKitRootPointKind, allowOutsideRoot: true, out var lockedAppKitRootPoint)", macSource);
        Assert.Contains("TryGetMacPlacementRootCanvasPoint(lockedAppKitRootPoint, allowOutside: true", macSource);
        Assert.Contains("TryGetMacCurrentAppKitWindowPoint(nativeWindow, rootView", macSource);
        Assert.Contains("TryGetMacCurrentScreenPointer(rootPoint, out var rawStartScreen, out var rawStartScreenPointKind)", macSource);
        Assert.Contains("macPlacementPollingRawStartScreen = hasRawStartScreen ? rawStartScreen : null;", macSource);
        Assert.Contains("macPlacementPollingScreenPointKind = hasRawStartScreen ? rawStartScreenPointKind : null;", macSource);
        Assert.Contains("macPlacementPollingScreenPointKind is MacPointerScreenPointKind screenPointKind", macSource);
        Assert.Contains("TryGetMacCurrentRootPointer(screenPointKind, allowOutsideRoot: true, out var lockedRootPoint)", macSource);
        Assert.Contains("TryGetMacPlacementRootCanvasPoint(lockedRootPoint, allowOutside: true", macSource);
        Assert.Contains("UpdateMacPlacementNativeSelectionOverlay(macPlacementPollingStartRootPoint ?? rootPoint, rootPoint);", macSource);
        Assert.Contains("private void UpdateMacPlacementNativeSelectionOverlay(Point startRootPoint, Point currentRootPoint, bool hide = false)", macSource);
        Assert.Contains("private UIView? EnsureMacPlacementNativeSelectionOverlay(UIView rootView)", macSource);
        Assert.Contains("private void HideMacPlacementNativeSelectionOverlay(bool fade = false)", macSource);
        Assert.Contains("UIView.AnimateNotify(", macSource);
        Assert.Contains("UiTimingPolicy.SelectionRectFadeOutDurationMs / 1000d", macSource);
        Assert.Contains("HideMacPlacementNativeSelectionOverlay(fade: true);", macSource);
        Assert.Contains("rootView.AddSubview(overlay);", macSource);
        Assert.Contains("rootView.BringSubviewToFront(overlay);", macSource);
        Assert.Contains("macPlacementSelectionOverlayView.RemoveFromSuperview();", macSource);
        Assert.Contains("macPlacementSelectionOverlayView = overlay;", macSource);
        Assert.Contains("anchorViewport.X + rawScreenPoint.X - rawStartScreen.X", macSource);
        Assert.Contains("EnumerateMacPointerScreenPointCandidatesWithKinds(currentEvent)", macSource);
        Assert.Contains("TryGetMacPointerScreenPointCandidate(currentEvent, screenPointKind, out var point)", macSource);
        Assert.Contains("private static bool TryGetMacCurrentScreenPointer(out Point screenPoint)", macSource);
        Assert.Contains("macPlacementPollingAppKitRootPointKind = null;", macSource);
        Assert.Contains("macPlacementPollingStartRootPoint = null;", macSource);
        Assert.Contains("macPlacementPollingScreenPointKind = null;", macSource);
        Assert.Contains("ResetMacPlacementPollingSelectionTracking();", macSource);
        Assert.Contains("macPlacementPollingSelectionActive = true;", macSource);
        Assert.Contains("macPlacementPollingSelectionActive = false;", macSource);
        Assert.Contains("macPlacementNativeSelectionIgnored = false;\n            macPlacementNativeSelectionStartRootPoint = null;", macSource);
        Assert.Contains("macPlacementNativeSelectionActive = true;", macSource);
        Assert.Contains("macPlacementNativeSelectionActive = false;", macSource);
        Assert.Contains("macPlacementNativeSelectionStartRootPoint = rootPoint;", macSource);
        Assert.Contains("UpdateMacPlacementNativeSelectionOverlay(macPlacementNativeSelectionStartRootPoint ?? rootPoint, rootPoint);", macSource);
        Assert.Contains("macPlacementNativeSelectionStartRootPoint = null;", macSource);
        Assert.Contains("RootGrid.Handler?.PlatformView is not UIView rootView", macSource);
        Assert.Contains("var nativeLocation = recognizer.LocationInView(macPlacementGestureNativeView);", macSource);
        Assert.Contains("rootView.ConvertPointFromView(nativeLocation, macPlacementGestureNativeView)", macSource);
        Assert.Contains("lastPointerOnRoot = rootPoint;", macSource);
        Assert.Contains("TryGetMacPlacementRootCanvasPoint(rootPoint, allowOutside, out canvasPoint, out viewportPoint)", macSource);
        Assert.Contains("nativeView.AddGestureRecognizer(macPlacementPrimarySelectionRecognizer);", macSource);
        Assert.Contains("Delegate = macPlacementGestureDelegate,", macSource);
        Assert.Contains("private sealed class MacPlacementGestureDelegate : UIGestureRecognizerDelegate", macSource);
        Assert.Contains("ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)", macSource);
        Assert.Contains("TrySetMacPlacementButtonMaskRequired(recognizer, 0x2);", macSource);
        Assert.Contains("case UIGestureRecognizerState.Began:", macSource);
        Assert.Contains("if (MacPlacementPrimarySelectionUsesPolling)", macSource);
        Assert.Contains("selectionStartViewport.X + e.TotalX", pointerSource);
        Assert.Contains("selectionStartViewport.Y + e.TotalY", pointerSource);
        Assert.Contains("case UIGestureRecognizerState.Changed:", macSource);
        Assert.Contains("case UIGestureRecognizerState.Ended:", macSource);
        Assert.Contains("case UIGestureRecognizerState.Cancelled:", macSource);
        Assert.Contains("new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, selectionStartCanvas.X, selectionStartCanvas.Y, GestureStatus.Started)", macSource);
        Assert.Contains("new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, canvasPoint.X, canvasPoint.Y, GestureStatus.Running)", macSource);
        Assert.Contains("new SelectionPayload(selectionStartCanvas.X, selectionStartCanvas.Y, selectionLastCanvas.X, selectionLastCanvas.Y, GestureStatus.Completed)", macSource);
        Assert.Contains("if (macPlacementNativeSelectionActive)", pointerSource);
        Assert.Contains("_ = OpenCreateEditorFromCanvasPointWithWarningAsync(canvasPoint, nameof(HandleMacPlacementSecondaryCreateRecognizer));", macSource);
        Assert.Contains("private async Task OpenCreateEditorFromCanvasPointWithWarningAsync(Point canvasPoint, string source)", pointerSource);
        Assert.Contains("macPlacementPollingCommandSelectionItemId == item.Id", pointerSource);
        Assert.Contains("macPlacementPollingCommandSelectionItemId = null;", editorSource);
        Assert.Contains("private int selectionRectFadeRevision;", interactionSource);
        Assert.Contains("FadeOutSelectionRect();", pointerSource);
        Assert.Contains("SelectionRect.FadeToAsync(0, UiTimingPolicy.SelectionRectFadeOutDurationMs, Easing.CubicOut)", pointerSource);
        Assert.Contains("SelectionRect.CancelAnimations();", pointerSource);

        var pollingRootIndex = macSource.IndexOf("private bool TryGetMacPlacementPollingRootPoint(out Point rootPoint)", StringComparison.Ordinal);
        var recentHoverIndex = macSource.IndexOf("private bool TryGetRecentMacPlacementHoverRootPoint(out Point rootPoint)", StringComparison.Ordinal);
        Assert.True(pollingRootIndex >= 0 && recentHoverIndex > pollingRootIndex, "Polling root helper should be present.");
        var pollingRootBody = macSource[pollingRootIndex..recentHoverIndex];
        Assert.DoesNotContain("TryGetMacPlacementPrimaryRecognizerRootPoint", pollingRootBody);

        var pollingRunningIndex = macSource.IndexOf("private bool TryGetMacPlacementPollingRunningPoint(out Point canvasPoint, out Point viewportPoint, out Point rootPoint)", StringComparison.Ordinal);
        var overlayUpdateIndex = macSource.IndexOf("private void UpdateMacPlacementNativeSelectionOverlay(Point startRootPoint, Point currentRootPoint, bool hide = false)", StringComparison.Ordinal);
        Assert.True(pollingRunningIndex >= 0 && overlayUpdateIndex > pollingRunningIndex, "Polling running helper should be present.");
        var pollingRunningBody = macSource[pollingRunningIndex..overlayUpdateIndex];
        Assert.DoesNotContain("TryGetMacPlacementPrimaryRecognizerRunningRootPoint", pollingRunningBody);
        var pollingRunningRawDeltaIndex = pollingRunningBody.IndexOf("anchorViewport.X + rawScreenPoint.X - rawStartScreen.X", StringComparison.Ordinal);
        var pollingRunningAppKitIndex = pollingRunningBody.IndexOf("TryGetMacCurrentRootPointerFromAppKit(appKitRootPointKind, allowOutsideRoot: true, out var lockedAppKitRootPoint)", StringComparison.Ordinal);
        var pollingRunningCoreGraphicsIndex = pollingRunningBody.IndexOf("TryGetMacCurrentRootPointer(screenPointKind, allowOutsideRoot: true, out var lockedRootPoint)", StringComparison.Ordinal);
        Assert.True(pollingRunningRawDeltaIndex >= 0 && pollingRunningAppKitIndex > pollingRunningRawDeltaIndex, "Polling rectangle updates should prefer locked raw-screen deltas over absolute AppKit root-coordinate fallback.");
        Assert.True(pollingRunningAppKitIndex >= 0 && pollingRunningCoreGraphicsIndex > pollingRunningAppKitIndex, "Polling rectangle updates should keep the accepted AppKit root-coordinate candidate ahead of CoreGraphics fallback.");

        var pollingCurrentRootIndex = macSource.IndexOf("private bool TryGetMacPlacementPollingCurrentRootPoint(out Point rootPoint)", StringComparison.Ordinal);
        var continuePollingIndex = macSource.IndexOf("private void ContinueMacPlacementPollingSelection()", StringComparison.Ordinal);
        Assert.True(pollingCurrentRootIndex >= 0 && continuePollingIndex > pollingCurrentRootIndex, "Polling current-root helper should be present.");
        var pollingCurrentRootBody = macSource[pollingCurrentRootIndex..continuePollingIndex];
        var currentRootAppKitIndex = pollingCurrentRootBody.IndexOf("TryGetMacCurrentRootPointerFromAppKit(out rootPoint, useCachedDistance: true)", StringComparison.Ordinal);
        var currentRootCoreGraphicsIndex = pollingCurrentRootBody.IndexOf("TryGetMacCurrentRootPointerFromCoreGraphics(out rootPoint, useCachedDistance: true)", StringComparison.Ordinal);
        Assert.True(currentRootAppKitIndex >= 0 && currentRootCoreGraphicsIndex > currentRootAppKitIndex, "Polling current-root fallback should prefer AppKit root coordinates before CoreGraphics candidates.");

        var currentPlacementRootIndex = macSource.IndexOf("private bool TryGetMacCurrentPlacementRootPointer(", StringComparison.Ordinal);
        var rootInsideIndex = macSource.IndexOf("private bool IsMacRootPointInsideRoot(Point rootPoint)", StringComparison.Ordinal);
        Assert.True(currentPlacementRootIndex >= 0 && rootInsideIndex > currentPlacementRootIndex, "Placement root pointer helper should be present.");
        var currentPlacementRootBody = macSource[currentPlacementRootIndex..rootInsideIndex];
        var placementRootAppKitIndex = currentPlacementRootBody.IndexOf("TryGetMacCurrentRootPointerFromAppKit(out rootPoint, useCachedDistance)", StringComparison.Ordinal);
        var placementRootCoreGraphicsIndex = currentPlacementRootBody.IndexOf("TryGetMacCurrentRootPointerFromCoreGraphics(out rootPoint, useCachedDistance)", StringComparison.Ordinal);
        Assert.True(placementRootAppKitIndex >= 0 && placementRootCoreGraphicsIndex > placementRootAppKitIndex, "Placement root pointer lookup should prefer AppKit root coordinates before CoreGraphics candidates.");
    }

    [Fact]
    public void MainPage_CopyNoticeAnimationFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.xaml.cs");
        Assert.Contains("catch (OperationCanceledException) when (token.IsCancellationRequested)", source);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(\"MainPage.CopyIconButton_Clicked\", $\"Copy notice animation failed while overlayVisible={CopyNoticeOverlay.IsVisible} tokenCanceled={token.IsCancellationRequested}: {safeMessage}\");", source);
    }

    [Fact]
    public void MainPage_StatusFlashAnimationFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.StatusAndTheme.cs");
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("var messageLength = message?.Length ?? 0;", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(TriggerStatusFlash), $\"Status flash animation failed for message length {messageLength} (isError={StatusFlashErrorPolicy.IsErrorStatus(message)}): {safeMessage}\");", source);
    }

    [Fact]
    public void MainPage_DockHoverExitFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.DockAndQuickLook.cs");
        Assert.Contains("catch (OperationCanceledException) when (token.IsCancellationRequested)", source);
        Assert.Equal(3, CountOccurrences(source, "var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);"));
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(HideDockScrollBarAfterExitDelayAsync), $\"Dock hover-exit hide failed while pointerHover={isDockPointerHovering}: {safeMessage}\");", source);
        Assert.True(
            source.IndexOf("SetDockScrollBarVisibility(isPointerOverDockRegion: false);", StringComparison.Ordinal)
            < source.IndexOf("CrashFileLogger.WriteWarning(nameof(HideDockScrollBarAfterExitDelayAsync), $\"Dock hover-exit hide failed while pointerHover={isDockPointerHovering}: {safeMessage}\");", StringComparison.Ordinal));
    }

    [Fact]
    public void MainPage_QuickLookShowFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.DockAndQuickLook.cs");
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(ShowQuickLookAfterDelayAsync), $\"Quick Look show failed for item '{item.Id}' while popupVisible={QuickLookPopup.IsVisible}: {safeMessage}\");", source);
        Assert.True(
            source.IndexOf("QuickLookPopup.CancelAnimations();", StringComparison.Ordinal)
            < source.IndexOf("CrashFileLogger.WriteWarning(nameof(ShowQuickLookAfterDelayAsync), $\"Quick Look show failed for item '{item.Id}' while popupVisible={QuickLookPopup.IsVisible}: {safeMessage}\");", StringComparison.Ordinal));
    }

    [Fact]
    public void MainPage_QuickLookHideFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.DockAndQuickLook.cs");
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(HideQuickLookAfterDelayAsync), $\"Quick Look hide failed for pending item '{quickLookPendingItemId}': {safeMessage}\");", source);
        Assert.True(
            source.IndexOf("QuickLookPopup.IsVisible = false;", StringComparison.Ordinal)
            < source.IndexOf("CrashFileLogger.WriteWarning(nameof(HideQuickLookAfterDelayAsync), $\"Quick Look hide failed for pending item '{quickLookPendingItemId}': {safeMessage}\");", StringComparison.Ordinal));
    }

    [Fact]
    public void MainPage_ButtonTapExecutionFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.EditorAndInput.cs");
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(Draggable_Tapped), $\"Button tap execution failed for '{item.ButtonText}' ({item.Id}): {safeMessage}\");", source);
    }

    [Fact]
    public void MainPage_SecondaryTapCreateFailures_AreWarningLogged()
    {
        var source = ReadRepositoryFile("Praxis", "MainPage.PointerAndSelection.cs");
        Assert.Equal(3, CountOccurrences(source, "var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);"));
        Assert.Contains("await OpenCreateEditorFromCanvasPointWithWarningAsync(canvasPoint, nameof(PlacementCanvas_SecondaryTapped));", source);
        Assert.Contains("_ = OpenCreateEditorFromCanvasPointWithWarningAsync(point.Value, nameof(Selection_PointerPressed));", source);
        Assert.Contains("CrashFileLogger.WriteWarning(source, $\"Placement-canvas create flow failed at ({canvasPoint.X:0.##}, {canvasPoint.Y:0.##}): {safeMessage}\");", source);
        Assert.Contains("var modalVisible = EditorOverlay.IsVisible;", source);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(FocusModalPrimaryEditorField), $\"Failed to focus modal ButtonText entry while shouldSelectAll={shouldSelectAll} modalVisible={modalVisible}: {safeMessage}\");", source);
    }

    [Fact]
    public void MainPage_WindowsFocusFallbackFailures_AreWarningLogged()
    {
        var focusSource = ReadRepositoryFile("Praxis", "MainPage.FocusAndContext.cs");
        var layoutSource = ReadRepositoryFile("Praxis", "MainPage.LayoutUtilities.cs");

        Assert.Contains("var controlType = control.GetType().Name;", focusSource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", focusSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(DisableWindowsSystemFocusVisual), $\"Failed to disable UseSystemFocusVisuals on {controlType}: {safeMessage}\");", focusSource);
        Assert.Contains("var safeMessage = CrashFileLogger.SafeExceptionMessage(ex);", layoutSource);
        Assert.Contains("var targetType = platformView.GetType().Name;", layoutSource);
        Assert.Contains("CrashFileLogger.WriteWarning(nameof(SetTabStop), $\"Failed to set IsTabStop={isTabStop} on {targetType}: {safeMessage}\");", layoutSource);
    }

    private static string ReadRepositoryFile(params string[] segments)
        => File.ReadAllText(Path.Combine(ResolveRepositoryRoot(), Path.Combine(segments)));

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Praxis"))
                && Directory.Exists(Path.Combine(current.FullName, "Praxis.Tests")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located from test output path.");
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static void AssertMethodContainsInOrder(string text, string methodSignature, params string[] markers)
    {
        var methodStart = text.IndexOf(methodSignature, StringComparison.Ordinal);
        Assert.True(methodStart >= 0, $"Missing method: {methodSignature}");
        Assert.True(markers.Length >= 2, "At least two ordered markers are required.");

        var methodBody = text[methodStart..];
        var previousIndex = -1;
        string? previousMarker = null;
        foreach (var marker in markers)
        {
            var currentIndex = methodBody.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(currentIndex >= 0, $"Missing marker: {marker}");
            if (previousMarker is not null)
            {
                Assert.True(currentIndex > previousIndex, $"Expected '{previousMarker}' to appear before '{marker}'.");
            }

            previousIndex = currentIndex;
            previousMarker = marker;
        }
    }
}
