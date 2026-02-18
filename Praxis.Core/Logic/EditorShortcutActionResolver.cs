namespace Praxis.Core.Logic;

public static class EditorShortcutActionResolver
{
    public static string ResolveTabNavigationAction(bool shiftDown)
    {
        return shiftDown ? "TabPrevious" : "TabNext";
    }
}
