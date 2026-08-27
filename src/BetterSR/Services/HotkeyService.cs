using BetterSR.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace BetterSR.Services;

public class HotkeyService
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const int WM_HOTKEY = 0x0312;

    private readonly Dictionary<int, HotkeyDefinition> _hotkeys = new();
    private HwndSource? _source;
    private int _nextId = 9000;

    public event EventHandler<string>? HotkeyPressed;

    public void RegisterWindow(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _source = HwndSource.FromHwnd(helper.EnsureHandle());
        _source.AddHook(WndProc);
    }

    public void UnregisterAll()
    {
        if (_source != null)
        {
            foreach (var id in _hotkeys.Keys)
            {
                UnregisterHotKey(_source.Handle, id);
            }
        }
        _hotkeys.Clear();
    }

    public bool Register(HotkeyDefinition hotkey)
    {
        if (_source == null) return false;

        var vk = (uint)KeyInterop.VirtualKeyFromKey(hotkey.Key);
        var mods = ModifierToWin32(hotkey.Modifiers);

        var id = _nextId++;
        if (RegisterHotKey(_source.Handle, id, mods, vk))
        {
            _hotkeys[id] = hotkey;
            return true;
        }
        return false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && _hotkeys.TryGetValue(wParam.ToInt32(), out var hotkey))
        {
            HotkeyPressed?.Invoke(this, hotkey.Id);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private static uint ModifierToWin32(ModifierKeys modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(ModifierKeys.Control)) result |= MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Alt)) result |= MOD_ALT;
        if (modifiers.HasFlag(ModifierKeys.Shift)) result |= MOD_SHIFT;
        if (modifiers.HasFlag(ModifierKeys.Windows)) result |= MOD_WIN;
        return result;
    }
}
