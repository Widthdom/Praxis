using Praxis.Core.Models;

namespace Praxis.Core.Logic;

public static class ButtonHistoryConsistencyPolicy
{
    public static bool MatchesExpectedVersion(LauncherButtonRecord? expected, LauncherButtonRecord? current)
    {
        if (expected is null)
        {
            return current is null;
        }

        if (current is null)
        {
            return false;
        }

        if (expected.UpdatedAtUtc == current.UpdatedAtUtc)
        {
            return true;
        }

        // SQLite round-trip or platform DateTime conversion can slightly alter precision.
        // Treat logically identical records as consistent even when UpdatedAtUtc differs.
        return HasSameContent(expected, current);
    }

    private static bool HasSameContent(LauncherButtonRecord expected, LauncherButtonRecord current)
    {
        return expected.Id == current.Id &&
            expected.Command == current.Command &&
            expected.ButtonText == current.ButtonText &&
            expected.Tool == current.Tool &&
            expected.Arguments == current.Arguments &&
            expected.ClipText == current.ClipText &&
            expected.Note == current.Note &&
            Math.Abs(expected.X - current.X) < 0.0001 &&
            Math.Abs(expected.Y - current.Y) < 0.0001 &&
            Math.Abs(expected.Width - current.Width) < 0.0001 &&
            Math.Abs(expected.Height - current.Height) < 0.0001 &&
            expected.UseInvertedThemeColors == current.UseInvertedThemeColors &&
            expected.CreatedAtUtc == current.CreatedAtUtc;
    }
}
