namespace Praxis.Core.Logic;

public sealed class ActionHistory<T>
{
    private readonly int capacity;
    private readonly List<T> undoStack = [];
    private readonly List<T> redoStack = [];

    public ActionHistory(int capacity = 100)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        this.capacity = capacity;
    }

    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;

    public void Push(T action)
    {
        undoStack.Add(action);
        if (undoStack.Count > capacity)
        {
            undoStack.RemoveAt(0);
        }

        redoStack.Clear();
    }

    public bool TryBeginUndo(out T action)
    {
        if (undoStack.Count == 0)
        {
            action = default!;
            return false;
        }

        var index = undoStack.Count - 1;
        action = undoStack[index];
        undoStack.RemoveAt(index);
        return true;
    }

    public void CompleteUndo(T action, bool applied)
    {
        if (applied)
        {
            redoStack.Add(action);
            if (redoStack.Count > capacity)
            {
                redoStack.RemoveAt(0);
            }

            return;
        }

        undoStack.Add(action);
        if (undoStack.Count > capacity)
        {
            undoStack.RemoveAt(0);
        }
    }

    public bool TryBeginRedo(out T action)
    {
        if (redoStack.Count == 0)
        {
            action = default!;
            return false;
        }

        var index = redoStack.Count - 1;
        action = redoStack[index];
        redoStack.RemoveAt(index);
        return true;
    }

    public void CompleteRedo(T action, bool applied)
    {
        if (applied)
        {
            undoStack.Add(action);
            if (undoStack.Count > capacity)
            {
                undoStack.RemoveAt(0);
            }

            return;
        }

        redoStack.Add(action);
        if (redoStack.Count > capacity)
        {
            redoStack.RemoveAt(0);
        }
    }

    public void Clear()
    {
        undoStack.Clear();
        redoStack.Clear();
    }
}
