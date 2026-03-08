#if MACCATALYST
using CoreGraphics;

namespace Praxis.Controls;

public class SearchEntryHandler : MacEntryHandler
{
    protected override MacEntryTextField CreatePlatformView()
    {
        return new SearchEntryTextField();
    }

    private sealed class SearchEntryTextField : MacEntryTextField
    {
        protected override nfloat TextInsetRight => 40;

        public override bool BecomeFirstResponder()
        {
            if (!MainPage.ShouldAllowMacSearchEntryFocus())
            {
                return false;
            }

            return base.BecomeFirstResponder();
        }
    }
}
#endif
