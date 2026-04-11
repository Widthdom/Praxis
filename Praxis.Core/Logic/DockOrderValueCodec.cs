namespace Praxis.Core.Logic;

public static class DockOrderValueCodec
{
    public static IReadOnlyList<Guid> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var normalizedValue = TrimEnclosingQuotes(value.Trim());
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return [];
        }

        var ids = new List<Guid>();
        var seen = new HashSet<Guid>();
        var parts = normalizedValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var candidate = TrimEnclosingQuotes(part);
            if (!Guid.TryParse(candidate, out var id) || id == Guid.Empty || !seen.Add(id))
            {
                continue;
            }

            ids.Add(id);
        }

        return ids;
    }

    public static string Serialize(IEnumerable<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var seen = new HashSet<Guid>();
        var values = new List<string>();
        foreach (var id in ids)
        {
            if (id == Guid.Empty || !seen.Add(id))
            {
                continue;
            }

            values.Add(id.ToString());
        }

        return string.Join(",", values);
    }

    private static string TrimEnclosingQuotes(string value)
    {
        if (value.Length >= 2)
        {
            var first = value[0];
            var last = value[^1];
            if ((first == '"' || first == '\'') && last == first)
            {
                return value[1..^1].Trim();
            }
        }

        return value;
    }
}
