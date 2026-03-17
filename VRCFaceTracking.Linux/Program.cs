using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core;
using VRCFaceTracking.Core.Contracts.Services;
using VRCFaceTracking.Core.Library;
using VRCFaceTracking.Core.mDNS;
using VRCFaceTracking.Core.Models;
using VRCFaceTracking.Core.OSC.Query.mDNS;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Services;
using VRCFaceTracking.Linux.Models;
using VRCFaceTracking.Linux.Services;

// Wipe reset file if present
var resetFile = Path.Combine(Utils.PersistentDataDirectory, "reset");
if (File.Exists(resetFile))
{
    foreach (var f in Directory.EnumerateFiles(Utils.PersistentDataDirectory, "*", SearchOption.AllDirectories))
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
    })
    .Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("VRCFaceTracking");

// Kill any lingering instances
Utils.KillAllProcessesOfName("VRCFaceTracking");
Utils.KillAllProcessesOfName("VRCFaceTracking.ModuleProcess");

var mainService = host.Services.GetRequiredService<IMainService>();
await mainService.InitializeAsync();

logger.LogInformation("VRCFaceTracking {Version} running on Linux",
    typeof(MainStandalone).Assembly.GetName().Version);
logger.LogInformation("Persistent data: {Dir}", Utils.PersistentDataDirectory);

// Graceful shutdown on Ctrl+C / SIGTERM
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};
AppDomain.CurrentDomain.ProcessExit += (_, _) => cts.Cancel();

try
{
    await host.StartAsync(cts.Token);
    logger.LogInformation("Tracking active. Press Ctrl+C to stop.");
    await host.WaitForShutdownAsync(cts.Token);
}
catch (OperationCanceledException) { }
finally
{
    await mainService.Teardown();
    logger.LogInformation("VRCFaceTracking stopped.");
}
