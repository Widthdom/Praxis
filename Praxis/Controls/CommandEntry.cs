using Microsoft.Maui.Controls;

namespace Praxis.Controls;

public class CommandEntry : Entry
{
    public static readonly BindableProperty EnableCommandNavigationShortcutsProperty =
        BindableProperty.Create(
            nameof(EnableCommandNavigationShortcuts),
            typeof(bool),
            typeof(CommandEntry),
            true);

    public static readonly BindableProperty EnableNativeActivationFocusProperty =
        BindableProperty.Create(
            nameof(EnableNativeActivationFocus),
            typeof(bool),
            typeof(CommandEntry),
            true);

    public bool EnableCommandNavigationShortcuts
    {
        get => (bool)GetValue(EnableCommandNavigationShortcutsProperty);
        set => SetValue(EnableCommandNavigationShortcutsProperty, value);
    }

    public bool EnableNativeActivationFocus
    {
        get => (bool)GetValue(EnableNativeActivationFocusProperty);
        set => SetValue(EnableNativeActivationFocusProperty, value);
    }

    public CommandEntry()
    {
        Keyboard = Keyboard.Plain;
    }
}
