namespace Praxis.Core.Models;

public sealed class ButtonHistoryAction
{
    public ButtonHistoryActionKind Kind { get; init; }

    public LauncherButtonRecord? Before { get; init; }

    public LauncherButtonRecord? After { get; init; }
}

public enum ButtonHistoryActionKind
{
    Add,
    Delete,
    Update,
}
