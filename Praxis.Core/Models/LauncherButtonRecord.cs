namespace Praxis.Core.Models;

public sealed class LauncherButtonRecord
{
    public LauncherButtonRecord()
    {
    }

    public LauncherButtonRecord(LauncherButtonRecord source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Id = source.Id;
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

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Command { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public string Tool { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string ClipText { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = Logic.ButtonLayoutDefaults.Width;
    public double Height { get; set; } = Logic.ButtonLayoutDefaults.Height;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public LauncherButtonRecord Clone() => new(this);
}
