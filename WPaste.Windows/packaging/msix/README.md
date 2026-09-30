# MSIX 打包占位

SP4 完成后在此目录补充：

- `Package.appxmanifest`（Identity Name=`com.chujianyun.wpaste` 等价包名）
- `Assets/`（从 macOS 图标导出多尺寸 `WPaste.ico` / PNG）
- `windows.startupTask` 开机自启声明

详见 `.spec-to-ship/windows-port/20260928_design-01.md` §5.9、§10。
