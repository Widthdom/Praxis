namespace Microsoft.Maui.ApplicationModel
{
    public static class MainThread
    {
        public static bool IsMainThread => true;

        public static void BeginInvokeOnMainThread(Action action)
        {
            action();
        }
    }
}

namespace Praxis.Behaviors
{
    public enum GestureStatus
    {
        Started,
        Running,
        Completed,
        Canceled,
    }

    public sealed record DragPayload(object? Item, GestureStatus Status, double TotalX, double TotalY);

    public sealed record SelectionPayload(double StartX, double StartY, double CurrentX, double CurrentY, GestureStatus Status);
}

namespace Praxis.ViewModels
{
    public enum ScrollOrientation
    {
        Neither,
        Horizontal,
        Vertical,
        Both,
    }

    public readonly record struct Rect(double X, double Y, double Width, double Height);

    public readonly record struct Size(double Width, double Height);
}
