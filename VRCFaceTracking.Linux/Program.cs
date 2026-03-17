using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Contracts;
using VRCFaceTracking.Core.Contracts.Services;
using VRCFaceTracking.Core.Library;
using VRCFaceTracking.Core.mDNS;
using VRCFaceTracking.Core.Models;
using VRCFaceTracking.Core.OSC.Query.mDNS;
using VRCFaceTracking.Core;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Services;
using VRCFaceTracking.Linux.Models;
using VRCFaceTracking.Linux.Services;
using VRCFaceTracking.Linux;
using VRCFaceTracking.Linux.ViewModels;
using CoreUtils = VRCFaceTracking.Core.Utils;
using UnifiedTracking = VRCFaceTracking.UnifiedTracking;

// Tmds.DBus (Wayland backend) throws TaskCanceledException on a background
// thread during shutdown when the Avalonia dispatcher is already torn down.
// It cannot be caught with try/catch since it propagates via Task.ThrowAsync.
// Intercept it here and exit cleanly instead of letting the CLR abort.
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    if (e.ExceptionObject is TaskCanceledException or OperationCanceledException)
        Environment.Exit(0);
};

// Wipe reset file if present
var resetFile = Path.Combine(CoreUtils.PersistentDataDirectory, "reset");
if (File.Exists(resetFile))
{
    foreach (var f in Directory.EnumerateFiles(CoreUtils.PersistentDataDirectory, "*", SearchOption.AllDirectories))
        File.Delete(f);
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddSentry(o =>
        {
            o.Dsn = "https://444b0799dd2b670efa85d866c8c12134@o4506152235237376.ingest.us.sentry.io/4506152246575104";
            o.MinimumBreadcrumbLevel = LogLevel.Error;
            o.MinimumEventLevel = LogLevel.Critical;
        });
        logging.AddProvider(new LogFileProvider());
    })
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureServices((context, services) =>
    {
        // Linux-specific service implementations
        services.AddSingleton<IDispatcherService, LinuxDispatcherService>();
        services.AddSingleton<IIdentityService, LinuxIdentityService>();
        services.AddSingleton<ILocalSettingsService, LinuxLocalSettingsService>();

        // Core cross-platform services
        services.AddTransient<IFileService, FileService>();
        services.AddSingleton<IModuleDataService, ModuleDataService>();
        services.AddSingleton<ModuleInstaller>();
        services.AddSingleton<OscQueryService>();
        services.AddSingleton<MulticastDnsService>();
        services.AddSingleton<IMainService, MainStandalone>();
        services.AddTransient<AvatarConfigParser>();
        services.AddTransient<OscQueryConfigParser>();
        services.AddSingleton<UnifiedTracking>();
        services.AddSingleton<ILibManager, UnifiedLibManager>();
        services.AddSingleton<IOscTarget, OscTarget>();
        services.AddSingleton<HttpHandler>();
        services.AddSingleton<OscSendService>();
        services.AddSingleton<OscRecvService>();
        services.AddSingleton<ParameterSenderService>();
        services.AddSingleton<UnifiedTrackingMutator>();

        services.AddHostedService<ParameterSenderService>(p => p.GetRequiredService<ParameterSenderService>());
        services.AddHostedService<OscRecvService>(p => p.GetRequiredService<OscRecvService>());

        services.Configure<LocalSettingsOptions>(
            context.Configuration.GetSection(nameof(LocalSettingsOptions)));

        // UI ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<RegistryViewModel>();
        services.AddSingleton<CalibrationViewModel>();
    })
    .Build();

// Kill any lingering instances
CoreUtils.KillAllProcessesOfName("VRCFaceTracking");
CoreUtils.KillAllProcessesOfName("VRCFaceTracking.ModuleProcess");

var mainService = host.Services.GetRequiredService<IMainService>();
await mainService.InitializeAsync();

// Start the background host (OSC, parameter sender, etc.)
await host.StartAsync();

// Wire ViewModel's log listener to the log file / console
var vm = host.Services.GetRequiredService<MainWindowViewModel>();

// Launch the Avalonia UI (blocks until window is closed)
VRCFaceTracking.Linux.App.Services = host.Services;
var exitCode = AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);

// Teardown is handled in MainWindow.OnClosing while the Avalonia dispatcher
// is still alive. Call these as a fallback in case the window was never shown.
try
{
    await mainService.Teardown();
    await host.StopAsync();
}
catch (TaskCanceledException) { }
catch (OperationCanceledException) { }

return exitCode;
