namespace Praxis.Core.Logic;

public static class QuickLookPreviewFormatter
{
    public const string EmptyValuePlaceholder = "-";

    public static string BuildLine(string label, string? value, int maxLength = 96)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        return $"{label}: {FormatValue(value, maxLength)}";
    }

    public static string FormatValue(string? value, int maxLength = 96)
    {
        if (maxLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return EmptyValuePlaceholder;
        }

        var normalized = string.Join(" ", value
            .Split(['\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries));

        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        if (maxLength <= 1)
        {
            return "...";
        }

        return normalized[..(maxLength - 1)] + "...";
    }
}
