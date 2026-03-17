using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Contracts.Services;
using VRCFaceTracking.Core.Models;
using VRCFaceTracking.Core.Services;

namespace VRCFaceTracking.Linux.ViewModels;

public partial class RegistryEntry : ObservableObject
{
    public InstallableTrackingModule Module { get; }
    [ObservableProperty] private bool _isInstalled;
    [ObservableProperty] private bool _isInstalling;

    public RegistryEntry(InstallableTrackingModule module, bool installed)
    {
        Module = module;
        _isInstalled = installed;
    }
}

public partial class RegistryViewModel : ObservableObject
{
    private readonly IModuleDataService _moduleDataService;
    private readonly ModuleInstaller _moduleInstaller;
    private readonly ILogger<RegistryViewModel> _logger;

    public ObservableCollection<RegistryEntry> Entries { get; } = new();

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string _statusMessage = "Loading registry...";

    public RegistryViewModel(
        IModuleDataService moduleDataService,
        ModuleInstaller moduleInstaller,
        ILogger<RegistryViewModel> logger)
    {
        _moduleDataService = moduleDataService;
        _moduleInstaller = moduleInstaller;
        _logger = logger;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "Fetching module registry...";
        try
        {
            var remote = (await _moduleDataService.GetRemoteModules()).ToList();
            var installed = _moduleDataService.GetInstalledModules()
                .Select(m => m.ModuleId).ToHashSet();

            Entries.Clear();
            foreach (var m in remote)
            {
                var entry = new RegistryEntry(
                    new InstallableTrackingModule
                    {
                        ModuleId = m.ModuleId,
                        ModuleName = m.ModuleName,
                        ModuleDescription = m.ModuleDescription,
                        AuthorName = m.AuthorName,
                        ModulePageUrl = m.ModulePageUrl,
                        DownloadUrl = m.DownloadUrl,
                        DllFileName = m.DllFileName,
                        InstallationState = installed.Contains(m.ModuleId)
                            ? InstallState.Installed : InstallState.NotInstalled
                    },
                    installed.Contains(m.ModuleId));
                Entries.Add(entry);
            }
            StatusMessage = $"{Entries.Count} modules available";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load registry: {ex.Message}";
            _logger.LogError(ex, "Failed to load module registry");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task InstallModule(RegistryEntry entry)
    {
        if (entry.IsInstalling) return;
        entry.IsInstalling = true;
        StatusMessage = $"Installing {entry.Module.ModuleName}...";
        try
        {
            var result = await _moduleInstaller.InstallRemoteModule(entry.Module);
            if (result != null)
            {
                entry.IsInstalled = true;
                entry.Module.InstallationState = InstallState.Installed;
                StatusMessage = $"Installed {entry.Module.ModuleName}. Restart to load it.";
            }
            else
            {
                StatusMessage = $"Failed to install {entry.Module.ModuleName}.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError(ex, "Failed to install module {name}", entry.Module.ModuleName);
        }
        finally
        {
            entry.IsInstalling = false;
        }
    }

    [RelayCommand]
    public void UninstallModule(RegistryEntry entry)
    {
        _moduleInstaller.UninstallModule(entry.Module);
        entry.IsInstalled = false;
        entry.Module.InstallationState = InstallState.NotInstalled;
        StatusMessage = $"Uninstalled {entry.Module.ModuleName}. Restart to apply.";
    }
}
