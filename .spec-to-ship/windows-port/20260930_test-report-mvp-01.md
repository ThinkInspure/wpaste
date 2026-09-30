# MVP 实现测试报告

> **日期**：2026-09-30  
> **范围**：R1–R16 MVP 首版实现  
> **基线**：`20260928_design-01.md` v0.2

---

## 1. 测试结果

```powershell
cd WPaste.Windows
dotnet test WPaste.Windows.sln -c Release -p:Platform=x64
```

| 项目 | 通过 | 失败 | 跳过 |
|---|---:|---:|---:|
| WPaste.Core.Tests | 2 | 0 | 0 |
| WPaste.Clipboard.Tests | 8 | 0 | 0 |
| WPaste.Paste.Tests | 4 | 0 | 0 |
| WPaste.Persistence.Tests | 3 | 0 | 0 |
| **合计** | **17** | **0** | **0** |

构建：Release x64 — **0 错误 0 警告**

---

## 2. 已实现模块

| 模块 | 状态 |
|---|---|
| SQLite `HistoryRepository` | ✅ 去重、删除、清空、过期清理 |
| `SettingsPersistence` JSON | ✅ |
| `ImageFileStore` | ✅ |
| `ClipboardSnapshotReader` + `ClipboardMonitor` | ✅ WM_CLIPBOARDUPDATE |
| `GlobalHotkeyService` | ✅ Ctrl+Shift+V / 次选 Insert |
| `AppModel` 编排 | ✅ 采集→入库→面板→粘贴 |
| `HistoryOverlayWindow` | ✅ 搜索、列表、←/→/Enter/Esc |
| `SettingsWindow` | ✅ 通用 + 隐私 |
| `OnboardingWindow` | ✅ 首次引导 |
| `TrayIconController` | ✅ 托盘菜单 |

---

## 3. 启动验证

| 项 | 结果 |
|---|---|
| 产物 | `WPaste.Windows/src/WPaste.Windows/bin/x64/Release/net8.0-windows10.0.19041.0/WPaste.Windows.exe` |
| 进程 | 自动化环境启动后立即退出（exit `-2140733418`，需交互桌面会话手工验收） |

---

## 4. 已知 MVP 差距（相对 AC 全矩阵）

| 项 | 说明 |
|---|---|
| R15 右键菜单 | 面板内复制/删除上下文菜单未实现 |
| R4 卡片 UI | 当前 ListView 文本行，非 macOS 卡片带 |
| R4 底部定位 | 未接 Monitor API 工作区定位 |
| 开机自启 | 设置项未接 StartupTask/Registry |
| SP4 MSIX | 打包仍占位 |

---

## 5. 结论

MVP 核心闭环（采集→存储→快捷键/托盘打开→搜索→粘贴）已实现并通过单元测试。可进入手工 AC 验收与 SP4 打包。
