using Praxis.Core.Logic;

namespace Praxis;

public partial class MainPage
{
#if WINDOWS
    private Microsoft.UI.Xaml.UIElement? capturedElement;
    private Microsoft.UI.Xaml.Controls.TextBox? commandTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? searchTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? modalGuidTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? modalCommandTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? modalButtonTextTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? modalToolTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? modalArgumentsTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? modalClipWordTextBox;
    private Microsoft.UI.Xaml.Controls.TextBox? modalNoteTextBox;
    private bool windowsSelectAllOnTabNavigationPending;
    private Microsoft.UI.Xaml.UIElement? pageNativeElement;
    private Microsoft.UI.Xaml.Input.KeyEventHandler? pageKeyDownHandler;
    private bool windowsEditorFocusRestorePending;
    private bool windowsConflictFocusRestorePending;
    private static readonly TimeSpan windowsFocusRestorePrimaryDelay = UiTimingPolicy.WindowsFocusRestorePrimaryDelay;
    private static readonly TimeSpan windowsFocusRestoreSecondaryDelay = UiTimingPolicy.WindowsFocusRestoreSecondaryDelay;
    private WindowsModalActionFocusTarget? windowsModalActionFocusTarget;
    private Microsoft.UI.Xaml.Window? windowsNativeWindow;
    private Microsoft.UI.Windowing.AppWindow? windowsAppWindow;
    private Microsoft.UI.Windowing.OverlappedPresenter? windowsOverlappedPresenter;
    private bool windowsTitleBarConfigured;
    private bool windowsTitleBarMaximized;

    private enum WindowsModalActionFocusTarget
    {
        Cancel,
        Save,
    }
#endif
}
