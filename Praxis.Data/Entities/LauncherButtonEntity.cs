using Praxis.Core.Models;
using SQLite;

namespace Praxis.Data.Entities;

public sealed class LauncherButtonEntity
{
    public LauncherButtonEntity()
    {
    }

    public LauncherButtonEntity(LauncherButtonRecord source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Id = source.Id.ToString();
        Command = source.Command;
        ButtonText = source.ButtonText;
        Tool = source.Tool;
        Arguments = source.Arguments;
        ClipText = source.ClipText;
        Note = source.Note;
        X = source.X;
        Y = source.Y;
        Width = source.Width;
        Height = source.Height;
        UseInvertedThemeColors = source.UseInvertedThemeColors;
        ColorKey = source.ColorKey.ToString();
        ToolTip = source.ToolTip;
        LastExecutedAtUtc = source.LastExecutedAtUtc;
        SortOrder = source.SortOrder;
        CreatedAtUtc = source.CreatedAtUtc;
        UpdatedAtUtc = source.UpdatedAtUtc;
    }

    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Command { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public string Tool { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string ClipText { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = Core.Logic.ButtonLayoutDefaults.Width;
    public double Height { get; set; } = Core.Logic.ButtonLayoutDefaults.Height;
    public bool UseInvertedThemeColors { get; set; }
    public string ColorKey { get; set; } = LauncherButtonColorKey.Default.ToString();
    public string ToolTip { get; set; } = string.Empty;
    public DateTime? LastExecutedAtUtc { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public LauncherButtonRecord ToRecord()
        => new()
        {
            Id = Guid.TryParse(Id, out var id) ? id : Guid.NewGuid(),
            Command = Command,
            ButtonText = ButtonText,
            Tool = Tool,
            Arguments = Arguments,
            ClipText = ClipText,
            Note = Note,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            UseInvertedThemeColors = UseInvertedThemeColors,
            ColorKey = Enum.TryParse<LauncherButtonColorKey>(ColorKey, ignoreCase: true, out var colorKey)
                ? colorKey
                : LauncherButtonColorKey.Default,
            ToolTip = ToolTip,
            LastExecutedAtUtc = LastExecutedAtUtc,
            SortOrder = SortOrder,
            CreatedAtUtc = CreatedAtUtc,
            UpdatedAtUtc = UpdatedAtUtc,
        };
}
