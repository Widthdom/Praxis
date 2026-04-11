using Praxis.Core.Logic;

namespace Praxis.Tests;

public class WindowsNativeFocusSafetyPolicyTests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
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

    [Fact]
    public void ShouldApplyNativeFocus_RequiresLiveTextBox_ForAllCombinations()
    {
        foreach (var hasTextBox in new[] { false, true })
        {
            foreach (var isLoaded in new[] { false, true })
            {
                foreach (var hasXamlRoot in new[] { false, true })
                {
                    var result = WindowsNativeFocusSafetyPolicy.ShouldApplyNativeFocus(hasTextBox, isLoaded, hasXamlRoot);
                    var expected = hasTextBox && isLoaded && hasXamlRoot;
                    Assert.Equal(expected, result);
                }
            }
        }
    }
}
