#if WINDOWS
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Input;

namespace Praxis.Controls;

public class CommandEntryHandler : EntryHandler
{
    protected override void ConnectHandler(Microsoft.UI.Xaml.Controls.TextBox platformView)
    {
        base.ConnectHandler(platformView);
        var scope = new InputScope();
        scope.Names.Add(new InputScopeName(InputScopeNameValue.AlphanumericHalfWidth));
        platformView.InputScope = scope;
    }
}
#endif
