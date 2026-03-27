using Praxis.Core.Logic;

namespace Praxis.Tests;

public class WindowsNativeFocusSafetyPolicyTests
{
    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(false, false, false, false)]
    public void ShouldApplyNativeFocus_ReturnsExpectedValue(
        bool hasTextBox,
        bool isLoaded,
        bool hasXamlRoot,
        bool expected)
    {
        var result = WindowsNativeFocusSafetyPolicy.ShouldApplyNativeFocus(
            hasTextBox: hasTextBox,
            isLoaded: isLoaded,
            hasXamlRoot: hasXamlRoot);

        Assert.Equal(expected, result);
    }
}
