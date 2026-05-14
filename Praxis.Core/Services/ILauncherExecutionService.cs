using Praxis.Core.Models;

namespace Praxis.Core.Services;

public interface ILauncherExecutionService
{
    Task<LauncherExecutionResult> ExecuteAsync(
        LauncherButtonModel button,
        CancellationToken cancellationToken = default);
}
