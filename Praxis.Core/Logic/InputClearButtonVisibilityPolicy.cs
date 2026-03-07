namespace Praxis.Core.Logic;

public static class InputClearButtonVisibilityPolicy
{
    public static bool ShouldShow(string? value)
        => !string.IsNullOrEmpty(value);
}
