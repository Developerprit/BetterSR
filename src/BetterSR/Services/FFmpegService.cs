using BetterSR.Models;
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

    public bool IsAvailable => File.Exists(_settings.FFmpegExePath);

    public async Task<bool> EnsureAvailableAsync(IProgress<double>? progress = null)
    {
        if (IsAvailable) return true;

        try
        {
            Directory.CreateDirectory(_settings.FFmpegDirectory);
            var zipPath = Path.Combine(_settings.FFmpegDirectory, "ffmpeg.zip");

            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            await using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
            var response = await client.GetAsync(_settings.FFmpegDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
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
                if (total > 0)
                {
                    progress?.Report((double)readTotal / total);
                }
            }
            await fs.DisposeAsync();

            ZipFile.ExtractToDirectory(zipPath, _settings.FFmpegDirectory);
            File.Delete(zipPath);

            // The zip contains a top-level folder like "ffmpeg-7.x.x-essentials_build".
            // Move its "bin" folder directly under our FFmpegDirectory.
            var extractedDir = Directory.GetDirectories(_settings.FFmpegDirectory).FirstOrDefault();
            if (extractedDir != null)
            {
                var innerBin = Path.Combine(extractedDir, "bin");
                var targetBin = Path.Combine(_settings.FFmpegDirectory, "bin");
                if (Directory.Exists(innerBin) && !Directory.Exists(targetBin))
                {
                    Directory.Move(innerBin, targetBin);
                    Directory.Delete(extractedDir, true);
                }
            }

            return IsAvailable;
        }
        catch
        {
            return false;
        }
    }
}
