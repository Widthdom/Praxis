using Praxis.Core.Models;

namespace Praxis.Services;

public interface IThemeService
{
    ThemeMode Current { get; }
    void Apply(ThemeMode mode);
}
