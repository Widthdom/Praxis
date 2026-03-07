using Praxis.Core.Logic;

namespace Praxis.Tests;

public class InputClearButtonVisibilityPolicyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ShouldShow_ReturnsFalse_WhenValueIsNullOrEmpty(string? value)
    {
        Assert.False(InputClearButtonVisibilityPolicy.ShouldShow(value));
    }

    [Theory]
    [InlineData("a")]
    [InlineData(" ")]
    [InlineData("command")]
    public void ShouldShow_ReturnsTrue_WhenValueExists(string value)
    {
        Assert.True(InputClearButtonVisibilityPolicy.ShouldShow(value));
    }
}
