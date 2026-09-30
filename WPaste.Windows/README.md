# WPaste Windows

Windows 版 WPaste v1.0 MVP 工程，对标 macOS 0.1.1。技术方案见 `.spec-to-ship/windows-port/20260928_design-01.md`。

## 前置条件

- Windows 10 22H2（19045）或更高
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Windows App SDK 1.6+](https://learn.microsoft.com/windows/apps/windows-app-sdk/)（随 NuGet 还原）
- Visual Studio 2022（可选）：工作负载「使用 C++ 的桌面开发」+「Windows 应用程序开发」

## 快速开始

```powershell
cd WPaste.Windows
dotnet restore WPaste.Windows.sln
dotnet test WPaste.Windows.sln
dotnet build src/WPaste.Windows/WPaste.Windows.csproj -c Debug -p:Platform=x64
```

未打包（`WindowsPackageType=None`）应用已启用 `WindowsAppSDKSelfContained`，运行时随输出目录分发，**无需单独安装 Windows App SDK 1.6 运行时**。

数据目录：`%LocalAppData%\WPaste\`（`wpaste.db`、`settings.json`、`Images/`）。

## 解决方案结构

| 项目 | 职责 |
| --- | --- |
| `WPaste.Core` | 领域模型、ContentFingerprint、AppSettings |
| `WPaste.Platform` | CsWin32、NativeMessageWindow、前台窗口与 SendInput |
| `WPaste.Clipboard` | 监听、解析、PrivacyFilter、ClipboardOptOutProbe |
| `WPaste.Persistence` | SQLite、图片文件、RetentionCleaner |
| `WPaste.Paste` | PasteCoordinator、ClipboardWriter |
| `WPaste.Shortcuts` | GlobalHotkeyService |
| `WPaste.Windows` | WinUI 3 入口、托盘、设置（薄 UI） |

v1.1 占位：`src/WPaste.Pinboards/`、`src/WPaste.Preview/`（无项目引用）。

## 当前状态

骨架阶段：接口与类型已就位，核心逻辑以 `NotImplementedException` / TODO 标记，待 **SP1–SP3** Spike 通过后实现。

## 打包

```powershell
.\packaging\scripts\test.ps1
.\packaging\scripts\build.ps1
.\packaging\scripts\package-msix.ps1   # SP4 完成后启用
```

产物目标：`build/WPaste-1.0.0-x64.msix` 或 EXE 安装器（见方案 §10）。
