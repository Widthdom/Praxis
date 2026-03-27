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
        Assert.Contains("<Setter Property=\"FontSize\" Value=\"12\" />", xaml);
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
        var modalRegion = xaml.Substring(modalEntryIndex, Math.Min(500, xaml.Length - modalEntryIndex));

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
        Assert.Contains("Color=\"{AppThemeBinding Light=#FFFFFF, Dark=#2A2A2A}\"", xaml);
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
