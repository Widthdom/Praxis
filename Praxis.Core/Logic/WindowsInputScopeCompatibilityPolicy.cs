namespace Praxis.Core.Logic;

public static class WindowsInputScopeCompatibilityPolicy
{
    public static bool ShouldDisableInputScopeOnException(Exception exception)
    {
        return exception is ArgumentException;
    }
}
