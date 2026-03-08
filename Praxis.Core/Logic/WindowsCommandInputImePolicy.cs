namespace Praxis.Core.Logic;

public static class WindowsCommandInputImePolicy
{
    private const uint ImeConversionNative = 0x0001;
    private const uint ImeConversionKatakana = 0x0002;
    private const uint ImeConversionFullShape = 0x0008;
    private const uint ImeConversionCharCode = 0x0020;
    private const uint ImeConversionHanjiConvert = 0x0040;
    private static readonly TimeSpan focusedInputScopeEnforcementInterval = TimeSpan.FromMilliseconds(60);

    public static TimeSpan FocusedInputScopeEnforcementInterval => focusedInputScopeEnforcementInterval;

    public static bool ShouldEnforceInputScope(bool isFocused) => isFocused;

    public static bool ShouldForceAsciiImeMode(bool isFocused) => isFocused;

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
