using Praxis.Core.Models;

namespace Praxis.Core.Logic;

public static class LauncherButtonOrderPolicy
{
    public static List<LauncherButtonRecord> ToSortedList(IEnumerable<LauncherButtonRecord> buttons)
    {
        ArgumentNullException.ThrowIfNull(buttons);

        return buttons
            .OfType<LauncherButtonRecord>()
            .OrderBy(x => x.Y)
            .ThenBy(x => x.X)
            .ToList();
    }
}
