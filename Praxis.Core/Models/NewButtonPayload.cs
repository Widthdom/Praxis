namespace Praxis.Core.Models;

public sealed class NewButtonPayload
{
    public double X { get; init; }

    public double Y { get; init; }

    public bool HasPosition { get; init; }

    public string Arguments { get; init; } = string.Empty;
}
