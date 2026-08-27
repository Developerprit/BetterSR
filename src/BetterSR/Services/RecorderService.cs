using BetterSR.Models;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace BetterSR.Services;

public class RecorderService
{
    private readonly ConfigService _config;
    private readonly ScreenCaptureService _capture;
    private readonly AudioService _audio;
    private CancellationTokenSource? _cts;
    private Task? _recordTask;
    private string? _currentOutputPath;

    public RecordingState State { get; private set; } = RecordingState.Idle;
    public TimeSpan CurrentDuration { get; private set; }
    public string? CurrentOutputPath => _currentOutputPath;

    public event EventHandler<RecordingState>? StateChanged;
    public event EventHandler<TimeSpan>? DurationChanged;

    public RecorderService(ConfigService config, ScreenCaptureService capture, AudioService audio)
    {
        _config = config;
        _capture = capture;
        _audio = audio;
    }

    public void StartFullscreen() => StartRecording(RecordingArea.Fullscreen, Rectangle.Empty, IntPtr.Zero);

    public void StartRegion(Rectangle region) => StartRecording(RecordingArea.Region, region, IntPtr.Zero);

    public void StartWindow(IntPtr hWnd) => StartRecording(RecordingArea.Window, Rectangle.Empty, hWnd);

    public void Pause()
    {
        if (State == RecordingState.Recording)
        {
            State = RecordingState.Paused;
            StateChanged?.Invoke(this, State);
        }
    }

    public void Resume()
    {
        if (State == RecordingState.Paused)
        {
            State = RecordingState.Recording;
            StateChanged?.Invoke(this, State);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _audio.Stop();
        try
        {
            _recordTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Expected on cancellation.
        }
        State = RecordingState.Idle;
        CurrentDuration = TimeSpan.Zero;
        StateChanged?.Invoke(this, State);
    }

    public void Discard()
    {
        Stop();
        if (!string.IsNullOrEmpty(_currentOutputPath) && File.Exists(_currentOutputPath))
        {
            try { File.Delete(_currentOutputPath); } catch { }
        }
    }

    public bool TakeScreenshotFullscreen(string? path = null)
    {
        using var bmp = _capture.CaptureScreen();
        return SaveScreenshot(bmp, path);
    }

    public bool TakeScreenshotRegion(Rectangle region, string? path = null)
    {
        using var bmp = _capture.CaptureRegion(region);
        return SaveScreenshot(bmp, path);
    }

    public bool TakeScreenshotWindow(IntPtr hWnd, string? path = null)
    {
        using var bmp = _capture.CaptureWindow(hWnd);
        return SaveScreenshot(bmp, path);
    }

    private bool SaveScreenshot(Bitmap? bmp, string? path)
    {
        if (bmp == null) return false;
        Directory.CreateDirectory(_config.Settings.OutputDirectory);
        var filePath = path ?? Path.Combine(
            _config.Settings.OutputDirectory,
            $"BetterSR_Shot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        bmp.Save(filePath, ImageFormat.Png);
        return true;
    }

    private void StartRecording(RecordingArea area, Rectangle region, IntPtr hWnd)
    {
        if (State == RecordingState.Recording || State == RecordingState.Paused) return;

        Directory.CreateDirectory(_config.Settings.OutputDirectory);
        _currentOutputPath = Path.Combine(
            _config.Settings.OutputDirectory,
            $"BetterSR_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");

        _cts = new CancellationTokenSource();
        State = RecordingState.Recording;
        StateChanged?.Invoke(this, State);

        _recordTask = Task.Run(() => RecordLoop(area, region, hWnd, _cts.Token));
    }

    private async Task RecordLoop(RecordingArea area, Rectangle region, IntPtr hWnd, CancellationToken token)
    {
        var settings = _config.Settings;
        var bounds = area switch
        {
            RecordingArea.Fullscreen => System.Windows.Forms.Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080),
            RecordingArea.Region => region,
            _ => _capture.GetWindowBounds(hWnd)
        };

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            bounds = new Rectangle(0, 0, 1920, 1080);
        }

        var videoPipeName = $"BetterSR_V_{Guid.NewGuid():N}";
        var audioPipeName = $"BetterSR_A_{Guid.NewGuid():N}";
        var hasAudio = settings.RecordSystemAudio || settings.RecordMicrophone;

        using var videoPipe = new NamedPipeServerStream(videoPipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte);
        using var audioPipe = new NamedPipeServerStream(audioPipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte);

        var args = $"-f rawvideo -pix_fmt bgr24 -s {bounds.Width}x{bounds.Height} -r {settings.FrameRate} -thread_queue_size 512 -i \\\".\\pipe\\{videoPipeName}\"";
        if (hasAudio)
        {
            args += $" -f s16le -ar 48000 -ac 2 -thread_queue_size 512 -i \\\".\\pipe\\{audioPipeName}\"";
        }
        args += $" -c:v libx264 -preset fast -crf 23 -pix_fmt yuv420p";
        if (hasAudio)
        {
            args += " -c:a aac -b:a 128k";
        }
        args += $" -y \"{_currentOutputPath}\"";

        var ffmpeg = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = settings.FFmpegExePath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };

        ffmpeg.Start();
        _ = Task.Run(() => ffmpeg.StandardError.ReadToEnd());

        var connectionTasks = new List<Task> { videoPipe.WaitForConnectionAsync(token) };
        if (hasAudio) connectionTasks.Add(audioPipe.WaitForConnectionAsync(token));
        await Task.WhenAll(connectionTasks);

        _audio.Start(settings);

        var frameInterval = TimeSpan.FromSeconds(1.0 / settings.FrameRate);
        var startTime = DateTime.Now;
        var lastFrameTime = startTime;

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (State == RecordingState.Paused)
                {
                    _audio.ReadMixedS16(10);
                    await Task.Delay(10, token);
                    continue;
                }

                var now = DateTime.Now;
                CurrentDuration = now - startTime;
                DurationChanged?.Invoke(this, CurrentDuration);

                Bitmap? frame = area switch
                {
                    RecordingArea.Fullscreen => _capture.CaptureScreen(),
                    RecordingArea.Region => _capture.CaptureRegion(region),
                    _ => _capture.CaptureWindow(hWnd)
                };

                if (frame != null)
                {
                    var bytes = BitmapToBgr24(frame);
                    frame.Dispose();
                    await videoPipe.WriteAsync(bytes, 0, bytes.Length, token);
                }

                if (hasAudio)
                {
                    var audioBytes = _audio.ReadMixedS16(10);
                    if (audioBytes.Length > 0)
                    {
                        await audioPipe.WriteAsync(audioBytes, 0, audioBytes.Length, token);
                    }
                }

                var nextFrameTime = lastFrameTime + frameInterval;
                var delay = nextFrameTime - DateTime.Now;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, token);
                }
                lastFrameTime = nextFrameTime;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on stop.
        }
        finally
        {
            _audio.Stop();
            try { videoPipe.WaitForPipeDrain(); } catch { }
            try { if (hasAudio) audioPipe.WaitForPipeDrain(); } catch { }
            try { ffmpeg.WaitForExit(TimeSpan.FromSeconds(5)); } catch { }
            try { if (!ffmpeg.HasExited) ffmpeg.Kill(); } catch { }
            ffmpeg.Dispose();
        }
    }

    private static byte[] BitmapToBgr24(Bitmap bitmap)
    {
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            var rowBytes = bitmap.Width * 3;
            var srcRow = new byte[data.Stride];
            var result = new byte[rowBytes * bitmap.Height];
            for (int y = 0; y < bitmap.Height; y++)
            {
                Marshal.Copy(data.Scan0 + y * data.Stride, srcRow, 0, data.Stride);
                Buffer.BlockCopy(srcRow, 0, result, y * rowBytes, rowBytes);
            }
            return result;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}

internal enum RecordingArea
{
    Fullscreen,
    Region,
    Window
}
