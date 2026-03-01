#if MACCATALYST
using Praxis.Controls;

namespace Praxis.Controls;

public class SearchEntryHandler : MacEntryHandler
{
    protected override MacEntryTextField CreatePlatformView()
    {
        return new SearchEntryTextField();
    }

    private sealed class SearchEntryTextField : MacEntryTextField
    {
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
