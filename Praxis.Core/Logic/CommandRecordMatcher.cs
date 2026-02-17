using Praxis.Core.Models;

namespace Praxis.Core.Logic;

public static class CommandRecordMatcher
{
    public static IReadOnlyList<LauncherButtonRecord> FindMatches(
        IEnumerable<LauncherButtonRecord> records,
        string commandInput)
    {
        if (records is null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        var normalizedInput = Normalize(commandInput);
        if (string.IsNullOrEmpty(normalizedInput))
        {
            return [];
        }

        return records
            .Where(x => string.Equals(Normalize(x.Command), normalizedInput, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string Normalize(string? value)
        => (value ?? string.Empty).Trim();
}
