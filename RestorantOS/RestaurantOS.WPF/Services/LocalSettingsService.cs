using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RestaurantOS.WPF.Services;

public class LocalSettingsService
{
    private readonly string _path;
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("RestaurantOS_V1");

    public LocalSettingsService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RestaurantOS");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "user.settings");
    }

    public void SaveLastUsername(string username)
    {
        var bytes = Encoding.UTF8.GetBytes(username);
        var encrypted = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_path, encrypted);
    }

    public string? GetLastUsername()
    {
        if (!File.Exists(_path)) return null;
        try
        {
            var encrypted = File.ReadAllBytes(_path);
            var decrypted = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return null;
        }
    }
}
