using Praxis.Core.Logic;

namespace Praxis.Tests;

public class ThemeShortcutModeResolverTests
{
    [Theory]
    [InlineData("l", "Light")]
    [InlineData("L", "Light")]
    [InlineData("d", "Dark")]
    [InlineData("D", "Dark")]
    [InlineData("h", "System")]
    [InlineData("H", "System")]
    public void TryResolveModeFromMacKeyInput_ReturnsExpectedMode(string input, string expected)
    {
        var resolved = ThemeShortcutModeResolver.TryResolveModeFromMacKeyInput(input, out var mode);
        Assert.True(resolved);
        Assert.Equal(expected, mode);
    }

    [Fact]
    public void TryResolveModeFromMacKeyInput_ReturnsFalse_ForUnsupportedInput()
    {
        var resolved = ThemeShortcutModeResolver.TryResolveModeFromMacKeyInput("x", out var mode);
        Assert.False(resolved);
        Assert.Equal(string.Empty, mode);
    }
}
