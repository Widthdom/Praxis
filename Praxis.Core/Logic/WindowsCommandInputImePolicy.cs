namespace Praxis.Core.Logic;

public static class WindowsCommandInputImePolicy
{
    private const uint ImeConversionNative = 0x0001;
    private const uint ImeConversionKatakana = 0x0002;
    private const uint ImeConversionFullShape = 0x0008;
    private const uint ImeConversionCharCode = 0x0020;
    private const uint ImeConversionHanjiConvert = 0x0040;
    private static readonly TimeSpan[] asciiImeNudgeDelays =
    [
        TimeSpan.Zero,
        TimeSpan.FromMilliseconds(28),
    ];
    private static readonly TimeSpan asciiImeReassertInterval = TimeSpan.FromMilliseconds(180);

    public static bool ShouldForceAsciiImeMode(bool isFocused) => isFocused;

    public static IReadOnlyList<TimeSpan> ResolveAsciiImeNudgeDelays(bool isFocused)
        => isFocused ? asciiImeNudgeDelays : [];

    public static bool ShouldReassertAsciiImeMode(bool isFocused, bool keepAsciiImeWhileFocused)
        => isFocused && keepAsciiImeWhileFocused;

    public static TimeSpan ResolveAsciiImeReassertInterval() => asciiImeReassertInterval;

    public static uint ResolveAsciiConversionMode(uint conversionMode)
        => conversionMode & ~(ImeConversionNative |
                              ImeConversionKatakana |
                              ImeConversionFullShape |
                              ImeConversionCharCode |
                              ImeConversionHanjiConvert);

    public static int ClampSelectionStart(int selectionStart, int textLength)
    {
        if (textLength <= 0)
        {
            return 0;
        }

        if (selectionStart <= 0)
        {
            return 0;
        }

        if (selectionStart >= textLength)
        {
            return textLength;
        }

        return selectionStart;
    }
}
