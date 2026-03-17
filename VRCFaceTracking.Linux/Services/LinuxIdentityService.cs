using System.Security.Cryptography;
using System.Text;
using VRCFaceTracking.Core.Contracts.Services;

namespace VRCFaceTracking.Linux.Services;

/// <summary>
/// Linux implementation of IIdentityService.
/// Uses /etc/machine-id (or a generated fallback) to produce a stable anonymous user ID.
/// </summary>
public class LinuxIdentityService : IIdentityService
{
    private string? _uniqueUserId;

    public string GetUniqueUserId()
    {
        if (_uniqueUserId != null)
            return _uniqueUserId;

        string rawId;
        const string machineIdPath = "/etc/machine-id";

        if (File.Exists(machineIdPath))
        {
            rawId = File.ReadAllText(machineIdPath).Trim();
        }
        else
        {
            // Fallback: persist a generated ID in the app data directory
            var fallbackPath = Path.Combine(VRCFaceTracking.Core.Utils.PersistentDataDirectory, ".identity");
            if (File.Exists(fallbackPath))
            {
                rawId = File.ReadAllText(fallbackPath).Trim();
            }
            else
            {
                rawId = Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(VRCFaceTracking.Core.Utils.PersistentDataDirectory);
                File.WriteAllText(fallbackPath, rawId);
            }
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawId));
        _uniqueUserId = Convert.ToHexString(hash).ToLowerInvariant();
        return _uniqueUserId;
    }
}
