using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Praxis.Avalonia.Services;
using Praxis.Avalonia.ViewModels;
using Praxis.Avalonia.Views;
using Praxis.Core.Models;
using Praxis.Data.Repositories;

namespace Praxis.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        MacDockIconService.ApplyIfNeeded();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = CreateMainWindowViewModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
            Dispatcher.UIThread.Post(async () => await viewModel.InitializeAsync());
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainWindowViewModel CreateMainWindowViewModel()
    {
        var syncNotifier = new FileStateSyncNotifier();
        var model = new MainModel(
            new DesktopLauncherExecutionService(),
            new SqliteLauncherButtonRepository(),
            syncNotifier);
        return new MainWindowViewModel(model, syncNotifier);
    }
}
