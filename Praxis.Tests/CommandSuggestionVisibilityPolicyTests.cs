using Praxis.Core.Logic;

namespace Praxis.Tests;

public class CommandSuggestionVisibilityPolicyTests
{
    [Fact]
    public void ShouldCloseOnContextMenuOpen_ReturnsTrue_WhenSuggestionIsOpen()
    {
        var shouldClose = CommandSuggestionVisibilityPolicy.ShouldCloseOnContextMenuOpen(isSuggestionOpen: true);
        Assert.True(shouldClose);
    }

    [Fact]
    public void ShouldCloseOnContextMenuOpen_ReturnsFalse_WhenSuggestionIsClosed()
    {
        var shouldClose = CommandSuggestionVisibilityPolicy.ShouldCloseOnContextMenuOpen(isSuggestionOpen: false);
        Assert.False(shouldClose);
    }
}
