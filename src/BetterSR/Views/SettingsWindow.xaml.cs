using BetterSR.Models;
using BetterSR.Services;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _config.Settings.OutputDirectory = OutputDirBox.Text;
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
