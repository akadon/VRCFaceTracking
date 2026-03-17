using System.Collections.ObjectModel;
using Timer = System.Timers.Timer;
using CommunityToolkit.Mvvm.ComponentModel;
using VRCFaceTracking.Core.Contracts;
using VRCFaceTracking.Core.Contracts.Services;
using VRCFaceTracking.Core.Library;
using VRCFaceTracking.Core.Models;
using VRCFaceTracking.Core.OSC;
using VRCFaceTracking.Core.Services;

namespace VRCFaceTracking.Linux.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    public ILibManager LibManager { get; }
    public IOscTarget OscTarget { get; }
    public ObservableCollection<string> LogLines { get; } = new();

    [ObservableProperty] private int _messagesInPerSec;
    [ObservableProperty] private int _messagesOutPerSec;
    [ObservableProperty] private string _statusText = "Starting...";
    [ObservableProperty] private bool _noModulesInstalled;
    public string ModulesDir => VRCFaceTracking.Core.Utils.CustomLibsDirectory;

    private int _messagesRecvd;
    private int _messagesSent;
    private readonly Timer _statsTimer;
    private readonly OscRecvService _oscRecvService;
    private readonly OscSendService _oscSendService;
    private const int MaxLogLines = 500;

    public MainWindowViewModel(
        ILibManager libManager,
        IOscTarget oscTarget,
        OscRecvService oscRecvService,
        OscSendService oscSendService,
        IModuleDataService moduleDataService)
    {
        LibManager = libManager;
        OscTarget = oscTarget;
        _oscRecvService = oscRecvService;
        _oscSendService = oscSendService;

        var installed = moduleDataService.GetInstalledModules().Any()
                        || moduleDataService.GetLegacyModules().Any();
        NoModulesInstalled = !installed;
        StatusText = installed ? "Running" : "No modules installed";

        oscRecvService.OnMessageReceived += _ => Interlocked.Increment(ref _messagesRecvd);
        oscSendService.OnMessagesDispatched += n => Interlocked.Add(ref _messagesSent, n);

        _statsTimer = new Timer(1000);
        _statsTimer.Elapsed += (_, _) =>
        {
            MessagesInPerSec = Interlocked.Exchange(ref _messagesRecvd, 0);
            MessagesOutPerSec = Interlocked.Exchange(ref _messagesSent, 0);
        };
        _statsTimer.Start();
    }

    public void AppendLog(string line)
    {
        if (LogLines.Count >= MaxLogLines)
            LogLines.RemoveAt(0);
        LogLines.Add(line);
    }

    public void Dispose()
    {
        _statsTimer.Dispose();
        _oscRecvService.OnMessageReceived -= _ => { };
        _oscSendService.OnMessagesDispatched -= _ => { };
    }
}
