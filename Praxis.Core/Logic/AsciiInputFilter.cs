namespace Praxis.Core.Logic;

public static class AsciiInputFilter
{
    public static bool ShouldBlockMarkedText(string? markedText)
    {
        if (string.IsNullOrEmpty(markedText))
        {
            return false;
        }

        return !IsAsciiOnly(markedText);
    }

    public static bool IsAsciiOnly(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        foreach (var character in value)
        {
            if (character > 0x7F)
            {
                return false;
            }
        }

        return true;
    }

    public static string FilterToAscii(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (IsAsciiOnly(value))
        {
            return value;
        }

        var buffer = new char[value.Length];
        var writeIndex = 0;
        foreach (var character in value)
        {
            if (character <= 0x7F)
            {
                buffer[writeIndex++] = character;
            }
        }

        return new string(buffer, 0, writeIndex);
    }
}
