using Praxis.Core.Logic;

namespace Praxis.Tests;

public class ActionHistoryTests
{
    [Fact]
    public void Constructor_Throws_WhenCapacityIsNotPositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ActionHistory<string>(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ActionHistory<string>(-1));
    }

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
    public void FailedRedo_ReturnsActionBackToRedoStack()
    {
        var history = new ActionHistory<string>();
        history.Push("edit");

        Assert.True(history.TryBeginUndo(out var action));
        history.CompleteUndo(action, applied: true);

        Assert.True(history.TryBeginRedo(out var redoAction));
        history.CompleteRedo(redoAction, applied: false);

        Assert.True(history.TryBeginRedo(out var restored));
        Assert.Equal("edit", restored);
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

    [Fact]
    public void Capacity_RemovesOldestRedoActions()
    {
        var history = new ActionHistory<string>(capacity: 2);
        history.CompleteUndo("first", applied: true);
        history.CompleteUndo("second", applied: true);
        history.CompleteUndo("third", applied: true);

        Assert.True(history.TryBeginRedo(out var redoLatest));
        Assert.Equal("third", redoLatest);
        Assert.True(history.TryBeginRedo(out var redoNext));
        Assert.Equal("second", redoNext);
        Assert.False(history.TryBeginRedo(out _));
    }

    [Fact]
    public void Clear_RemovesBothUndoAndRedoState()
    {
        var history = new ActionHistory<string>();
        history.Push("one");
        history.Push("two");
        Assert.True(history.TryBeginUndo(out var undone));
        history.CompleteUndo(undone, applied: true);

        history.Clear();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.False(history.TryBeginUndo(out _));
        Assert.False(history.TryBeginRedo(out _));
    }
}
