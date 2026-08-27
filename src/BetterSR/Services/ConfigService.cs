using BetterSR.Models;
using Newtonsoft.Json;
using System.IO;

namespace BetterSR.Services;

public class ConfigService
{
    private readonly AppSettings _settings;

    public ConfigService()
    {
        _settings = Load();
    }

    public AppSettings Settings => _settings;

    public AppSettings Load()
    {
        var settings = new AppSettings();
        try
        {
            if (File.Exists(settings.ConfigFilePath))
            {
                var json = File.ReadAllText(settings.ConfigFilePath);
                var loaded = JsonConvert.DeserializeObject<AppSettings>(json);
                if (loaded != null)
                {
                    settings = loaded;
                }
            }
        }
        catch
        {
            // Use defaults on any error.
        }
        return settings;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_settings.ConfigDirectory);
            var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            File.WriteAllText(_settings.ConfigFilePath, json);
        }
        catch
        {
            // Ignore save errors silently.
        }
    }
}
