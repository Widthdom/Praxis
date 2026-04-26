using System.Reflection;

namespace Praxis.Core.Logic;

/// <summary>
/// Classifies whether a MAUI PointerEventArgs.PlatformArgs (or any of its nested
/// GestureRecognizer / Event / CurrentEvent payloads) describes a secondary
/// (right) or middle (other-mouse) pointer press. Mirrors the reflection logic
/// in MainPage.PointerAndSelection.cs so the placement-area grab-cursor behavior
/// and other UI-side consumers can reuse the same test-covered rules.
/// </summary>
public static class PointerButtonClassifier
{
    public static bool IsSecondary(object? platformArgs)
        => platformArgs is not null && (
            IsSecondaryFromObject(platformArgs)
            || IsSecondaryFromObject(TryGetProperty(platformArgs, "GestureRecognizer"))
            || IsSecondaryFromObject(TryGetProperty(platformArgs, "Event")));

    public static bool IsMiddle(object? platformArgs)
        => platformArgs is not null && (
            IsMiddleFromObject(platformArgs)
            || IsMiddleFromObject(TryGetProperty(platformArgs, "GestureRecognizer"))
            || IsMiddleFromObject(TryGetProperty(platformArgs, "Event")));

    public static bool IsPrimaryOnly(object? platformArgs)
        => !IsSecondary(platformArgs) && !IsMiddle(platformArgs);

    private static bool IsSecondaryFromObject(object? source)
    {
        if (source is null)
        {
            return false;
        }

        var rightPressed = TryGetProperty(source, "IsRightButtonPressed");
        if (rightPressed is bool pressed && pressed)
        {
            return true;
        }

        var pressedButton = TryGetProperty(source, "PressedButton");
        if (IsSecondaryButtonValue(pressedButton))
        {
            return true;
        }

        var button = TryGetProperty(source, "Button");
        if (IsSecondaryButtonValue(button))
        {
            return true;
        }

        var buttons = TryGetProperty(source, "Buttons");
        if (IsSecondaryButtonValue(buttons))
        {
            return true;
        }

        var buttonMask = TryGetProperty(source, "ButtonMask");
        if (TryConvertToUInt64(buttonMask, out var mask) && (mask & 0x2) != 0)
        {
            return true;
        }

        var buttonNumber = TryGetProperty(source, "ButtonNumber");
        if (TryConvertToInt32(buttonNumber, out var number) && number == 1)
        {
            return true;
        }

        var currentEvent = TryGetProperty(source, "CurrentEvent");
        if (currentEvent is not null && !ReferenceEquals(currentEvent, source))
        {
            return IsSecondaryFromObject(currentEvent);
        }

        return false;
    }

    private static bool IsMiddleFromObject(object? source)
    {
        if (source is null)
        {
            return false;
        }

        var eventTypeText = TryGetProperty(source, "Type")?.ToString() ?? string.Empty;
        if (eventTypeText.Contains("OtherMouse", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var middlePressed = TryGetProperty(source, "IsMiddleButtonPressed");
        if (middlePressed is bool pressed && pressed)
        {
            return true;
        }

        var pressedButton = TryGetProperty(source, "PressedButton");
        if (IsMiddleButtonValue(pressedButton))
        {
            return true;
        }

        var button = TryGetProperty(source, "Button");
        if (IsMiddleButtonValue(button))
        {
            return true;
        }

        var buttons = TryGetProperty(source, "Buttons");
        if (IsMiddleButtonValue(buttons))
        {
            return true;
        }

        var buttonNumber = TryGetProperty(source, "ButtonNumber");
        if (TryConvertToInt32(buttonNumber, out var number) && number >= 2)
        {
            return true;
        }

        var looksLikeOtherMouse = eventTypeText.Contains("OtherMouse", StringComparison.OrdinalIgnoreCase);

        var buttonMask = TryGetProperty(source, "ButtonMask");
        if (TryConvertToUInt64(buttonMask, out var mask))
        {
            if ((mask & 0x4) != 0 || (mask & 0x8) != 0 || (mask & 0x10) != 0)
            {
                return true;
            }

            if ((mask & 0x2) != 0 && looksLikeOtherMouse)
            {
                return true;
            }
        }

        var currentEvent = TryGetProperty(source, "CurrentEvent");
        if (currentEvent is not null && !ReferenceEquals(currentEvent, source))
        {
            return IsMiddleFromObject(currentEvent);
        }

        return false;
    }

    private static bool IsMiddleButtonValue(object? value)
    {
        if (value is null)
        {
            return false;
        }

        var text = value.ToString() ?? string.Empty;
        if (text.Contains("Middle", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Auxiliary", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Center", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Tertiary", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Other", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Button2", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Button3", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(text, out var number) && (number == 2 || number == 3);
    }

    private static bool IsSecondaryButtonValue(object? value)
    {
        if (value is null)
        {
            return false;
        }

        if (IsMiddleButtonValue(value))
        {
            return false;
        }

        var text = value.ToString() ?? string.Empty;
        if (text.Contains("Secondary", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Right", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Button1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(text, out var number) && number == 1;
    }

    private static object? TryGetProperty(object? source, string propertyName)
    {
        if (source is null)
        {
            return null;
        }

        var prop = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(source);
    }

    private static bool TryConvertToUInt64(object? value, out ulong number)
    {
        switch (value)
        {
            case null:
                number = 0;
                return false;
            case ulong unsignedLong:
                number = unsignedLong;
                return true;
            case Enum enumValue:
                number = Convert.ToUInt64(enumValue);
                return true;
            default:
                return ulong.TryParse(value.ToString(), out number);
        }
    }

    private static bool TryConvertToInt32(object? value, out int number)
    {
        switch (value)
        {
            case null:
                number = 0;
                return false;
            case int signed:
                number = signed;
                return true;
            case Enum enumValue:
                number = Convert.ToInt32(enumValue);
                return true;
            default:
                return int.TryParse(value.ToString(), out number);
        }
    }
}
