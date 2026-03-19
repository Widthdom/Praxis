namespace Praxis.Core.Logic;

public static class UiTimingPolicy
{
    public const int MacActivationSuppressionWindowMs = 500;
    public const int CopyNoticeHoldDurationMs = 320;
    public const uint StatusFlashInDurationMs = 120;
    public const uint StatusFlashOutDurationMs = 220;

    public static readonly TimeSpan WindowsFocusRestorePrimaryDelay = TimeSpan.FromMilliseconds(24);
    public static readonly TimeSpan WindowsFocusRestoreSecondaryDelay = TimeSpan.FromMilliseconds(120);
    public static readonly TimeSpan MacActivationFocusWindow = TimeSpan.FromMilliseconds(900);
    public static readonly TimeSpan MacActivationFocusRequestCoalesceDelay = TimeSpan.FromMilliseconds(45);
    public static readonly TimeSpan MacSearchFocusUserIntentWindow = TimeSpan.FromMilliseconds(800);
    public static readonly TimeSpan MacActivationFocusRetryFirstDelay = TimeSpan.FromMilliseconds(20);
    public static readonly TimeSpan MacActivationFocusRetrySecondDelay = TimeSpan.FromMilliseconds(160);
    public static readonly TimeSpan MacActivationFocusRetryThirdDelay = TimeSpan.FromMilliseconds(420);
    public static readonly TimeSpan MacActivationFocusReassertDelay = TimeSpan.FromMilliseconds(100);
    public static readonly TimeSpan ContextMenuFocusInitialDelay = TimeSpan.FromMilliseconds(40);
    public static readonly TimeSpan ContextMenuFocusRetryDelay = TimeSpan.FromMilliseconds(140);
    public static readonly TimeSpan EditorOpenFocusDelay = TimeSpan.FromMilliseconds(60);
    public static readonly TimeSpan MacEditorKeyCommandsRetryDelay = TimeSpan.FromMilliseconds(220);
    public static readonly TimeSpan CommandNotFoundRefocusDelay = TimeSpan.FromMilliseconds(20);
    public static readonly TimeSpan ConflictDialogFocusRetryDelay = TimeSpan.FromMilliseconds(120);
    public static readonly TimeSpan ConflictDialogEditorFocusRestoreDelay = TimeSpan.FromMilliseconds(40);
    public static readonly TimeSpan ModalOpenInitialFocusDelay = TimeSpan.FromMilliseconds(60);
    public static readonly TimeSpan ModalOpenMacCaretRetryDelay = TimeSpan.FromMilliseconds(40);
    public static readonly TimeSpan MacInitialCommandFocusRetryDelay = TimeSpan.FromMilliseconds(140);
    public static readonly TimeSpan MacMiddleButtonPollingInterval = TimeSpan.FromMilliseconds(16);
}
