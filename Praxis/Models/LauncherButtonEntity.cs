using Praxis.Core.Logic;

namespace Praxis.Models;

public sealed class LauncherButtonEntity
{
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
}
