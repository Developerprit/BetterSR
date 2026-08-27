using Newtonsoft.Json;
using System.IO;

namespace BetterSR.Models;

public class AppSettings
{
    public string OutputDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "BetterSR");

    public int FrameRate { get; set; } = 60;
    public int VideoBitrateKbps { get; set; } = 8000;
    public bool RecordSystemAudio { get; set; } = true;
    public bool RecordMicrophone { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public string Theme { get; set; } = "Light";
    public string FFmpegDownloadUrl { get; set; } = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";

    [JsonIgnore]
    public string ConfigDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BetterSR");

    [JsonIgnore]
    public string ConfigFilePath => Path.Combine(ConfigDirectory, "settings.json");

    [JsonIgnore]
    public string FFmpegDirectory => Path.Combine(ConfigDirectory, "ffmpeg");

    [JsonIgnore]
    public string FFmpegExePath => Path.Combine(FFmpegDirectory, "bin", "ffmpeg.exe");
}
