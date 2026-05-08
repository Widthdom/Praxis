#if WINDOWS
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Praxis.Controls;

public class SearchEntryHandler : EntryHandler
{
    protected override void ConnectHandler(TextBox platformView)
    {
        base.ConnectHandler(platformView);
        WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(platformView);
        platformView.GotFocus += PlatformView_GotFocus;
        platformView.LostFocus += PlatformView_LostFocus;
        platformView.SelectionChanged += PlatformView_SelectionChanged;
    }

    protected override void DisconnectHandler(TextBox platformView)
    {
        platformView.GotFocus -= PlatformView_GotFocus;
        platformView.LostFocus -= PlatformView_LostFocus;
        platformView.SelectionChanged -= PlatformView_SelectionChanged;
        base.DisconnectHandler(platformView);
    }

    private static void PlatformView_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(textBox);
        }
    }

    private static void PlatformView_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(textBox);
        }
    }

    private static void PlatformView_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            WindowsTextBoxVisualPolicy.ApplyInputCaretContrast(textBox);
        }
    }
}
#endif
