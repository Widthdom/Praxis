namespace Praxis.Core.Logic;

public static class SearchFocusGuardPolicy
{
    public static bool ShouldAllowSearchFocus(
        bool shouldFocusMainCommand,
        bool isAppForeground,
        bool isUserInitiated)
    {
        if (!shouldFocusMainCommand || !isAppForeground)
        {
            return true;
        }

        return isUserInitiated;
    }
}
