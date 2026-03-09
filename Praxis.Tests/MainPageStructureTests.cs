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
