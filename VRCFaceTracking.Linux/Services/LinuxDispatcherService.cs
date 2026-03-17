using VRCFaceTracking.Core.Contracts.Services;

namespace VRCFaceTracking.Linux.Services;

/// <summary>
/// Linux implementation of IDispatcherService.
/// On Linux there is no UI thread, so actions are executed inline on the calling thread.
/// </summary>
public class LinuxDispatcherService : IDispatcherService
{
    public void Run(Action action) => action();
}
