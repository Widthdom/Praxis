using Praxis.Core.Logic;

namespace Praxis.Tests;

public class UiTimingPolicyTests
{
    [Fact]
    public void KeyDelays_AreStableAndNamed()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(24), UiTimingPolicy.WindowsFocusRestorePrimaryDelay);
        Assert.Equal(TimeSpan.FromMilliseconds(120), UiTimingPolicy.WindowsFocusRestoreSecondaryDelay);
        Assert.Equal(TimeSpan.FromMilliseconds(900), UiTimingPolicy.MacActivationFocusWindow);
        Assert.Equal(TimeSpan.FromMilliseconds(800), UiTimingPolicy.MacSearchFocusUserIntentWindow);
        Assert.Equal(TimeSpan.FromMilliseconds(60), UiTimingPolicy.ModalOpenInitialFocusDelay);
        Assert.Equal(TimeSpan.FromMilliseconds(16), UiTimingPolicy.MacMiddleButtonPollingInterval);
        Assert.Equal(500, UiTimingPolicy.MacActivationSuppressionWindowMs);
    }

    [Fact]
    public void RelatedDelays_PreserveExpectedOrdering()
    {
        Assert.True(UiTimingPolicy.WindowsFocusRestorePrimaryDelay < UiTimingPolicy.WindowsFocusRestoreSecondaryDelay);
        Assert.True(UiTimingPolicy.ContextMenuFocusInitialDelay < UiTimingPolicy.ContextMenuFocusRetryDelay);
        Assert.True(UiTimingPolicy.MacActivationFocusRetryFirstDelay < UiTimingPolicy.MacActivationFocusRetrySecondDelay);
        Assert.True(UiTimingPolicy.MacActivationFocusRetrySecondDelay < UiTimingPolicy.MacActivationFocusRetryThirdDelay);
    }
}
