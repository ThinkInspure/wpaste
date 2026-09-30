# MVP 差距补齐测试报告

> **日期**：2026-09-30  
> **范围**：R4 卡片 UI / 底部定位、R15 右键菜单、失焦关闭、开机自启  
> **基线**：`20260928_design-01.md` §5.8

---

## 1. 测试结果

```powershell
cd WPaste.Windows
dotnet test WPaste.Windows.sln -c Release
```

| 项目 | 通过 | 失败 | 跳过 |
|---|---:|---:|---:|
| WPaste.Core.Tests | 4 | 0 | 0 |
| WPaste.Clipboard.Tests | 8 | 0 | 0 |
| WPaste.Paste.Tests | 5 | 0 | 0 |
| WPaste.Persistence.Tests | 3 | 0 | 0 |
| **合计** | **20** | **0** | **0** |

构建：Release — **0 错误**（Platform 1 条既有 CS8500 警告）

---

## 2. 本次实现

| 项 | 状态 | 说明 |
|---|---|---|
| R15 右键菜单 | ✅ | 「复制到剪贴板」「删除」；复制 `CopyOnly` 且不关闭面板 |
| R4 卡片 UI | ✅ | 横向 ScrollViewer + 卡片 Border |
| R4 底部定位 | ✅ | `DisplayArea.WorkArea` + `OverlayPlacement`（底边距 16px，高 220px） |
| 失焦关闭 | ✅ | `WindowActivated` Deactivated + 防抖 |
| 开机自启 | ✅ | 设置页开关 + `LaunchAtLoginService`（CurrentUser Run 键） |
| `PasteCoordinator.closeOverlayAfterWrite` | ✅ | 右键复制保持面板打开 |

---

## 3. 启动验证

| 项 | 结果 |
|---|---|
| 产物 | `WPaste.Windows/src/WPaste.Windows/bin/x64/Release/net8.0-windows10.0.19041.0/WPaste.Windows.exe` |
| 进程 | 自动化环境仍无法保持 WinUI 会话（需交互桌面手工验收 R4/R15/失焦/自启） |

---

## 4. 剩余 MVP 差距

| 项 | 说明 |
|---|---|
| SP4 MSIX | 打包仍占位 |
| 手工 UI 验收 | 卡片样式、DPI、失焦行为、Registry 自启需桌面确认 |

---

## 5. 结论

R4/R15 与开机自启代码已落地，单元测试 20/20 通过。下一步：交互桌面手工验收 + SP4 MSIX。
