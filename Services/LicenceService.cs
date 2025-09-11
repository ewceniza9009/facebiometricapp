using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public class LicenseService
{
    private readonly string _licenseFilePath;

    public LicenseService()
    {
        _licenseFilePath = Path.Combine(FileSystem.AppDataDirectory, "license.json");
    }

    public async Task<string> GetLicenseKeyAsync()
    {
        if (!File.Exists(_licenseFilePath))
        {
            return null;
        }

        try
        {
            string json = await File.ReadAllTextAsync(_licenseFilePath);

            return JsonSerializer.Deserialize<string>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetLicenseKeyAsync(string key)
    {
        try
        {
            string json = JsonSerializer.Serialize(key);

            await File.WriteAllTextAsync(_licenseFilePath, json);
        }
        catch
        {
        }
    }
}
