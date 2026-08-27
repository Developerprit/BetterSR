using BetterSR.Models;
using BetterSR.Services;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BetterSR.Views;

public partial class SettingsWindow : Window
{
    private readonly ConfigService _config;
    private readonly AutostartService _autostart;

    public SettingsWindow(ConfigService config, AutostartService autostart)
    {
        InitializeComponent();
        _config = config;
        _autostart = autostart;

        OutputDirBox.Text = _config.Settings.OutputDirectory;
        SelectComboItem(FpsCombo, _config.Settings.FrameRate.ToString());
        SelectComboItem(BitrateCombo, _config.Settings.VideoBitrateKbps.ToString());
        AutostartCheck.IsChecked = _autostart.IsEnabled();
        TrayCheck.IsChecked = _config.Settings.MinimizeToTray;

        FfmpegPathBox.Text = _config.Settings.CustomFFmpegPath ?? "";
        ValidateFfmpeg();
    }

    private void ValidateFfmpeg()
    {
        var path = FfmpegPathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            FfmpegStatusText.Text = "未指定：将尝试自动下载（gyan.dev / jsDelivr 等）。";
            FfmpegStatusText.Foreground = System.Windows.Media.Brushes.Gray;
            return;
        }
        if (File.Exists(path))
        {
            FfmpegStatusText.Text = "✓ 已找到 ffmpeg.exe";
            FfmpegStatusText.Foreground = System.Windows.Media.Brushes.Green;
        }
        else
        {
            FfmpegStatusText.Text = "✗ 文件不存在，请重新选择";
            FfmpegStatusText.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private void FfmpegPathBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ValidateFfmpeg();
    }

    private static void SelectComboItem(System.Windows.Controls.ComboBox combo, string value)
    {
        foreach (System.Windows.Controls.ComboBoxItem item in combo.Items)
        {
            if (item.Content?.ToString() == value)
            {
                combo.SelectedItem = item;
                return;
            }
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog();
        dialog.SelectedPath = _config.Settings.OutputDirectory;
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            OutputDirBox.Text = dialog.SelectedPath;
        }
    }

    private void BrowseFfmpegButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
            Title = "选择 ffmpeg.exe"
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            FfmpegPathBox.Text = dialog.FileName;
            ValidateFfmpeg();
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _config.Settings.OutputDirectory = OutputDirBox.Text;
        var ffmpegPath = FfmpegPathBox.Text.Trim();
        _config.Settings.CustomFFmpegPath = string.IsNullOrWhiteSpace(ffmpegPath) ? null : ffmpegPath;
        if (FpsCombo.SelectedItem is ComboBoxItem fpsItem && int.TryParse(fpsItem.Content?.ToString(), out var fps))
        {
            _config.Settings.FrameRate = fps;
        }
        if (BitrateCombo.SelectedItem is ComboBoxItem bitrateItem && int.TryParse(bitrateItem.Content?.ToString(), out var bitrate))
        {
            _config.Settings.VideoBitrateKbps = bitrate;
        }
        _config.Settings.MinimizeToTray = TrayCheck.IsChecked == true;
        _autostart.SetEnabled(AutostartCheck.IsChecked == true);
        _config.Save();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
