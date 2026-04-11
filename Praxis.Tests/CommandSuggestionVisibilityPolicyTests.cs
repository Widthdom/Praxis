using Praxis.Core.Logic;

namespace Praxis.Tests;

public class CommandSuggestionVisibilityPolicyTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldCloseOnContextMenuOpen_MirrorsSuggestionOpenState(bool isSuggestionOpen)
    {
        var shouldClose = CommandSuggestionVisibilityPolicy.ShouldCloseOnContextMenuOpen(isSuggestionOpen);
        Assert.Equal(isSuggestionOpen, shouldClose);
    }
}
