using InputAccelerator.Core.Services;
using InputAccelerator.Helpers;
using InputAccelerator.Services;
using InputAccelerator.ViewModels;
using InputAccelerator.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace InputAccelerator;

public partial class App : Application
{
    private IHost? _host;
    private SingleInstanceService? _singleInstance;

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _singleInstance = new SingleInstanceService();
        if (!_singleInstance.TryAcquire())
        {
            SingleInstanceService.SignalExistingInstance();
            _singleInstance.Dispose();
            Exit();
            return;
        }

        try
        {
            AppPaths.EnsureDataDirectory();

            var builder = Host.CreateApplicationBuilder();
            builder.Logging.ClearProviders();
            builder.Logging.SetMinimumLevel(LogLevel.Information);
            builder.Logging.AddProvider(new FileLoggerProvider(AppPaths.LogFile));

            builder.Services.AddSingleton(_singleInstance);
            builder.Services.AddSingleton(new EntryRepository(AppPaths.EntriesFile));
            builder.Services.AddSingleton(new SettingsStore(AppPaths.SettingsFile));
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddSingleton<AppearanceService>();
            builder.Services.AddSingleton<EntryListViewModel>();
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>();
            builder.Services.AddSingleton<FloatingBarViewModel>();
            builder.Services.AddSingleton<ForegroundWindowTracker>();
            builder.Services.AddSingleton<InputInjectionService>();
            builder.Services.AddSingleton<TrayIconService>();
            builder.Services.AddSingleton<SettingsWindow>();
            builder.Services.AddSingleton<MainWindow>();
            builder.Services.AddSingleton<FloatingWindow>();
            builder.Services.AddSingleton<FloatingWindowPlacementManager>();
            builder.Services.AddSingleton<AppCoordinator>();

            _host = builder.Build();
            await _host.StartAsync();

            var coordinator = _host.Services.GetRequiredService<AppCoordinator>();
            await coordinator.InitializeAsync();
        }
        catch (Exception ex)
        {
            LogFatal(ex);
            _singleInstance?.Dispose();
            _singleInstance = null;
            Exit();
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LogFatal(e.Exception);
    }

    private static void LogFatal(Exception exception)
    {
        try
        {
            AppPaths.EnsureDataDirectory();
            var message = string.Concat(
                DateTimeOffset.Now.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                " [Fatal] ",
                exception);
            File.AppendAllText(AppPaths.LogFile, message + Environment.NewLine + Environment.NewLine);
        }
        catch (Exception)
        {
        }
    }
}
