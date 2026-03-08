using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Praxis.Core.Logic;

namespace Praxis.ViewModels;

public partial class MainViewModel
{
    [RelayCommand]
    private async Task RateAppAsync()
    {
        IsRatingPromptVisible = false;
        await repository.SetRatingInfoAsync(AppRatingState.Done, deferredAtCount: 0);
        try
        {
            await Launcher.OpenAsync("https://github.com/Widthdom/Praxis/releases");
        }
        catch
        {
        }
    }

    [RelayCommand]
    private async Task DeferRatingAsync()
    {
        IsRatingPromptVisible = false;
        var launchCount = await repository.GetLaunchCountAsync();
        await repository.SetRatingInfoAsync(AppRatingState.Deferred, deferredAtCount: launchCount);
    }

    [RelayCommand]
    private async Task DismissRatingAsync()
    {
        IsRatingPromptVisible = false;
        await repository.SetRatingInfoAsync(AppRatingState.Done, deferredAtCount: 0);
    }

    internal async Task CheckRatingPromptAsync()
    {
        try
        {
            var launchCount = await repository.IncrementLaunchCountAsync();
            var (state, deferredAt) = await repository.GetRatingInfoAsync();
            if (AppRatingPolicy.ShouldPrompt(launchCount, state, deferredAt))
            {
                IsRatingPromptVisible = true;
            }
        }
        catch
        {
        }
    }
}
