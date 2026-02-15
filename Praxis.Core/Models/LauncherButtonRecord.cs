namespace Praxis.Core.Models;

public sealed class LauncherButtonRecord
{
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
}
