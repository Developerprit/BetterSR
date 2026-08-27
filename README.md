# BetterSR — 更好的录制屏幕 / A Better Screen Recorder

![BetterSR](Assets/Background.png)

> 一款更轻、更好看、更听话的 Windows 屏幕录制工具。  
> A lighter, prettier, and more obedient screen recorder for Windows.

---

## 特性 / Features

- 🎀 **浅粉配色 + 半透明背景图** —— 温润如玉的录制界面
- 🌗 **浅色 / 深色双主题** —— 白天夜晚都舒适
- 🖥️ **全屏 / 区域 / 窗口录制** —— 想录哪里录哪里
- 🔊 **系统音频 + 麦克风** —— 独立开关，灵活混音
- ⌨️ **20+ 全局快捷键** —— 录制全程不用碰鼠标
- 🚀 **开机自启 + 托盘常驻** —— 随时待命
- 💬 **每日一言** —— 每次打开都有新句子
- 📦 **单文件 exe** —— 下载即用，无需安装

---

## 快捷键 / Hotkeys

| 功能 / Function | 快捷键 / Hotkey |
|---|---|
| 全屏录制 / Fullscreen | `Ctrl + Alt + F9` |
| 暂停/继续 / Pause & Resume | `Ctrl + Alt + F10` |
| 区域录制 / Region | `Ctrl + Alt + R` |
| 窗口录制 / Window | `Ctrl + Alt + W` |
| 全屏截图 / Screenshot | `Ctrl + Alt + S` |
| 区域截图 / Screenshot Region | `Ctrl + Alt + Shift + S` |
| 显示主界面 / Show Window | `Ctrl + Alt + B` |
| 切换麦克风 / Toggle Mic | `Ctrl + Alt + M` |
| 切换系统音频 / Toggle System Audio | `Ctrl + Alt + N` |
| 停止并保存 / Stop & Save | `Ctrl + Alt + End` |
| 丢弃录制 / Discard | `Ctrl + Alt + Esc` |
| 切换主题 / Toggle Theme | `Ctrl + T` |

---

## 系统要求 / Requirements

- Windows 10 版本 1809 或更高 / Windows 10 1809+
- 64 位系统 / 64-bit
- 首次启动需联网下载 FFmpeg（约 100MB）/ Internet required for first-run FFmpeg download (~100MB)

---

## 使用说明 / Usage

1. 下载 `BetterSR.exe` 到任意位置。  
   Download `BetterSR.exe` to any folder.
2. 双击运行，首次启动会自动准备 FFmpeg。  
   Double-click to run; FFmpeg will be prepared automatically on first launch.
3. 使用快捷键或界面按钮开始录制。  
   Use hotkeys or the on-screen buttons to start recording.
4. 录制文件默认保存到 `桌面\BetterSR`。  
   Recordings are saved to `Desktop\BetterSR` by default.

---

## 构建 / Build

```bash
dotnet publish src/BetterSR/BetterSR.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o publish
```

---

## 许可证 / License

Available License —— [https://license.kscm.top/available.md](https://license.kscm.top/available.md)

---

Made with 🧵 by kscm.
