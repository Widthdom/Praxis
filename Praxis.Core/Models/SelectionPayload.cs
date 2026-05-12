namespace Praxis.Core.Models;

public sealed class SelectionPayload
{
    public double StartX { get; init; }

    public double StartY { get; init; }

    public double CurrentX { get; init; }

    public double CurrentY { get; init; }

    public InteractionStatus Status { get; init; }
}
