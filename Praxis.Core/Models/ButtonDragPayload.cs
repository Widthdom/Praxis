namespace Praxis.Core.Models;

public sealed class ButtonDragPayload
{
    public LauncherButtonModel? Button { get; init; }

    public InteractionStatus Status { get; init; }

    public double TotalX { get; init; }

    public double TotalY { get; init; }
}
