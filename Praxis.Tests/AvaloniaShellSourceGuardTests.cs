namespace Praxis.Tests;

public class AvaloniaShellSourceGuardTests
{
    [Fact]
    public void MainWindowCodeBehind_LoadsXamlDirectly()
    {
        var source = ReadRepositoryFile("Praxis.Avalonia", "Views", "MainWindow.axaml.cs");

        Assert.Contains("AvaloniaXamlLoader.Load(this);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InitializeComponent", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PointerPressed", source, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyDown", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PropertyChanged", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_DefinesIconDragAreaAndCaptionButtons()
    {
        var xaml = ReadRepositoryFile("Praxis.Avalonia", "Views", "MainWindow.axaml");

        Assert.Contains("Icon=\"/Assets/praxis-icon.ico\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("praxis-title-icon", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Praxis\"", xaml, StringComparison.Ordinal);
        Assert.Contains("WindowDragBehavior.IsDragArea=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"MacTitleBarDragSurface\"", xaml, StringComparison.Ordinal);
        Assert.Contains("window.FindControl<Border>(\"MacTitleBarDragSurface\")!.IsVisible = isMac;", ReadRepositoryFile("Praxis.Avalonia", "Behaviors", "MainWindowInteractionBehavior.cs"), StringComparison.Ordinal);
        Assert.Contains("NotifyMoveDragStarted", ReadRepositoryFile("Praxis.Avalonia", "Behaviors", "WindowDragBehavior.cs"), StringComparison.Ordinal);
        Assert.Contains("IsTitleBarDoubleClick", ReadRepositoryFile("Praxis.Avalonia", "Behaviors", "WindowDragBehavior.cs"), StringComparison.Ordinal);
        Assert.Contains("e.ClickCount", ReadRepositoryFile("Praxis.Avalonia", "Behaviors", "WindowDragBehavior.cs"), StringComparison.Ordinal);
        Assert.Contains("WindowState.Maximized", ReadRepositoryFile("Praxis.Avalonia", "Behaviors", "WindowDragBehavior.cs"), StringComparison.Ordinal);
        Assert.Contains("x:Name=\"MacCaptionButtons\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"WindowsCaptionButtons\"", xaml, StringComparison.Ordinal);
        Assert.Contains("WindowChromeButtonBehavior.Action=\"Minimize\"", xaml, StringComparison.Ordinal);
        Assert.Contains("WindowChromeButtonBehavior.Action=\"ToggleMaximize\"", xaml, StringComparison.Ordinal);
        Assert.Contains("WindowChromeButtonBehavior.Action=\"Close\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_WindowsHitTest_PrioritizesTopResizeBeforeCaptionDrag()
    {
        var behavior = ReadRepositoryFile("Praxis.Avalonia", "Behaviors", "MainWindowInteractionBehavior.cs");

        Assert.Contains("WindowsTopResizeHeightDip", behavior, StringComparison.Ordinal);
        Assert.Contains("WindowsWindowHitTestPolicy.ResolveTopBandHit", behavior, StringComparison.Ordinal);
        Assert.Contains("WindowsWindowHitTestZone.Top => (IntPtr)HtTop", behavior, StringComparison.Ordinal);
        Assert.Contains("WindowsWindowHitTestZone.TopLeft => (IntPtr)HtTopLeft", behavior, StringComparison.Ordinal);
        Assert.Contains("WindowsWindowHitTestZone.TopRight => (IntPtr)HtTopRight", behavior, StringComparison.Ordinal);
        Assert.Contains("WindowsWindowHitTestZone.Caption => (IntPtr)HtCaption", behavior, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_MacNormalMaximize_RestoresCapturedBoundsWhenReportedAsNormal()
    {
        var behavior = ReadRepositoryFile("Praxis.Avalonia", "Behaviors", "MainWindowInteractionBehavior.cs");

        Assert.Contains("IsAtMacNormalMaximizedBounds()", behavior, StringComparison.Ordinal);
        Assert.Contains("RestoreCapturedNormalMaximizeBounds();", behavior, StringComparison.Ordinal);
        Assert.DoesNotContain("else\n        {\n            RestoreFallbackMacNormalBounds();\n        }\n\n        macNormalZoomRestoreBounds = null;", behavior, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_UsesIconPlaceholdersAndPointerSurface()
    {
        var xaml = ReadRepositoryFile("Praxis.Avalonia", "Views", "MainWindow.axaml");
        var behavior = ReadRepositoryFile("Praxis.Avalonia", "Behaviors", "MainWindowInteractionBehavior.cs");

        Assert.Contains("MainWindowInteractionBehavior.IsEnabled=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("praxis-placeholder-icon", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"&gt;\"", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility=\"Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"{Binding PlacementSurfaceWidth}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Height=\"{Binding PlacementSurfaceHeight}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("MinWidth=\"1600\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("MinHeight=\"880\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsTabStop=\"False\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("PointerPressed=\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyDown=\"", xaml, StringComparison.Ordinal);
        Assert.Contains("LauncherButton_PointerPressed", behavior, StringComparison.Ordinal);
        Assert.Contains("handledEventsToo: true", behavior, StringComparison.Ordinal);
        Assert.Contains("DockButton_PointerPressed", behavior, StringComparison.Ordinal);
        Assert.Contains("PlacementSurface_PointerPressed", behavior, StringComparison.Ordinal);
        Assert.Contains("MouseButton.Left", behavior, StringComparison.Ordinal);
        Assert.Contains("IsRightButtonPressed", behavior, StringComparison.Ordinal);
        Assert.Contains("IsMiddleButtonPressed", behavior, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_DefinesEditorModalAndStatusDismissHooks()
    {
        var xaml = ReadRepositoryFile("Praxis.Avalonia", "Views", "MainWindow.axaml");
        var behavior = ReadRepositoryFile("Praxis.Avalonia", "Behaviors", "MainWindowInteractionBehavior.cs");

        Assert.Contains("IsEditorOpen", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Model.", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("xmlns:models", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("using:Praxis.Core.Models", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("DataType=\"models:", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"GUID\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsReadOnly=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"ButtonText\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Clip Word\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Note\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Invert Theme\"", xaml, StringComparison.Ordinal);
        Assert.Contains("praxis-copy-button", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Click=\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SaveEditorCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("CancelEditorCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"StatusBar\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsVisible=\"{Binding Status.IsVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("DispatcherTimer", behavior, StringComparison.Ordinal);
        Assert.Contains("Key.Escape", behavior, StringComparison.Ordinal);
        Assert.Contains("Key.S", behavior, StringComparison.Ordinal);
        Assert.Contains("UndoCommand", behavior, StringComparison.Ordinal);
        Assert.Contains("ApplyThemeCommand", behavior, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_RestoresCommandFocusOnWindowActivation()
    {
        var behavior = ReadRepositoryFile("Praxis.Avalonia", "Behaviors", "MainWindowInteractionBehavior.cs");

        Assert.Contains("window.Activated += WindowOnActivated;", behavior, StringComparison.Ordinal);
        Assert.Contains("window.Activated -= WindowOnActivated;", behavior, StringComparison.Ordinal);
        Assert.Contains("ScheduleCommandFocusAfterActivation", behavior, StringComparison.Ordinal);
        Assert.Contains("FocusCommandBoxAfterActivation", behavior, StringComparison.Ordinal);
        Assert.Contains("commandBox.Focus();", behavior, StringComparison.Ordinal);
        Assert.Contains("commandBox.SelectAll();", behavior, StringComparison.Ordinal);
        Assert.Contains("WindowActivationCommandFocusPolicy.ShouldFocusMainCommand", behavior, StringComparison.Ordinal);
        Assert.Contains("UiTimingPolicy.MacActivationFocusRequestCoalesceDelay", behavior, StringComparison.Ordinal);
        Assert.Contains("UiTimingPolicy.WindowsFocusRestorePrimaryDelay", behavior, StringComparison.Ordinal);
    }

    [Fact]
    public void AvaloniaProject_EmbedsIconAssets()
    {
        var project = ReadRepositoryFile("Praxis.Avalonia", "Praxis.Avalonia.csproj");
        var app = ReadRepositoryFile("Praxis.Avalonia", "App.axaml.cs");
        var macDockIconService = ReadRepositoryFile("Praxis.Avalonia", "Services", "MacDockIconService.cs");
        var iconPath = Path.Combine(ResolveRepositoryRoot(), "Praxis.Avalonia", "Assets", "praxis-icon.ico");
        var dockIconPath = Path.Combine(ResolveRepositoryRoot(), "Praxis.Avalonia", "Assets", "praxis-dock-icon.png");

        Assert.Contains("<ApplicationIcon>Assets/praxis-icon.ico</ApplicationIcon>", project, StringComparison.Ordinal);
        Assert.Contains("<AvaloniaResource Include=\"Assets\\**\" />", project, StringComparison.Ordinal);
        Assert.Contains("<None Update=\"Assets\\praxis-dock-icon.png\" CopyToOutputDirectory=\"PreserveNewest\" />", project, StringComparison.Ordinal);
        Assert.Contains("MacDockIconService.ApplyIfNeeded();", app, StringComparison.Ordinal);
        Assert.Contains("OperatingSystem.IsMacOS()", macDockIconService, StringComparison.Ordinal);
        Assert.Contains("setApplicationIconImage:", macDockIconService, StringComparison.Ordinal);
        Assert.True(File.Exists(iconPath));
        Assert.True(File.Exists(dockIconPath));
    }

    private static string ReadRepositoryFile(params string[] path)
        => File.ReadAllText(Path.Combine([ResolveRepositoryRoot(), .. path]));

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".github"))
                && Directory.Exists(Path.Combine(current.FullName, "Praxis.Tests")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located from test output path.");
    }
}
