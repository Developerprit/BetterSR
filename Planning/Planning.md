# BetterSR 项目规划 / Project Planning

> 一款更轻、更好看、更听话的 Windows 屏幕录制工具。  
> A lighter, prettier, and more obedient screen recorder for Windows.

---

## 1. 需求确认 / Requirements

| 需求项 | 内容 |
|--------|------|
| 产品形态 | 纯 `.exe` 可执行文件（Windows 桌面端） |
| 启动方式 | 支持开机自启，程序内可开关 |
| 视觉风格 | 浅粉配色 + 背景图 `Background.png` 透明度 50% |
| 主题模式 | 浅色 / 深色 双模式切换 |
| 录制范围 | 全屏 / 区域选择 / 单窗口 |
| 音频来源 | 系统音频 + 麦克风，各自可开关 |
| 输出格式 | MP4（H.264 + AAC） |
| 一言 API | `https://uapis.cn/api/v1/saying`，启动时随机拉取 |
| 快捷键 | 20+ 个录制相关快捷键 |
| 辅助产出 | 中英双语 README + 根目录商业风格 `.html` |

---

## 2. 技术栈 / Tech Stack

| 层级 | 选型 | 说明 |
|------|------|------|
| 框架 | C# WPF on .NET 9 | 原生 Windows UI、自包含单文件发布 |
| 视频捕获 | Windows.Graphics.Capture（Win10 1803+）为主，BitBlt 兜底 | 高性能、支持窗口/区域捕获 |
| 音频捕获 | NAudio | WasapiLoopbackCapture（系统音频）+ WasapiCapture（麦克风） |
| 编码/封装 | FFmpeg 子进程通过 stdin 接收原始帧 | H.264 + AAC → MP4，兼容性与性能兼顾 |
| 全局热键 | Win32 `RegisterHotKey` | 系统级快捷键，即使程序未聚焦也生效 |
| 开机自启 | Windows Registry `Run` key | 用户可在设置中开启/关闭 |
| 网络请求 | `HttpClient` | 拉取一言，带离线 fallback |
| 打包 | `dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained true` | 单文件 exe |

> **注意 / Note**：单文件 exe 会内置 FFmpeg 可执行文件作为资源，首次运行时释放到用户临时目录并调用。最终产物为一个 exe + 一个可选背景图（可内置）。

---

## 3. 功能模块 / Modules

### 3.1 主界面 / Main Window
- 顶部一言展示栏（启动随机刷新）
- 录制控制区：全屏 / 区域 / 窗口 三颗大按钮
- 音频开关：系统音频、麦克风
- 状态栏：录制时长、文件大小、当前模式
- 设置入口：输出目录、帧率、码率、快捷键、开机自启、主题

### 3.2 录制引擎 / Recorder Engine
- 捕获线程（60/30 fps 可选）
- 音频混音线程
- FFmpeg 编码管道
- 暂停/继续/停止/丢弃

### 3.3 区域选择 / Region Picker
- 半透明遮罩层
- 可拖拽矩形框
- 实时显示分辨率
- Esc 取消，Enter 确认

### 3.4 窗口选择 / Window Picker
- 枚举可见窗口
- 鼠标悬停高亮
- 点击确认目标窗口

### 3.5 设置与配置 / Settings
- JSON 配置文件 `%AppData%\BetterSR\settings.json`
- 开机自启开关
- 主题持久化
- 快捷键可自定义（v1 先内置，v2 再开放自定义编辑）

---

## 4. 快捷键方案 / Hotkeys（≥ 20 个）

### 全局热键（系统级）
| # | 功能 | 默认快捷键 |
|---|------|-----------|
| 1 | 开始/停止全屏录制 | Ctrl + Alt + F9 |
| 2 | 暂停/继续录制 | Ctrl + Alt + F10 |
| 3 | 开始区域录制 | Ctrl + Alt + R |
| 4 | 开始窗口录制 | Ctrl + Alt + W |
| 5 | 快速截图（全屏） | Ctrl + Alt + S |
| 6 | 截图区域 | Ctrl + Alt + Shift + S |
| 7 | 打开主界面 | Ctrl + Alt + B |
| 8 | 切换麦克风静音 | Ctrl + Alt + M |
| 9 | 切换系统音频静音 | Ctrl + Alt + N |
| 10 | 显示/隐藏摄像头画中画 | Ctrl + Alt + C |
| 11 | 强制停止并保存 | Ctrl + Alt + End |
| 12 | 强制丢弃当前录制 | Ctrl + Alt + Escape |

### 程序内快捷键
| # | 功能 | 默认快捷键 |
|---|------|-----------|
| 13 | 新建录制 | Ctrl + N |
| 14 | 全屏录制 | Ctrl + Shift + F |
| 15 | 区域录制 | Ctrl + Shift + R |
| 16 | 窗口录制 | Ctrl + Shift + W |
| 17 | 开始/暂停 | Space |
| 18 | 保存设置 | Ctrl + S |
| 19 | 打开输出文件夹 | Ctrl + O |
| 20 | 退出程序 | Ctrl + Q |
| 21 | 切换浅色/深色主题 | Ctrl + T |
| 22 | 显示快捷键帮助 | Ctrl + H |
| 23 | 切换麦克风 | Ctrl + Shift + M |
| 24 | 切换系统音频 | Ctrl + Shift + N |
| 25 | 切换开机自启 | Ctrl + Shift + A |

> 总计 25 个快捷键，覆盖录制全流程。

---

## 5. UI/UX 设计 / Design

- **主色调**：浅粉 `#F8C3CD` / `#FCEFF3`，深粉强调 `#E06C9F`
- **背景**：`Background.png` 作为窗体背景，`Opacity="0.5"`
- **字体**：Segoe UI（西文）+ 微软雅黑（中文）
- **控件**：圆角卡片、微阴影、粉白渐变按钮
- **深色模式**：背景变深紫灰，文字变白，粉色作为强调色
- **主题切换**：设置页开关或 Ctrl + T，持久化到配置文件

---

## 6. 数据流 / Data Flow

```
用户触发快捷键/WPF 按钮
    ↓
RecorderService 选择捕获源（Screen/Region/Window）
    ↓
VideoCaptureLoop 抓取帧 → 写入 FFmpeg stdin
AudioCaptureLoop 抓取 PCM → 写入 FFmpeg stdin
    ↓
FFmpeg 实时编码 → MP4 文件
    ↓
通知 UI 更新状态/时长/文件大小
```

---

## 7. 项目结构 / Project Layout

```
E:\PC\BetterSR\
├── Planning\Planning.md          # 本文档
├── BetterSR.sln
├── src\BetterSR\BetterSR.csproj
├── src\BetterSR\App.xaml
├── src\BetterSR\MainWindow.xaml
├── src\BetterSR\Views\          # 设置页、区域选择、窗口选择
├── src\BetterSR\Services\       # 录制、音频、热键、配置、API
├── src\BetterSR\Models\          # 配置模型、热键模型
├── src\BetterSR\Assets\          # Background.png、FFmpeg 资源
├── README.md                      # 中英双语
├── LICENSE.md                     # Available License
└── BetterSR.html                  # 商业风格宣传页
```

---

## 8. 构建命令 / Build Commands

```bash
# 开发构建
dotnet build src/BetterSR/BetterSR.csproj

# 单文件自包含发布
dotnet publish src/BetterSR/BetterSR.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o publish
```

---

## 9. 风险点与应对 / Risks

| 风险 | 应对 |
|------|------|
| Windows.Graphics.Capture 在某些系统不可用 | BitBlt 兜底；启动时检测能力 |
| 系统音频录制需要 WASAPI Loopback | NAudio 封装，失败时提示用户 |
| 全局热键冲突 | 启动时检测占用，冲突则提示 |
| FFmpeg 版权/体积 | 使用官方 LGPL 构建；首次运行时释放 |
| 单文件启动慢 | 使用 `IncludeNativeLibrariesForSelfExtract=true` |

---

## 10. 待确认 / Open Questions

- 默认输出目录：`%UserProfile%\Videos\BetterSR` 是否可接受？
- 默认帧率：30 fps 或 60 fps？
- 是否需要在托盘常驻？（推荐：是，录屏工具通常托盘化）

---

*规划完成，等待陌老师确认后即可进入开发阶段。*  
*Planning completed, awaiting approval before development begins.*
