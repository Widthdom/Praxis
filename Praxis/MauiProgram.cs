using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Praxis.Controls;
using Praxis.Services;
using Praxis.ViewModels;

namespace Praxis;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
#if MACCATALYST
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(Entry), typeof(MacEntryHandler));
            handlers.AddHandler(typeof(TabNavigatingEditor), typeof(MacEditorHandler));
            handlers.AddHandler(typeof(CommandEntry), typeof(CommandEntryHandler));
        });
#endif

        builder.Services.AddSingleton<IAppRepository, SqliteAppRepository>();
        builder.Services.AddSingleton<ICommandExecutor, CommandExecutor>();
        builder.Services.AddSingleton<IClipboardService, MauiClipboardService>();
        builder.Services.AddSingleton<IThemeService, MauiThemeService>();
        builder.Services.AddSingleton<IStateSyncNotifier, FileStateSyncNotifier>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
