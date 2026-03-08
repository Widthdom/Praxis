using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Models;

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
        CreatedAtUtc = source.CreatedAtUtc;
        UpdatedAtUtc = source.UpdatedAtUtc;
    }

    [SQLite.PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Command { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public string Tool { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string ClipText { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = ButtonLayoutDefaults.Width;
    public double Height { get; set; } = ButtonLayoutDefaults.Height;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public LauncherButtonRecord ToRecord()
        => new()
        {
            Id = Guid.Parse(Id),
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
            CreatedAtUtc = CreatedAtUtc,
            UpdatedAtUtc = UpdatedAtUtc,
        };
}
