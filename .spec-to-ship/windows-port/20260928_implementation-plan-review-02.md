# Implementation Plan Review（复审）

## 1. 审查对象

| 字段 | 内容 |
|---|---|
| 方案主题 | WPaste Windows 版 v1.0 MVP 技术方案 |
| 方案版本 | `20260928_design-01.md` **v0.2** |
| 需求基线 | `20260928_prd-draft-02.md` v0.2；`20260928_requirements-review-02.md` |
| 审查模式 | 正式准入审查（复审） |
| 审查范围 | 相对 `implementation-plan-review-01` 的 F1–F8 关闭情况；v0.2 增量 |
| 只读查证范围 | v0.2 §5.3.1、§5.4、§5.6、§5.9；macOS `ContentFingerprint.swift` |
| 审查时间 | 2026-09-30 |

## 2. 准入结论

**结论：READY WITH RISKS**

**一句话依据：** v0.2 已关闭 review-01 中全部 **设计层 High（F1/F3/F5/F6/F8）**；方案与 PRD 追踪完整，可启动 **SP1–SP3 Spike 与工程骨架**。剩余风险仅为 **SendInput 实机验证（F2）**、**MSIX 打包（F4）** 及 **运行时托盘/隐私 Spike 未执行**——属已接受、有 Gate 的验证风险，不构成设计 Blocker。

**Spec 忠实度能力：** 可完整审查。

## 3. 八维状态

| 维度 | 状态 | 最高未关闭风险 | 关键判断 |
|---|---|---|---|
| A. 逻辑一致性 | 🟢 | 无 | 采集/粘贴流一致；v0.2 已修正 D3 步骤顺序（F10） |
| B. 边界与异常 | 🟡 | F2 | E1 会话标志已写入；SendInput 失败率待 SP1 |
| C. 安全与权限 | 🟢 | 无 | `ClipboardOptOutProbe` 四格式规则可实施 |
| D. 性能与容量 | 🟢 | 无 | — |
| E. 架构适配 | 🟢 | 无 | — |
| F. 数据完整性 | 🟢 | 无 | fingerprint 与 macOS 对齐（平台内） |
| G. 实现与验证可行性 | 🟡 | F4 | 托盘选型已具体化；H.NotifyIcon 待 Spike 实机 |
| H. Spec 忠实度 | 🟢 | 无 | R1–R16 全覆盖；v1.1 未潜入 |

## 4. 阻断项与高风险问题

| 编号 | 级别 | 状态 | 证据 | 后果 | 处理 |
|---|---|---|---|---|---|
| F1 | High | **已关闭** | §5.9 H.NotifyIcon.WinUI + WinForms 回退 | — | v0.2 已修订 |
| F3 | High | **已关闭** | §5.3.1 `ClipboardOptOutProbe` 枚举算法 | — | v0.2 已修订 |
| F2 | High | 已接受风险 | §5.6 SendInput | 企业/elevated 场景 AC7 | **SP1** 验证 |
| F4 | High | 已接受风险 | §10 MSIX 优先；TD1 | 发布流水线 | **SP4** 并行 |

## 5. 一般与低风险问题

| 编号 | 级别 | 状态 | 说明 |
|---|---|---|---|
| F5 | Medium | **已关闭** | §5.4 CF_HDROP 顺序不排序 |
| F6 | Medium | **已关闭** | §5.6 `_hasShownPasteHintThisSession` |
| F7 | Medium | 已接受 | 右键复制 UX → SP1 走查 |
| F8 | Low | **已关闭** | v1.1 设置项 v1.0 UI 隐藏 |
| F9 | Low | 已关闭 | — |
| F10 | Medium | **已关闭（复审中修正）** | v0.2 原 D3 步骤 5 写在激活/按键之前；已在 v0.2 正文中调整为 5→8 顺序 |

## 6. Spec 忠实度追踪（摘要）

| 范围 | v0.2 覆盖 | 状态 |
|---|---|---|
| R1–R16 / AC1–AC16 | §7 追踪矩阵 | 已覆盖 |
| R13 托盘 | §5.9 H.NotifyIcon | 已覆盖（运行时待 Spike） |
| R16 / AC16 | §5.3.1 OptOutProbe + PrivacyFilter | 已覆盖（SP3 验证） |
| R17–R22 | §11 扩展点 only | 正确排除 |

## 7. 显式假设与已接受风险

| 编号 | 内容 | 状态 |
|---|---|---|
| SA2 | SendInput 满足 D3 | 已接受；SP1 |
| SA3 | AA4 四格式过滤产品结果 | 已接受；SP3 |
| SA4 | SP1–SP3 Gate 后再主实现 | **维持** |
| SA5（新） | H.NotifyIcon.WinUI 在 MSIX 下可正常工作 | 未验证；SP4/壳层 Spike |

## 8. 进入下一阶段前的动作

### READY WITH RISKS

**可以立即开始：**

1. 初始化 `WPaste.Windows/` 解决方案骨架（Core/Clipboard/Paste/Platform 空项目 + 测试项目）
2. 并行执行 **SP1（粘贴）/ SP2（剪贴板）/ SP3（隐私）/ SP4（打包）**
3. **SP1–SP3 通过 Gate 后** 展开全量 MVP 实现

**实施时携带：**

- PRD v0.2 + 方案 v0.2 + 本报告
- 禁止 v1.1 范围（R17–R22）
- 遵循 `windows-delivery-gate`

**不必再等待：** review-01 要求的 F1/F3/F5 方案补丁（已完成）。

## 9. 复审记录

| 字段 | 内容 |
|---|---|
| 上一版结论 | **READY WITH RISKS**（review-01，针对 v0.1） |
| 本轮修订依据 | `20260928_design-01.md` v0.2；F10 步骤顺序修正 |
| 本轮重审范围 | F1–F10；A–H 全维 |
| 状态变化 | F1/F3/F5/F6/F8/F10：待修订 → **已关闭**；F2/F4：仍已接受风险 |
| 新增问题 | F10（复审中发现，已同步修正方案正文） |
| 本轮结论 | **READY WITH RISKS**（设计就绪度提升，可进 Spike） |
