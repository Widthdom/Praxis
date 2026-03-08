using Praxis.Core.Logic;

namespace Praxis.Tests;

public class ActionHistoryTests
{
    [Fact]
    public void Push_ThenUndoRedo_FollowsLifoOrder()
    {
        var history = new ActionHistory<string>(capacity: 4);
        history.Push("a");
        history.Push("b");

        Assert.True(history.TryBeginUndo(out var undoAction));
        Assert.Equal("b", undoAction);

        history.CompleteUndo(undoAction, applied: true);

        Assert.True(history.TryBeginRedo(out var redoAction));
        Assert.Equal("b", redoAction);
    }

    [Fact]
    public void FailedUndo_ReturnsActionBackToUndoStack()
    {
        var history = new ActionHistory<string>();
        history.Push("edit");

        Assert.True(history.TryBeginUndo(out var action));
        history.CompleteUndo(action, applied: false);

        Assert.True(history.TryBeginUndo(out var restored));
        Assert.Equal("edit", restored);
    }

    [Fact]
    public void Push_AfterUndo_ClearsRedoStack()
    {
        var history = new ActionHistory<string>();
        history.Push("one");
        history.Push("two");

        Assert.True(history.TryBeginUndo(out var action));
        history.CompleteUndo(action, applied: true);

        history.Push("three");

        Assert.False(history.TryBeginRedo(out _));
    }

    [Fact]
    public void Capacity_RemovesOldestUndoActions()
    {
        var history = new ActionHistory<string>(capacity: 2);
        history.Push("first");
        history.Push("second");
        history.Push("third");

        Assert.True(history.TryBeginUndo(out var latest));
        Assert.Equal("third", latest);
        history.CompleteUndo(latest, applied: false);

        Assert.True(history.TryBeginUndo(out var next));
        Assert.Equal("third", next);
        history.CompleteUndo(next, applied: true);

        Assert.True(history.TryBeginUndo(out var remaining));
        Assert.Equal("second", remaining);
    }
}
