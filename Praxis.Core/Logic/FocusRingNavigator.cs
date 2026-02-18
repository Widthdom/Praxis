namespace Praxis.Core.Logic;

public static class FocusRingNavigator
{
    public static int GetNextIndex(int currentIndex, int itemCount, bool forward)
    {
        if (itemCount <= 0)
        {
            return -1;
        }

        if (currentIndex < 0 || currentIndex >= itemCount)
        {
            return forward ? 0 : itemCount - 1;
        }

        var step = forward ? 1 : -1;
        return (currentIndex + step + itemCount) % itemCount;
    }
}
