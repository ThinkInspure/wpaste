# SP2 — 剪贴板解析 Spike 报告

> **日期**：2026-09-30  
> **状态**：**通过**  
> **方案**：`20260928_design-01.md` §8 SP2 / §5.2–§5.4

---

## 1. 目标

四类剪贴板内容解析（文件 / 图片 / URL / 文本）与指纹去重，对标 macOS `ClipboardParser` + `ContentFingerprint`。

## 2. 实现范围

| 组件 | 路径 | 状态 |
|---|---|---|
| `ClipboardSnapshot` / `ParsedClipboard` | `WPaste.Clipboard/Parsing/` | ✅ |
| `ClipboardParser` | 优先级 files → image → url → text | ✅ |
| `ContentFingerprint` | `WPaste.Core/Fingerprint/` | ✅ 含 CRLF 规范化、文件顺序 |
| `ClipboardInterop` | `WPaste.Platform/Clipboard/` | ✅ Win32 读写基础 |
| `ClipboardMonitor` | 抑制写入 + PollOnce 编排 | ✅ 骨架可跑 |

## 3. 自动化验证

```powershell
cd WPaste.Windows
dotnet test WPaste.Windows.sln -c Release --filter "FullyQualifiedName~ClipboardParserTests|FullyQualifiedName~ContentFingerprintTests"
```

| 用例（对标 macOS） | AC | 结果 |
|---|---|---|
| 文件优先于图片/URL/文本 | AC1 | ✅ |
| 图片 / URL 规范化 / 文本 | AC1 | ✅ |
| 空剪贴板忽略 | AC1 | ✅ |
| 同文本 CRLF/LF 同 fingerprint | AC2 | ✅ |
| 多文件 CF_HDROP 顺序影响 fingerprint | AC2 | ✅ |

**Clipboard + Core 测试**：10 通过 / 0 失败

## 4. 实机验证

| 步骤 | 结果 |
|---|---|
| `ClipboardWriter` 写入 Unicode 文本 | ✅ |
| 资源管理器多文件 HDROP 采集 | ⏳ 待 `ClipboardSnapshotReader` 联调 |
| 图片 CF_DIB 采集 | ⏳ 待 MVP 阶段 ImageFileStore |

## 5. 结论

- **Gate SP2**：**通过** — 解析优先级与指纹算法与 macOS 0.1.1 一致，单元覆盖 AC1/AC2 样本。
- **下一步**：实现 `ClipboardSnapshotReader`（CF_HDROP / CF_DIB / CF_UNICODETEXT）并接入 `ClipboardMonitor` 事件驱动路径。
