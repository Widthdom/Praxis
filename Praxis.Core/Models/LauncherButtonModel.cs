using CommunityToolkit.Mvvm.ComponentModel;

namespace Praxis.Core.Models;

public partial class LauncherButtonModel : ObservableObject
{
    [ObservableProperty]
    private Guid id = Guid.NewGuid();

    [ObservableProperty]
    private string text = string.Empty;

    [ObservableProperty]
    private string command = string.Empty;

    [ObservableProperty]
    private double x;

    [ObservableProperty]
    private double y;

    [ObservableProperty]
    private double width = Logic.ButtonLayoutDefaults.Width;

    [ObservableProperty]
    private double height = Logic.ButtonLayoutDefaults.Height;

    [ObservableProperty]
    private LauncherButtonColorKey colorKey = LauncherButtonColorKey.Default;

    [ObservableProperty]
    private string toolTip = string.Empty;

    [ObservableProperty]
    private string commandPath = string.Empty;

    [ObservableProperty]
    private string arguments = string.Empty;

    [ObservableProperty]
    private string clipText = string.Empty;

    [ObservableProperty]
    private string note = string.Empty;

    [ObservableProperty]
    private bool useInvertedThemeColors;

    [ObservableProperty]
    private DateTime? lastExecutedAtUtc;

    [ObservableProperty]
    private int sortOrder;

    [ObservableProperty]
    private DateTime createdAtUtc = DateTime.UtcNow;

    [ObservableProperty]
    private DateTime updatedAtUtc = DateTime.UtcNow;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isExecuting;

    public string ToolTipText
    {
        get
        {
            var lines = new List<string>();
            AddLine(lines, "Command", Command);
            AddLine(lines, "ButtonText", Text);
            AddLine(lines, "Tool", CommandPath);
            AddLine(lines, "Arguments", Arguments);
            AddLine(lines, "Clip Word", ClipText);
            AddLine(lines, "Note", Note);
            return lines.Count == 0 ? Text : string.Join(Environment.NewLine, lines);
        }
    }

    partial void OnTextChanged(string value) => OnPropertyChanged(nameof(ToolTipText));

    partial void OnCommandChanged(string value) => OnPropertyChanged(nameof(ToolTipText));

    partial void OnCommandPathChanged(string value) => OnPropertyChanged(nameof(ToolTipText));

    partial void OnArgumentsChanged(string value) => OnPropertyChanged(nameof(ToolTipText));

    partial void OnClipTextChanged(string value) => OnPropertyChanged(nameof(ToolTipText));

    partial void OnNoteChanged(string value) => OnPropertyChanged(nameof(ToolTipText));

    private static void AddLine(List<string> lines, string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            lines.Add($"{label}: {value}");
        }
    }
}
