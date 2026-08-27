using BetterSR.Models;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace BetterSR.Services;

public class FFmpegService
{
    private readonly AppSettings _settings;

    public FFmpegService(AppSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// 是否已有可用的 ffmpeg.exe。检测顺序：
    ///   1) 用户手动指定的路径（CustomFFmpegPath）
    ///   2) 自动下载目录 %AppData%\BetterSR\ffmpeg\bin\ffmpeg.exe
    ///   3) BetterSR.exe 同级目录的 ffmpeg.exe（便于“放旁边即用”）
    ///   4) 系统 PATH 中已有的 ffmpeg.exe
    /// 任一命中即视为就绪，无需联网下载。
    /// </summary>
    public bool IsAvailable => ResolveExisting() != null;

    /// <summary>在系统 PATH 中查找 ffmpeg.exe，找到则返回完整路径，否则 null。</summary>
    public static string? FindSystemFFmpeg()
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir, "ffmpeg.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch { /* 跳过无权限目录 */ }
        }
        return null;
    }

    /// <summary>
    /// 确保 ffmpeg 可用：优先复用已有（PATH / 同级目录 / 手动指定），否则依次尝试下载候选源。
    /// 每个源独立 3 分钟超时，失败立即切换下一个，避免单次卡死。全部失败返回 false。
    /// </summary>
    public async Task<bool> EnsureAvailableAsync(IProgress<double>? progress = null)
    {
        if (ResolveExisting() != null) return true;

        Directory.CreateDirectory(_settings.FFmpegDirectory);
        var candidates = _settings.FFmpegDownloadCandidates;
        for (int i = 0; i < candidates.Length; i++)
        {
            var (url, isRaw) = candidates[i];
            try
            {
                progress?.Report(0);
                var ok = isRaw
                    ? await DownloadRawAsync(url, progress)
                    : await DownloadAndExtractAsync(url, progress);
                if (ok && IsAvailable) return true;
            }
            catch
            {
                // 该源失败，尝试下一个
            }
        }
        return IsAvailable;
    }

    private string? ResolveExisting()
    {
        if (!string.IsNullOrWhiteSpace(_settings.CustomFFmpegPath) && File.Exists(_settings.CustomFFmpegPath))
            return _settings.CustomFFmpegPath;

        if (File.Exists(_settings.FFmpegExePath))
            return _settings.FFmpegExePath;

        var baseDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        var sideBySide = Path.Combine(baseDir, "ffmpeg.exe");
        if (File.Exists(sideBySide)) return sideBySide;

        return FindSystemFFmpeg();
    }

    private async Task<bool> DownloadRawAsync(string url, IProgress<double>? progress)
    {
        var target = Path.Combine(_settings.FFmpegDirectory, "bin", "ffmpeg.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        client.DefaultRequestHeaders.Add("User-Agent", "BetterSR");
        await using var fs = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None);
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? -1L;
        await using var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[8192];
        long readTotal = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, read));
            readTotal += read;
            if (total > 0) progress?.Report((double)readTotal / total * 0.99);
        }
        await fs.DisposeAsync();
        // 校验：文件存在且体积合理（ffmpeg.exe 必然 > 1MB）
        return File.Exists(target) && new FileInfo(target).Length > 1_000_000;
    }

    private async Task<bool> DownloadAndExtractAsync(string url, IProgress<double>? progress)
    {
        var zipPath = Path.Combine(_settings.FFmpegDirectory, "ffmpeg.zip");
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        client.DefaultRequestHeaders.Add("User-Agent", "BetterSR");
        await using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? -1L;
        await using var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[8192];
        long readTotal = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, read));
            readTotal += read;
            if (total > 0) progress?.Report((double)readTotal / total * 0.99);
        }
        await fs.DisposeAsync();

        var extractDir = Path.Combine(_settings.FFmpegDirectory, "extract_" + Guid.NewGuid().ToString("N"));
        ZipFile.ExtractToDirectory(zipPath, extractDir);
        File.Delete(zipPath);

        var found = FindExeUnder(extractDir);
        if (found == null) return false;

        var targetBin = Path.Combine(_settings.FFmpegDirectory, "bin");
        Directory.CreateDirectory(targetBin);
        var target = Path.Combine(targetBin, "ffmpeg.exe");
        if (File.Exists(target)) File.Delete(target);
        File.Move(found, target);

        try { Directory.Delete(extractDir, true); } catch { }
        return File.Exists(target);
    }

    private static string? FindExeUnder(string root)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(root, "ffmpeg.exe", SearchOption.AllDirectories))
                return file;
        }
        catch { }
        return null;
    }
}
