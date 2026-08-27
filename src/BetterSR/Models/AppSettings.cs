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

    /// <summary>
    /// 用户手动指定的 ffmpeg.exe 完整路径，优先级最高。
    /// 为空时回退到自动下载目录。
    /// </summary>
    public string? CustomFFmpegPath { get; set; }

    [JsonIgnore]
    public string ConfigDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BetterSR");

    [JsonIgnore]
    public string ConfigFilePath => Path.Combine(ConfigDirectory, "settings.json");

    [JsonIgnore]
    public string FFmpegDirectory => Path.Combine(ConfigDirectory, "ffmpeg");

    /// <summary>
    /// 自动下载后 ffmpeg.exe 的默认落点。
    /// 若 CustomFFmpegPath 已指定且存在，则直接返回它（手动优先）。
    /// </summary>
    [JsonIgnore]
    public string FFmpegExePath
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(CustomFFmpegPath) && File.Exists(CustomFFmpegPath))
                return CustomFFmpegPath;
            return Path.Combine(FFmpegDirectory, "bin", "ffmpeg.exe");
        }
    }

    /// <summary>
    /// 自动下载的候选源（按顺序尝试）：
    ///   Item1 = 下载 URL
    ///   Item2 = 是否为“单一可执行文件”（true 直接保存为 ffmpeg.exe；false 为需解压的 zip）
    /// jsDelivr 的 ffmpeg-static 体积最小（约 33MB）且国内 CDN 加速，列为首选。
    /// </summary>
    [JsonIgnore]
    public (string Url, bool IsRawExe)[] FFmpegDownloadCandidates =>
    [
        ("https://cdn.jsdelivr.net/npm/ffmpeg-static@5.1.0/ffmpeg-win32-x64", true),
        ("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip", false),
        ("https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip", false),
    ];
}
