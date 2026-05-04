namespace Praxis.Tests;

public class MainPageStructureTests
{
    [Fact]
    public void MainPage_FieldDeclarations_AreSplitIntoConcernSpecificPartials()
    {
        var root = ResolveRepositoryRoot();

        var xamlCodeBehind = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.xaml.cs"));
        Assert.DoesNotContain("private readonly MainViewModel viewModel;", xamlCodeBehind);
        Assert.DoesNotContain("private enum ConflictDialogFocusTarget", xamlCodeBehind);
        Assert.DoesNotContain("private static WeakReference<MainPage>? macLastActivePage;", xamlCodeBehind);

        var coreFields = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.Fields.Core.cs"));
        Assert.Contains("private readonly MainViewModel viewModel;", coreFields);
        Assert.Contains("private Window? attachedWindow;", coreFields);

        var overlayFields = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.Fields.OverlayAndEditor.cs"));
        Assert.Contains("private CancellationTokenSource? quickLookShowCts;", overlayFields);
        Assert.Contains("private const double ModalSingleLineRowHeight = 40;", overlayFields);

        var windowsFields = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.Fields.Windows.cs"));
        Assert.Contains("#if WINDOWS", windowsFields);
        Assert.Contains("private Microsoft.UI.Xaml.Controls.TextBox? commandTextBox;", windowsFields);

        var macFields = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.Fields.MacCatalyst.cs"));
        Assert.Contains("#if MACCATALYST", macFields);
        Assert.Contains("private enum ModalFocusTarget", macFields);
    }

    [Fact]
    public void MainPage_FieldPartialFiles_Exist()
    {
        var root = ResolveRepositoryRoot();
        var expectedFiles = new[]
        {
            "MainPage.Fields.Core.cs",
            "MainPage.Fields.OverlayAndEditor.cs",
            "MainPage.Fields.ConflictDialog.cs",
            "MainPage.Fields.Windows.cs",
            "MainPage.Fields.MacCatalyst.cs",
            "MainPage.InteractionState.cs",
        };

        foreach (var file in expectedFiles)
        {
            var fullPath = Path.Combine(root, "Praxis", file);
            Assert.True(File.Exists(fullPath), $"Expected file does not exist: {fullPath}");
        }
    }

    [Fact]
    public void MainPage_PlacementAndDockButtonLabels_UseSmallerFontOnWindowsAndMac()
    {
        var root = ResolveRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.xaml"));

        Assert.Contains("<Style x:Key=\"PlacementButtonTextLabelStyle\"", xaml);
        Assert.Contains("<Setter Property=\"FontSize\" Value=\"{OnPlatform Default=12, MacCatalyst=14}\" />", xaml);
        Assert.Equal(2, CountOccurrences(xaml, "Style=\"{StaticResource PlacementButtonTextLabelStyle}\""));
    }

    [Fact]
    public void MainPage_CommandSuggestionScrollView_IsNamed()
    {
        var root = ResolveRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.xaml"));

        Assert.Contains("x:Name=\"CommandSuggestionScrollView\"", xaml);
    }

    [Fact]
    public void MainPage_GlassmorphismSurface_IsTransparentAndUsesMaterialFrames()
    {
        var root = ResolveRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.xaml"));

        Assert.Contains("BackgroundColor=\"Transparent\"", xaml);
        Assert.DoesNotContain("x:Name=\"MainGlassFrame\"", xaml);
        Assert.DoesNotContain("StaticResource GlassRootLight", xaml);
        Assert.DoesNotContain("StaticResource GlassRootDark", xaml);
        Assert.Contains("x:Name=\"RootPage\"", xaml);
        Assert.Contains("x:Name=\"RootGrid\"", xaml);
        Assert.Contains("x:Name=\"DummyRootGlassFrame\"", xaml);
        Assert.Contains("MacOSBackdropOpacity=\"0.30\"", xaml);
        Assert.DoesNotContain("Margin=\"-12,0,12,0\"", xaml);
        Assert.Contains("HorizontalOptions=\"Fill\"", xaml);
        Assert.Contains("VerticalOptions=\"Fill\"", xaml);
        Assert.Contains("StaticResource DummyRootGlassLight", xaml);
        Assert.Contains("StaticResource DummyRootGlassDark", xaml);
        Assert.Contains("MacOSBehindWindowBlur=\"True\"", xaml);
        Assert.Contains("<Color x:Key=\"GlassPanelLight\">#00FFFFFF</Color>", xaml);
        Assert.Contains("<Color x:Key=\"GlassPanelDark\">#00000000</Color>", xaml);
        Assert.Contains("<Color x:Key=\"GlassPopupLight\">#24FFFFFF</Color>", xaml);
        Assert.Contains("<Color x:Key=\"GlassPopupDark\">#381E2228</Color>", xaml);
        Assert.Contains("<Color x:Key=\"GlassInputLight\">#30FFFFFF</Color>", xaml);
        Assert.Contains("<Color x:Key=\"GlassInputDark\">#40363B43</Color>", xaml);
        Assert.Contains("<Color x:Key=\"GlassButtonLight\">#6AFFFFFF</Color>", xaml);
        Assert.Contains("<Color x:Key=\"GlassButtonDark\">#78363B43</Color>", xaml);
        Assert.Contains("x:Key=\"GlassModalActionButtonStyle\"", xaml);
        Assert.Contains("x:Key=\"GlassCopyButtonStyle\"", xaml);
        Assert.Contains("x:Name=\"MainCommandEntry\"\n                                       Placeholder=\"Command\"\n                                       Text=\"{Binding CommandInput}\"\n                                       BackgroundColor=\"{AppThemeBinding Light={StaticResource GlassInputLight}, Dark={StaticResource GlassInputDark}}\"", xaml);
        Assert.Contains("x:Name=\"MainSearchEntry\"\n                                      Placeholder=\"Search\"\n                                      Text=\"{Binding SearchText}\"\n                                      BackgroundColor=\"{AppThemeBinding Light={StaticResource GlassInputLight}, Dark={StaticResource GlassInputDark}}\"", xaml);
        Assert.Contains("x:Name=\"QuickLookPopup\"", xaml);
        Assert.Contains("x:Name=\"EditorOverlay\"", xaml);
        Assert.True(CountOccurrences(xaml, "MacOSBackdropOpacity=\"0.62\"") >= 2);
        Assert.Contains("ModalGuidEntry\" Grid.Row=\"0\" Grid.Column=\"1\" Text=\"{Binding Editor.GuidText}\" IsReadOnly=\"{OnPlatform Default=True, MacCatalyst=False}\" HeightRequest=\"40\" BackgroundColor=\"{AppThemeBinding Light={StaticResource GlassInputLight}, Dark={StaticResource GlassInputDark}}\"", xaml);
        Assert.Contains("ModalClipWordContainer\"\n                                    Grid.Row=\"5\"\n                                    Grid.Column=\"1\"\n                                    Stroke=\"Transparent\"\n                                    StrokeThickness=\"0\"", xaml);
        Assert.Contains("ModalNoteContainer\"\n                                    Grid.Row=\"6\"\n                                    Grid.Column=\"1\"\n                                    Stroke=\"Transparent\"\n                                    StrokeThickness=\"0\"", xaml);
        Assert.Contains("x:Name=\"ModalCancelButton\"\n                                Style=\"{StaticResource GlassModalActionButtonStyle}\"", xaml);
        Assert.Contains("x:Name=\"ModalSaveButton\"\n                                Style=\"{StaticResource GlassModalActionButtonStyle}\"", xaml);
        Assert.Equal(7, CountOccurrences(xaml, "Style=\"{StaticResource GlassCopyButtonStyle}\""));
        Assert.Equal(2, CountOccurrences(xaml, "Style=\"{StaticResource GlassModalActionButtonStyle}\""));
        Assert.Equal(2, CountOccurrences(xaml, "BackgroundColor=\"{AppThemeBinding Light={StaticResource GlassInputLight}, Dark={StaticResource GlassInputDark}}\"\n                                                                  TextColor="));
        Assert.DoesNotContain("MacOSBackdropOpacity=\"0.18\"", xaml);
        Assert.True(CountOccurrences(xaml, "<controls:MaterialFrame ") >= 9);
    }

    [Fact]
    public void MainPage_EditorModal_PlacesButtonTextBeforeCommand()
    {
        var root = ResolveRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.xaml"));

        var buttonTextIndex = xaml.IndexOf("x:Name=\"ModalButtonTextEntry\"", StringComparison.Ordinal);
        var commandIndex = xaml.IndexOf("x:Name=\"ModalCommandEntry\"", StringComparison.Ordinal);

        Assert.True(buttonTextIndex >= 0, "ModalButtonTextEntry was not found in MainPage.xaml.");
        Assert.True(commandIndex >= 0, "ModalCommandEntry was not found in MainPage.xaml.");
        Assert.True(buttonTextIndex < commandIndex, "ButtonText should appear before Command in the editor modal.");
    }

    [Fact]
    public void MainPage_EditorModal_DefaultFocusTargetsButtonText()
    {
        var root = ResolveRepositoryRoot();
        var pointerSource = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.PointerAndSelection.cs"));
        var viewModelEventsSource = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.ViewModelEvents.cs"));
        var windowsSource = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.WindowsInput.cs"));
        var conflictSource = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.ShortcutsAndConflict.cs"));

        Assert.Contains("private void FocusModalPrimaryEditorField()", pointerSource);
        Assert.Contains("ModalButtonTextEntry.Focus();", pointerSource);
        Assert.Contains("UiTimingPolicy.ModalOpenInitialFocusDelay", pointerSource);
        Assert.DoesNotContain("FocusModalCommandEntryForOpen", pointerSource);
        Assert.Contains("FocusModalPrimaryEditorField();", viewModelEventsSource);
        Assert.Contains("FocusModalPrimaryEditorField();", windowsSource);
        Assert.Contains("FocusModalPrimaryEditorField();", conflictSource);
    }

    [Fact]
    public void MainPage_NewEditorOpen_SelectsAllButtonTextOnInitialFocus()
    {
        var root = ResolveRepositoryRoot();
        var pointerSource = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.PointerAndSelection.cs"));
        var viewModelEventsSource = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.ViewModelEvents.cs"));
        var macSource = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.MacCatalystBehavior.cs"));
        var fieldsSource = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.Fields.OverlayAndEditor.cs"));

        Assert.Contains("private bool modalPrimaryFieldSelectAllPending;", fieldsSource);
        Assert.Contains("modalPrimaryFieldSelectAllPending = !viewModel.Editor.IsExistingRecord;", viewModelEventsSource);
        Assert.Contains("var shouldSelectAll = modalPrimaryFieldSelectAllPending;", pointerSource);
        Assert.Contains("textBox.SelectAll();", pointerSource);
        Assert.Contains("TryFocusModalPrimaryTarget(selectAllText: shouldSelectAll);", pointerSource);
        Assert.Contains("private bool TryFocusModalPrimaryTarget(bool selectAllText = false)", macSource);
        Assert.Contains("SelectAllMacEntryText(ModalButtonTextEntry);", macSource);
    }

    [Fact]
    public void MainPage_ModalCommandEntry_OptsIntoAsciiInputEnforcement_WhileMainCommandEntryDoesNot()
    {
        var root = ResolveRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.xaml"));

        var mainEntryIndex = xaml.IndexOf("x:Name=\"MainCommandEntry\"", StringComparison.Ordinal);
        var modalEntryIndex = xaml.IndexOf("x:Name=\"ModalCommandEntry\"", StringComparison.Ordinal);

        Assert.True(mainEntryIndex >= 0, "MainCommandEntry was not found in MainPage.xaml.");
        Assert.True(modalEntryIndex >= 0, "ModalCommandEntry was not found in MainPage.xaml.");

        var mainRegion = xaml.Substring(mainEntryIndex, Math.Min(500, xaml.Length - mainEntryIndex));
        var modalRegion = xaml.Substring(modalEntryIndex, Math.Min(750, xaml.Length - modalEntryIndex));

        Assert.DoesNotContain("EnforceAsciiInput=\"True\"", mainRegion);
        Assert.Contains("EnforceAsciiInput=\"True\"", modalRegion);
    }

    [Fact]
    public void MainPage_SuggestionRowMiddleClickAndSecondaryTap_AreImplementedInNonMacRebuildStack()
    {
        var root = ResolveRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.ShortcutsAndConflict.cs"));

        Assert.Contains("IsMiddlePointerPressed(e)", source);
        Assert.Contains("IsSecondaryPointerPressed(e)", source);
        Assert.Contains("ButtonsMask.Secondary", source);
        Assert.Contains("OpenEditorCommand", source);
        Assert.Contains("OpenContextMenuCommand", source);
    }

    [Fact]
    public void MainPage_SuggestionRowMiddleClickAndSecondaryTap_AreImplementedInMacRebuildStack()
    {
        var root = ResolveRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.MacCatalystBehavior.cs"));

        Assert.Contains("IsOtherMouseFromPlatformArgs(e.PlatformArgs)", source);
        Assert.Contains("IsMiddlePointerPressed(e)", source);
        Assert.Contains("IsSecondaryPointerPressed(e)", source);
        Assert.Contains("ButtonsMask.Secondary", source);
        Assert.Contains("OpenEditorCommand", source);
        Assert.Contains("OpenContextMenuCommand", source);
    }

    [Fact]
    public void MainPage_HandleMacMiddleClick_ChecksSuggestionItemsBeforePlacementButtons()
    {
        var root = ResolveRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.ShortcutsAndConflict.cs"));

        Assert.Contains("TryGetSuggestionItemAtRootPoint", source);

        var suggestionCheckIndex = source.IndexOf("TryGetSuggestionItemAtRootPoint", StringComparison.Ordinal);
        var placementCheckIndex = source.IndexOf("TryGetPlacementButtonAtRootPoint", StringComparison.Ordinal);
        Assert.True(suggestionCheckIndex < placementCheckIndex,
            "TryGetSuggestionItemAtRootPoint should appear before TryGetPlacementButtonAtRootPoint in HandleMacMiddleClick");
    }

    [Fact]
    public void MainPage_TryGetSuggestionItemAtRootPoint_AccountsForScrollOffset()
    {
        var root = ResolveRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.ShortcutsAndConflict.cs"));

        Assert.Contains("CommandSuggestionScrollView.ScrollY", source);
        Assert.Contains("TryConvertRootPointToElementLocal(rootPoint, CommandSuggestionStack)", source);
    }

    [Fact]
    public void MainPage_InvertedThemeButtonUi_IsWiredInPlacementDockAndEditor()
    {
        var root = ResolveRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "Praxis", "MainPage.xaml"));

        Assert.Contains("Binding=\"{Binding UseInvertedThemeColors}\"", xaml);
        Assert.Contains("Text=\"Invert Theme\"", xaml);
        Assert.Contains("IsChecked=\"{Binding Editor.UseInvertedThemeColors}\"", xaml);
        Assert.Contains("Opacity=\"0\"", xaml);
        Assert.Contains("Color=\"{AppThemeBinding Light=#54FFFFFF, Dark=#68282D34}\"", xaml);
        Assert.Contains("Color=\"{AppThemeBinding Light=#CECECE, Dark=#4E4E4E}\"", xaml);
        Assert.Contains("HeightRequest=\"1\"", xaml);
        Assert.Contains("WidthRequest=\"1\"", xaml);
        Assert.Contains("<shapes:Polyline", xaml);
        Assert.Contains("Points=\"1.4,5.6 3.5,8 8.9,2.1\"", xaml);
        Assert.Contains("StrokeThickness=\"2\"", xaml);
        Assert.Contains("Tapped=\"ModalInvertThemeToggle_Tapped\"", xaml);
        Assert.Contains("Text=\"Use opposite theme colors for this button\"", xaml);
        var labelIndex = xaml.IndexOf("Text=\"Use opposite theme colors for this button\"", StringComparison.Ordinal);
        var labelRegion = xaml.Substring(labelIndex, 500);
        Assert.Contains("ModalInvertThemeToggle_Tapped", labelRegion);
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Praxis.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test runtime base directory.");
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
