using System.Collections.Generic;
using System.Windows;

namespace BetterSR.Views;

public partial class HotkeyHelpWindow : Window
{
    public HotkeyHelpWindow()
    {
        InitializeComponent();
        HotkeyGrid.ItemsSource = new List<HotkeyItem>
        {
            new("全屏录制", "Ctrl + Alt + F9"),
            new("暂停/继续", "Ctrl + Alt + F10"),
            new("区域录制", "Ctrl + Alt + R"),
            new("窗口录制", "Ctrl + Alt + W"),
            new("全屏截图", "Ctrl + Alt + S"),
            new("区域截图", "Ctrl + Alt + Shift + S"),
            new("显示主界面", "Ctrl + Alt + B"),
            new("切换麦克风", "Ctrl + Alt + M"),
            new("切换系统音频", "Ctrl + Alt + N"),
            new("停止并保存", "Ctrl + Alt + End"),
            new("丢弃录制", "Ctrl + Alt + Esc"),
            new("切换主题", "Ctrl + T"),
            new("打开设置", "Ctrl + ,"),
        };
    }

    private record HotkeyItem(string Name, string KeyText);
}
