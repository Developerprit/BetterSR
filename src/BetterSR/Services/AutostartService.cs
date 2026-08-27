using Microsoft.Win32;

namespace BetterSR.Services;

public class AutostartService
{
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "BetterSR";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
        return key?.GetValue(AppName) != null;
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
        if (key == null) return;

        if (enabled)
        {
            var exePath = Environment.ProcessPath
                ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                ?? string.Empty;
            if (!string.IsNullOrEmpty(exePath))
            {
                key.SetValue(AppName, $"\"{exePath}\"");
            }
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }
}
