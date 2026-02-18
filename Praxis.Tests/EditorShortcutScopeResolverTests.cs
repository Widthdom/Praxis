using Praxis.Core.Logic;

namespace Praxis.Tests;

public class EditorShortcutScopeResolverTests
{
    [Fact]
    public void IsEditorShortcutScopeActive_ReturnsFalse_WhenNoOverlayIsOpen()
    {
        var active = EditorShortcutScopeResolver.IsEditorShortcutScopeActive(
            isConflictDialogOpen: false,
            isContextMenuOpen: false,
            isEditorOpen: false);

        Assert.False(active);
    }

    [Fact]
    public void IsEditorShortcutScopeActive_ReturnsTrue_WhenConflictDialogIsOpen()
    {
        var active = EditorShortcutScopeResolver.IsEditorShortcutScopeActive(
            isConflictDialogOpen: true,
            isContextMenuOpen: false,
            isEditorOpen: false);

        Assert.True(active);
    }

    [Fact]
    public void IsEditorShortcutScopeActive_ReturnsTrue_WhenContextMenuIsOpen()
    {
        var active = EditorShortcutScopeResolver.IsEditorShortcutScopeActive(
            isConflictDialogOpen: false,
            isContextMenuOpen: true,
            isEditorOpen: false);

        Assert.True(active);
    }

    [Fact]
    public void IsEditorShortcutScopeActive_ReturnsTrue_WhenEditorIsOpen()
    {
        var active = EditorShortcutScopeResolver.IsEditorShortcutScopeActive(
            isConflictDialogOpen: false,
            isContextMenuOpen: false,
            isEditorOpen: true);

        Assert.True(active);
    }
}
