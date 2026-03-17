using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using VRCFaceTracking.Core.Contracts.Services;
using VRCFaceTracking.Core.Helpers;
using VRCFaceTracking.Linux.Models;

namespace VRCFaceTracking.Linux.Services;

/// <summary>
/// Linux implementation of ILocalSettingsService.
/// Stores settings as JSON in the app's persistent data directory.
/// </summary>
public class LinuxLocalSettingsService : ILocalSettingsService
{
    private const string DefaultApplicationDataFolder = "VRCFaceTracking/ApplicationData";
    private const string DefaultLocalSettingsFile = "LocalSettings.json";

    private readonly IFileService _fileService;
    private readonly string _applicationDataFolder;
    private readonly string _localSettingsFile;

    private IDictionary<string, object> _settings = new Dictionary<string, object>();
    private bool _isInitialized;

    public LinuxLocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        var opts = options.Value;
        _applicationDataFolder = Path.Combine(
            Core.Utils.PersistentDataDirectory,
            opts.ApplicationDataFolder ?? DefaultApplicationDataFolder);
        _localSettingsFile = opts.LocalSettingsFile ?? DefaultLocalSettingsFile;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;
        _settings = await Task.Run(() =>
            _fileService.Read<IDictionary<string, object>>(_applicationDataFolder, _localSettingsFile))
            ?? new Dictionary<string, object>();
        _isInitialized = true;
    }

    public async Task<T?> ReadSettingAsync<T>(string key, T? defaultValue = default, bool forceLocal = false)
    {
        await EnsureInitializedAsync();
        if (_settings.TryGetValue(key, out var obj))
            return await Json.ToObjectAsync<T>((string)obj);
        return defaultValue;
    }

    public async Task SaveSettingAsync<T>(string key, T value, bool forceLocal = false)
    {
        await EnsureInitializedAsync();
        _settings[key] = await Json.StringifyAsync(value);
        await _fileService.Save(_applicationDataFolder, _localSettingsFile, _settings);
    }

    public async Task Load(object instance)
    {
        foreach (var property in instance.GetType().GetProperties())
        {
            var attrs = property.GetCustomAttributes(typeof(SavedSettingAttribute), false);
            if (attrs.Length == 0) continue;

            var attr = (SavedSettingAttribute)attrs[0];
            var setting = await ReadSettingAsync(attr.GetName(), attr.Default(), attr.ForceLocal());
            try
            {
                property.SetValue(instance, Convert.ChangeType(setting, property.PropertyType));
            }
            catch
            {
                property.SetValue(instance, attr.Default());
            }
        }
    }

    public async Task Save(object instance)
    {
        foreach (var property in instance.GetType().GetProperties())
        {
            var attrs = property.GetCustomAttributes(typeof(SavedSettingAttribute), false);
            if (attrs.Length == 0) continue;

            var attr = (SavedSettingAttribute)attrs[0];
            await SaveSettingAsync(attr.GetName(), property.GetValue(instance), attr.ForceLocal());
        }
    }
}
