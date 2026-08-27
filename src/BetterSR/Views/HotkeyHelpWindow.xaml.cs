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
            new("区域录制", "Ctrl + Alt + R"),
            new("窗口录制", "Ctrl + Alt + W"),
            new("暂停 / 继续", "Ctrl + Alt + F10"),
            new("停止并保存", "Ctrl + Alt + End"),
            new("丢弃录制", "Ctrl + Alt + Esc"),
            new("添加章节标记", "Ctrl + Alt + K"),
            new("全屏截图", "Ctrl + Alt + S"),
            new("区域截图", "Ctrl + Alt + Shift + S"),
            new("窗口截图（选择）", "Ctrl + Alt + Shift + W"),
            new("活动窗口截图", "Ctrl + Alt + Shift + A"),
            new("截图到剪贴板", "Ctrl + Alt + Shift + C"),
            new("打开输出文件夹", "Ctrl + Alt + O"),
            new("打开上次录制", "Ctrl + Alt + Shift + O"),
            new("复制上次录制路径", "Ctrl + Alt + C"),
            new("切换麦克风", "Ctrl + Alt + M"),
            new("切换系统音频", "Ctrl + Alt + N"),
            new("显示主界面", "Ctrl + Alt + B"),
            new("切换主题（浅/深）", "Ctrl + T"),
            new("打开设置", "Ctrl + ,"),
        };
    }

    private record HotkeyItem(string Name, string KeyText);
}
