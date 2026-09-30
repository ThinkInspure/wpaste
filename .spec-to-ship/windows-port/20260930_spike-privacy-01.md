# SP3 — 隐私过滤 Spike 报告

> **日期**：2026-09-30  
> **状态**：**通过（规则层 + OptOut 探针；浏览器实机矩阵待补）**  
> **方案**：`20260928_design-01.md` §8 SP3 / §5.3.1

---

## 1. 目标

Windows 敏感/瞬时剪贴板映射（AA4）：`ClipboardOptOutProbe` 四格式枚举 + `PrivacyFilter` 规则，对标 macOS `PrivacyFilter.swift`。

## 2. 实现范围

| 规则 | Windows 映射 | 默认 |
|---|---|---|
| 暂停记录 | `AppSettings.RecordingPaused` | — |
| 密码管理器 | exe 名表（1Password、Bitwarden…） | 拒绝 |
| 敏感 | `ExcludeClipboardContentFromMonitorProcessing` / `Clipboard Viewer Ignore` | 拒绝 |
| 瞬时 | `CanIncludeInClipboardHistory` / `CanUploadToCloudClipboard` DWORD==0 | 拒绝 |

**组件**：

- `ClipboardOptOutProbe` — 打开剪贴板后枚举格式 ID / 读 DWORD
- `PrivacyFilter` — 对标 macOS 决策树（v1.0 无忽略应用 Bundle，改用 `IgnoredExeNames`）

## 3. 自动化验证

```powershell
cd WPaste.Windows
dotnet test WPaste.Windows.sln -c Release --filter "FullyQualifiedName~PrivacyFilterTests"
```

| 用例（对标 macOS PrivacyFilterTests） | AC16 | 结果 |
|---|---|---|
| 暂停记录拒绝 | ✅ | ✅ |
| 忽略 exe / 密码管理器 | ✅ | ✅ |
| 敏感/瞬时格式 + 设置开关 | ✅ | ✅ |
| 普通内容允许 | ✅ | ✅ |

**Privacy 测试**：4 通过 / 0 失败

## 4. 实机矩阵（TD3 待补）

| 样本操作 | 预期 | 状态 |
|---|---|---|
| Edge/Chrome 密码框复制 | 不入库 | ⏳ 待 UI 联调验证 |
| 记事本普通文本 | 入库 | ⏳ 待 ClipboardMonitor 联调 |
| 1Password/Bitwarden 复制 | 不入库 | ⏳ 待密码管理器实机 |
| 关闭「忽略敏感内容」 | 对标 macOS 测试 | ⏳ 待设置 UI |

OptOut 探针代码路径已在 `ClipboardOptOutProbe` 实现；浏览器密码框依赖来源应用标记格式，需在联调阶段用 `EnumerateFormatNames()` 记录证据。

## 5. 结论

- **Gate SP3**：**通过** — F3 已关闭：读取方仅枚举格式，不调用写入 API；规则层与 macOS 语义对齐。
- **AA4 残留**：浏览器/密码管理器实机矩阵待 MVP UI 阶段补证据；若格式探测不足则触发 PRD 回写流程。
- **下一步**：ClipboardMonitor 联调时落盘各样本的 `DeclaredFormats` 日志。
