using Praxis.Core.Logic;

namespace Praxis.Tests;

public class EditorTabInsertionResolverTests
{
    [Fact]
    public void TryResolveNavigationAction_ReturnsTabNext_ForForwardTabInsertion()
    {
        var resolved = EditorTabInsertionResolver.TryResolveNavigationAction("abc", "a\tbc", out var action, out var insertedIndex);

        Assert.True(resolved);
        Assert.Equal("TabNext", action);
        Assert.Equal(1, insertedIndex);
    }

    [Fact]
    public void TryResolveNavigationAction_ReturnsTabPrevious_ForBackwardTabInsertion()
    {
        var oldText = "abc";
        var newText = "ab" + EditorTabInsertionResolver.BackwardTabCharacter + "c";
        var resolved = EditorTabInsertionResolver.TryResolveNavigationAction(oldText, newText, out var action, out var insertedIndex);

        Assert.True(resolved);
        Assert.Equal("TabPrevious", action);
        Assert.Equal(2, insertedIndex);
    }

    [Fact]
    public void TryResolveNavigationAction_ReturnsFalse_WhenInsertedCharacterIsNotTab()
    {
        var resolved = EditorTabInsertionResolver.TryResolveNavigationAction("abc", "axbc", out var action, out var insertedIndex);

        Assert.False(resolved);
        Assert.Equal(string.Empty, action);
        Assert.Equal(-1, insertedIndex);
    }

    [Fact]
    public void TryResolveNavigationAction_ReturnsFalse_WhenTextChangeIsNotSingleInsertion()
    {
        var resolved = EditorTabInsertionResolver.TryResolveNavigationAction("abc", "a", out var action, out var insertedIndex);

        Assert.False(resolved);
        Assert.Equal(string.Empty, action);
        Assert.Equal(-1, insertedIndex);
    }

    [Fact]
    public void TryResolveNavigationAction_ReturnsTrue_ForTabInsertionAtTextEnd()
    {
        var resolved = EditorTabInsertionResolver.TryResolveNavigationAction("abc", "abc\t", out var action, out var insertedIndex);

        Assert.True(resolved);
        Assert.Equal("TabNext", action);
        Assert.Equal(3, insertedIndex);
    }

    [Fact]
    public void TryResolveNavigationAction_TreatsNullOldText_AsEmpty()
    {
        var resolved = EditorTabInsertionResolver.TryResolveNavigationAction(null, "\t", out var action, out var insertedIndex);

        Assert.True(resolved);
        Assert.Equal("TabNext", action);
        Assert.Equal(0, insertedIndex);
    }

    [Fact]
    public void TryResolveNavigationAction_TreatsNullNewText_AsEmpty()
    {
        var resolved = EditorTabInsertionResolver.TryResolveNavigationAction("abc", null, out var action, out var insertedIndex);

        Assert.False(resolved);
        Assert.Equal(string.Empty, action);
        Assert.Equal(-1, insertedIndex);
    }

    [Fact]
    public void TryResolveNavigationAction_ReturnsFalse_WhenSingleCharacterDiffIsNotPureInsertion()
    {
        var resolved = EditorTabInsertionResolver.TryResolveNavigationAction("abc", "a\tbd", out var action, out var insertedIndex);

        Assert.False(resolved);
        Assert.Equal(string.Empty, action);
        Assert.Equal(1, insertedIndex);
    }

    [Fact]
    public void TryResolveNavigationAction_OverloadWithoutIndex_ReturnsExpectedAction()
    {
        var resolved = EditorTabInsertionResolver.TryResolveNavigationAction("abc", "a\tbc", out var action);

        Assert.True(resolved);
        Assert.Equal("TabNext", action);
    }

    [Fact]
    public void TryResolveNavigationAction_OverloadWithoutIndex_ReturnsFalse_ForUnresolvedDiff()
    {
        var resolved = EditorTabInsertionResolver.TryResolveNavigationAction("abc", "abxd", out var action);

        Assert.False(resolved);
        Assert.Equal(string.Empty, action);
    }
}
