using Praxis.Core.Models;

namespace Praxis.ViewModels;

internal sealed class ButtonHistoryAction
{
    public required string Description { get; init; }
    public required IReadOnlyList<ButtonHistoryMutation> Mutations { get; init; }
    public IReadOnlyList<Guid>? DockOrderBefore { get; init; }
    public IReadOnlyList<Guid>? DockOrderAfter { get; init; }
}

internal sealed class ButtonHistoryMutation
{
    public required Guid Id { get; init; }
    public LauncherButtonRecord? Before { get; init; }
    public LauncherButtonRecord? After { get; init; }
}
