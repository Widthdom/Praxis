namespace Praxis.Core.Logic;

public static class DockOrderValueCodec
{
    public static IReadOnlyList<Guid> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var ids = new List<Guid>();
        var seen = new HashSet<Guid>();
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (!Guid.TryParse(part, out var id) || id == Guid.Empty || !seen.Add(id))
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
}
