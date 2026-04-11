using System.Globalization;

namespace Praxis.Core.Logic;

public static class StateSyncPayloadParser
{
    public static bool TryParse(string? payload, out string sourceInstanceId, out DateTime timestampUtc)
    {
        sourceInstanceId = string.Empty;
        timestampUtc = default;

        var trimmed = payload?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return false;
        }

        var parts = trimmed.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
        {
            return false;
        }

        if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
        {
            return false;
        }

        sourceInstanceId = parts[0];
        if (string.IsNullOrWhiteSpace(sourceInstanceId))
        {
            sourceInstanceId = string.Empty;
            return false;
        }

        timestampUtc = new DateTime(ticks, DateTimeKind.Utc);
        return true;
    }
}
