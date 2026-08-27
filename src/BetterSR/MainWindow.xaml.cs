using BetterSR.Models;
using BetterSR.Services;
using BetterSR.Views;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BetterSR;

public partial class MainWindow : Window
{
    private readonly ConfigService _config;
    private readonly HotkeyService _hotkeys;
    private readonly AutostartService _autostart;
    private readonly FFmpegService _ffmpeg;
    private readonly SayingService _saying;
    private readonly ScreenCaptureService _capture;
    private readonly AudioService _audio;
    private readonly RecorderService _recorder;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private DispatcherTimer? _durationTimer;
    private RecordingState _lastRecorderState = RecordingState.Idle;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    public MainWindow()
    {
        InitializeComponent();

        _config = new ConfigService();
        _hotkeys = new HotkeyService();
        _autostart = new AutostartService();
        _ffmpeg = new FFmpegService(_config.Settings);
        _saying = new SayingService();
        _capture = new ScreenCaptureService();
        _audio = new AudioService();
        _recorder = new RecorderService(_config, _capture, _audio);

        _recorder.StateChanged += OnRecorderStateChanged;
        _recorder.DurationChanged += OnDurationChanged;

        InitializeTrayIcon();
        ApplyTheme(_config.Settings.Theme, false);
        UpdateAudioToggles();
    }

    private void InitializeTrayIcon()
    {
        var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        var icon = exePath != null ? System.Drawing.Icon.ExtractAssociatedIcon(exePath) : System.Drawing.SystemIcons.Application;

        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = icon,
            Text = "BetterSR",
            Visible = true
        };
        _trayIcon.DoubleClick += (s, e) => ShowMainWindow();

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("显示", null, (s, e) => ShowMainWindow());
        menu.Items.Add("全屏录制", null, (s, e) => Dispatcher.Invoke(StartFullscreen));
        menu.Items.Add("停止录制", null, (s, e) => Dispatcher.Invoke(() => _recorder.Stop()));
        menu.Items.Add("退出", null, (s, e) => ExitApp());
        _trayIcon.ContextMenuStrip = menu;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _hotkeys.RegisterWindow(this);
        InitializeHotkeys();
        _hotkeys.HotkeyPressed += OnHotkeyPressed;

        try
        {
            var text = await _saying.GetSayingAsync();
            SayingText.Text = $"“{text}”";
        }
        catch
        {
            SayingText.Text = "“每一次录制，都是一次创作。”";
        }

        if (!_ffmpeg.IsAvailable)
        {
            FooterText.Text = "正在准备 FFmpeg...";
            var progress = new Progress<double>(p => FooterText.Text = $"正在下载 FFmpeg... {p:P0}");
            var ok = await _ffmpeg.EnsureAvailableAsync(progress);
            FooterText.Text = ok ? "FFmpeg 准备就绪" : "FFmpeg 准备失败，请检查网络";
        }
        else
        {
            FooterText.Text = "就绪";
        }
    }

    private void InitializeHotkeys()
    {
        var hotkeys = new[]
        {
            new HotkeyDefinition { Id = "Fullscreen", Name = "全屏录制", Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.F9, IsGlobal = true },
            new HotkeyDefinition { Id = "Pause", Name = "暂停/继续", Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.F10, IsGlobal = true },
            new HotkeyDefinition { Id = "Region", Name = "区域录制", Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.R, IsGlobal = true },
            new HotkeyDefinition { Id = "Window", Name = "窗口录制", Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.W, IsGlobal = true },
            new HotkeyDefinition { Id = "Screenshot", Name = "全屏截图", Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.S, IsGlobal = true },
            new HotkeyDefinition { Id = "ScreenshotRegion", Name = "区域截图", Modifiers = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, Key = Key.S, IsGlobal = true },
            new HotkeyDefinition { Id = "ShowWindow", Name = "显示窗口", Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.B, IsGlobal = true },
            new HotkeyDefinition { Id = "ToggleMic", Name = "切换麦克风", Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.M, IsGlobal = true },
            new HotkeyDefinition { Id = "ToggleSysAudio", Name = "切换系统音频", Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.N, IsGlobal = true },
            new HotkeyDefinition { Id = "StopSave", Name = "停止并保存", Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.End, IsGlobal = true },
            new HotkeyDefinition { Id = "Discard", Name = "丢弃录制", Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.Escape, IsGlobal = true },
            new HotkeyDefinition { Id = "Marker", Name = "添加标记", Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.K, IsGlobal = true },
            new HotkeyDefinition { Id = "OpenFolder", Name = "打开输出文件夹", Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.O, IsGlobal = true },
            new HotkeyDefinition { Id = "CopyPath", Name = "复制上次路径", Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.C, IsGlobal = true },
            new HotkeyDefinition { Id = "ScreenshotWindow", Name = "窗口截图", Modifiers = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, Key = Key.W, IsGlobal = true },
            new HotkeyDefinition { Id = "ScreenshotActive", Name = "活动窗口截图", Modifiers = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, Key = Key.A, IsGlobal = true },
            new HotkeyDefinition { Id = "ScreenshotClipboard", Name = "截图到剪贴板", Modifiers = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, Key = Key.C, IsGlobal = true },
            new HotkeyDefinition { Id = "OpenLastFile", Name = "打开上次录制", Modifiers = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, Key = Key.O, IsGlobal = true },
            new HotkeyDefinition { Id = "Theme", Name = "切换主题", Modifiers = ModifierKeys.Control, Key = Key.T, IsGlobal = true },
            new HotkeyDefinition { Id = "Settings", Name = "打开设置", Modifiers = ModifierKeys.Control, Key = Key.OemComma, IsGlobal = true },
        };

        foreach (var hk in hotkeys)
        {
            _hotkeys.Register(hk);
        }
    }

    private void OnHotkeyPressed(object? sender, string id)
    {
        Dispatcher.Invoke(() =>
        {
            switch (id)
            {
                case "Fullscreen": StartFullscreen(); break;
                case "Pause": TogglePause(); break;
                case "Region": StartRegion(); break;
                case "Window": StartWindow(); break;
                case "Screenshot": TakeScreenshotFullscreen(); break;
                case "ScreenshotRegion": TakeScreenshotRegion(); break;
                case "ShowWindow": ShowMainWindow(); break;
                case "ToggleMic": ToggleMicrophone.IsChecked = !ToggleMicrophone.IsChecked; break;
                case "ToggleSysAudio": ToggleSystemAudio.IsChecked = !ToggleSystemAudio.IsChecked; break;
                case "StopSave": _recorder.Stop(); break;
                case "Discard": _recorder.Discard(); break;
                case "Marker": AddMarker(); break;
                case "OpenFolder": OpenOutputFolder(); break;
                case "CopyPath": CopyLastPath(); break;
                case "ScreenshotWindow": TakeScreenshotWindow(); break;
                case "ScreenshotActive": TakeScreenshotActiveWindow(); break;
                case "ScreenshotClipboard": CopyScreenshotToClipboard(); break;
                case "OpenLastFile": OpenLastRecording(); break;
                case "Theme": ThemeButton_Click(this, new RoutedEventArgs()); break;
                case "Settings": SettingsButton_Click(this, new RoutedEventArgs()); break;
            }
        });
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_config.Settings.MinimizeToTray && _recorder.State == RecordingState.Idle)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            ExitApp();
        }
        base.OnClosing(e);
    }

    private void ExitApp()
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        _hotkeys.UnregisterAll();
        _audio.Dispose();
        _config.Save();
        System.Windows.Application.Current.Shutdown();
    }

    private void OnRecorderStateChanged(object? sender, RecordingState state)
    {
        Dispatcher.Invoke(() =>
        {
            var isRecording = state == RecordingState.Recording || state == RecordingState.Paused;
            RecordingPanel.Visibility = isRecording ? Visibility.Visible : Visibility.Collapsed;
            BtnFullscreen.IsEnabled = !isRecording;
            BtnRegion.IsEnabled = !isRecording;
            BtnWindow.IsEnabled = !isRecording;

            if (state == RecordingState.Paused)
            {
                StatusText.Text = "已暂停";
                BtnPause.Content = "▶";
            }
            else if (state == RecordingState.Recording)
            {
                StatusText.Text = "正在录制";
                BtnPause.Content = "⏸";
                if (_durationTimer == null)
                {
                    _durationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                    _durationTimer.Tick += (s, e) => DurationText.Text = _recorder.CurrentDuration.ToString(@"hh\:mm\:ss");
                    _durationTimer.Start();
                }
            }
            else
            {
                _durationTimer?.Stop();
                _durationTimer = null;
                CurrentDuration = TimeSpan.Zero;
                DurationText.Text = "00:00:00";
                FooterText.Text = _recorder.CurrentOutputPath != null
                    ? $"已保存: {Path.GetFileName(_recorder.CurrentOutputPath)}"
                    : "就绪";
            }

            if (state == RecordingState.Recording && _lastRecorderState == RecordingState.Idle)
            {
                ShowRecordingToast();
            }
            _lastRecorderState = state;
        });
    }

    private void ShowRecordingToast() => ShowToast("BetterSR 正在录制中...");

    private void ShowToast(string message)
    {
        ToastText.Text = message;
        RecordingToast.Visibility = Visibility.Visible;
        ToastTransform.Y = -80;

        var anim = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromSeconds(1.8)
        };
        // 从上滑入(0.3s) -> 停顿 1.2s -> 向上回缩消失(0.3s)
        anim.KeyFrames.Add(new LinearDoubleKeyFrame(-80, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3)), new CubicEase { EasingMode = EasingMode.EaseOut }));
        anim.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.5))));
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(-80, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.8)), new CubicEase { EasingMode = EasingMode.EaseIn }));

        var sb = new Storyboard();
        Storyboard.SetTarget(anim, ToastTransform);
        Storyboard.SetTargetProperty(anim, new PropertyPath(TranslateTransform.YProperty));
        sb.Children.Add(anim);
        sb.Completed += (s, e) => RecordingToast.Visibility = Visibility.Collapsed;
        sb.Begin();
    }

    private void AddMarker()
    {
        if (_recorder.State != RecordingState.Recording)
        {
            ShowToast("未录制，无法添加标记");
            return;
        }
        var n = _recorder.AddMarker();
        if (n > 0) ShowToast($"标记 #{n} 已添加");
    }

    private void OpenOutputFolder()
    {
        var dir = _config.Settings.OutputDirectory;
        Directory.CreateDirectory(dir);
        try
        {
            Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
            FooterText.Text = "已打开输出文件夹";
        }
        catch
        {
            ShowToast("无法打开文件夹");
        }
    }

    private void CopyLastPath()
    {
        var path = _recorder.CurrentOutputPath;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            System.Windows.Clipboard.SetText(path);
            ShowToast("已复制上次录制路径");
        }
        else
        {
            ShowToast("还没有已保存的录制");
        }
    }

    private void OpenLastRecording()
    {
        var path = _recorder.CurrentOutputPath;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch
            {
                ShowToast("无法打开文件");
            }
        }
        else
        {
            ShowToast("还没有已保存的录制");
        }
    }

    private void TakeScreenshotWindow()
    {
        var picker = new WindowPickerWindow { Owner = this };
        if (picker.ShowDialog() == true && picker.SelectedHwnd != IntPtr.Zero)
        {
            if (_recorder.TakeScreenshotWindow(picker.SelectedHwnd))
            {
                FooterText.Text = "已保存窗口截图";
            }
        }
    }

    private void TakeScreenshotActiveWindow()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd != IntPtr.Zero && _recorder.TakeScreenshotWindow(hwnd))
        {
            FooterText.Text = "已保存活动窗口截图";
        }
    }

    private void CopyScreenshotToClipboard()
    {
        var bmp = _recorder.CaptureScreenBitmap();
        if (bmp == null)
        {
            ShowToast("截图失败");
            return;
        }
        try
        {
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            var src = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            System.Windows.Clipboard.SetImage(src);
            ShowToast("截图已复制到剪贴板");
        }
        catch
        {
            ShowToast("截图复制失败");
        }
        finally
        {
            bmp.Dispose();
        }
    }

    private TimeSpan CurrentDuration { get; set; }

    private void OnDurationChanged(object? sender, TimeSpan duration)
    {
        Dispatcher.Invoke(() =>
        {
            CurrentDuration = duration;
            DurationText.Text = duration.ToString(@"hh\:mm\:ss");
        });
    }

    private void BtnFullscreen_Click(object sender, RoutedEventArgs e) => StartFullscreen();

    private void BtnRegion_Click(object sender, RoutedEventArgs e) => StartRegion();

    private void BtnWindow_Click(object sender, RoutedEventArgs e) => StartWindow();

    private void StartFullscreen()
    {
        if (_recorder.State != RecordingState.Idle) return;
        if (!EnsureFFmpeg()) return;
        _recorder.StartFullscreen();
    }

    private void StartRegion()
    {
        if (_recorder.State != RecordingState.Idle) return;
        if (!EnsureFFmpeg()) return;

        var picker = new RegionPickerWindow { Owner = this };
        if (picker.ShowDialog() == true && picker.SelectedRegion.Width > 0)
        {
            _recorder.StartRegion(picker.SelectedRegion);
        }
    }

    private void StartWindow()
    {
        if (_recorder.State != RecordingState.Idle) return;
        if (!EnsureFFmpeg()) return;

        var picker = new WindowPickerWindow { Owner = this };
        if (picker.ShowDialog() == true && picker.SelectedHwnd != IntPtr.Zero)
        {
            _recorder.StartWindow(picker.SelectedHwnd);
        }
    }

    private void TakeScreenshotFullscreen()
    {
        if (_recorder.TakeScreenshotFullscreen())
        {
            FooterText.Text = "已保存全屏截图";
        }
    }

    private void TakeScreenshotRegion()
    {
        var picker = new RegionPickerWindow { Owner = this };
        if (picker.ShowDialog() == true && picker.SelectedRegion.Width > 0)
        {
            if (_recorder.TakeScreenshotRegion(picker.SelectedRegion))
            {
                FooterText.Text = "已保存区域截图";
            }
        }
    }

    private void TogglePause()
    {
        if (_recorder.State == RecordingState.Recording) _recorder.Pause();
        else if (_recorder.State == RecordingState.Paused) _recorder.Resume();
    }

    private void BtnPause_Click(object sender, RoutedEventArgs e) => TogglePause();

    private void BtnStop_Click(object sender, RoutedEventArgs e) => _recorder.Stop();

    private void BtnDiscard_Click(object sender, RoutedEventArgs e) => _recorder.Discard();

    private bool EnsureFFmpeg()
    {
        if (_ffmpeg.IsAvailable) return true;
        System.Windows.MessageBox.Show("FFmpeg 尚未准备就绪，请检查网络后重试。", "BetterSR", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private void UpdateAudioToggles()
    {
        ToggleSystemAudio.IsChecked = _config.Settings.RecordSystemAudio;
        ToggleMicrophone.IsChecked = _config.Settings.RecordMicrophone;
    }

    private void ToggleSystemAudio_Checked(object sender, RoutedEventArgs e)
    {
        _config.Settings.RecordSystemAudio = ToggleSystemAudio.IsChecked == true;
        _config.Save();
    }

    private void ToggleMicrophone_Checked(object sender, RoutedEventArgs e)
    {
        _config.Settings.RecordMicrophone = ToggleMicrophone.IsChecked == true;
        _config.Save();
    }

    private void ToggleSystemAudio_Unchecked(object sender, RoutedEventArgs e)
    {
        _config.Settings.RecordSystemAudio = false;
        _config.Save();
    }

    private void ToggleMicrophone_Unchecked(object sender, RoutedEventArgs e)
    {
        _config.Settings.RecordMicrophone = false;
        _config.Save();
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        var newTheme = _config.Settings.Theme == "Light" ? "Dark" : "Light";
        ApplyTheme(newTheme, true);
    }

    private void ApplyTheme(string theme, bool save)
    {
        _config.Settings.Theme = theme;
        if (save) _config.Save();

        var dict = new ResourceDictionary { Source = new Uri($"/Themes/{theme}Theme.xaml", UriKind.Relative) };
        System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
        System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);

        ThemeButton.Content = theme == "Light" ? "🌙" : "☀";
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        new HotkeyHelpWindow { Owner = this }.ShowDialog();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow(_config, _autostart) { Owner = this };
        if (window.ShowDialog() == true)
        {
            UpdateAudioToggles();
            ApplyTheme(_config.Settings.Theme, false);
        }
    }
}
