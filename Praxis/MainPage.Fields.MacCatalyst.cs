using Praxis.Core.Logic;
#if MACCATALYST
using Foundation;
using UIKit;
#endif

namespace Praxis;

public partial class MainPage
{
#if MACCATALYST
    private static WeakReference<MainPage>? macLastActivePage;
    private static readonly IntPtr nsCursorClass = ObjcGetClass("NSCursor");
    private static readonly IntPtr pointingHandCursorSelector = SelRegisterName("pointingHandCursor");
    private static readonly IntPtr arrowCursorSelector = SelRegisterName("arrowCursor");
    private static readonly IntPtr setCursorSelector = SelRegisterName("set");
    private static readonly TimeSpan macActivationFocusWindow = UiTimingPolicy.MacActivationFocusWindow;
    private static readonly TimeSpan macActivationFocusRequestCoalesceDelay = UiTimingPolicy.MacActivationFocusRequestCoalesceDelay;
    private static readonly TimeSpan macSearchFocusUserIntentWindow = UiTimingPolicy.MacSearchFocusUserIntentWindow;

    private long macActivationFocusRequestId;
    private DateTimeOffset macActivationFocusSessionUntilUtc;
    private DateTimeOffset macSearchFocusUserIntentUntilUtc;
    private NSObject? macDidBecomeActiveObserver;
    private NSObject? macWillEnterForegroundObserver;
    private NSObject? macSceneDidActivateObserver;
    private NSObject? macSceneWillEnterForegroundObserver;
    private NSObject? macWindowDidBecomeKeyObserver;
    private NSObject? macWillResignActiveObserver;
    private NSObject? macDidEnterBackgroundObserver;
    private NSObject? macWindowDidResignKeyObserver;

    private static readonly ModalFocusTarget[] ModalFocusOrder =
    [
        ModalFocusTarget.Guid,
        ModalFocusTarget.Command,
        ModalFocusTarget.ButtonText,
        ModalFocusTarget.Tool,
        ModalFocusTarget.Arguments,
        ModalFocusTarget.ClipWord,
        ModalFocusTarget.Note,
        ModalFocusTarget.InvertThemeColors,
        ModalFocusTarget.CancelButton,
        ModalFocusTarget.SaveButton,
    ];

    private UIKeyCommand? modalEscapeKeyCommand;
    private UIKeyCommand? modalSaveKeyCommand;
    private UIKeyCommand? modalTabNextKeyCommand;
    private UIKeyCommand? modalTabPreviousKeyCommand;
    private UIKeyCommand? modalPrimaryActionKeyCommand;
    private UIKeyCommand? modalPrimaryActionAlternateKeyCommand;
    private static readonly string macEscapeKeyInput = ResolveMacKeyInput("InputEscape", "\u001B");
    private static readonly string macTabKeyInput = ResolveMacKeyInput("InputTab", "\t");
    private static readonly string macReturnKeyInput = ResolveMacKeyInput("InputReturn", "\r");
    private static readonly string? macEnterKeyInput = TryResolveMacKeyInput("InputEnter");
    private static readonly string macUpArrowKeyInput = ResolveMacKeyInput("InputUpArrow", "\uF700");
    private static readonly string macDownArrowKeyInput = ResolveMacKeyInput("InputDownArrow", "\uF701");
    private UIKeyCommand? commandSuggestionUpKeyCommand;
    private UIKeyCommand? commandSuggestionDownKeyCommand;
    private Microsoft.Maui.Dispatching.IDispatcherTimer? macMiddleButtonPollTimer;
    private bool macMiddleButtonWasDown;
    private bool macInitialCommandFocusApplied;
    private ModalFocusTarget? macPseudoFocusedModalTarget;
    private ContextMenuFocusTarget? macPseudoFocusedContextMenuTarget;
    private static readonly bool macDynamicKeyCommandRegistrationEnabled = false;
    private readonly UITextFieldDelegate macGuidReadOnlyDelegate = new MacGuidReadOnlyTextFieldDelegate();
    private UITextField? macGuidNativeTextField;
    private string macGuidLockedText = string.Empty;
    private bool macApplyingGuidTextLock;
    private bool macSuppressEditorTabFallback;

    private enum ModalFocusTarget
    {
        Guid,
        Command,
        ButtonText,
        Tool,
        Arguments,
        ClipWord,
        Note,
        InvertThemeColors,
        CancelButton,
        SaveButton,
    }

    private enum ContextMenuFocusTarget
    {
        Edit,
        Delete,
    }
#endif
}
