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
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\0")]
    [InlineData("command")]
    public void ShouldShow_ReturnsTrue_WhenValueExists(string value)
    {
        Assert.True(InputClearButtonVisibilityPolicy.ShouldShow(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("text")]
    [InlineData(" ")]
    [InlineData("\n")]
    public void ShouldShow_MatchesStringIsNullOrEmptyContract(string? value)
    {
        Assert.Equal(!string.IsNullOrEmpty(value), InputClearButtonVisibilityPolicy.ShouldShow(value));
    }
}
