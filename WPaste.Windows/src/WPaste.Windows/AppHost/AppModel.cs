using WPaste.Clipboard.Monitoring;
using WPaste.Clipboard.Parsing;
using WPaste.Clipboard.Privacy;
using WPaste.Core.Domain;
using WPaste.Core.Paste;
using WPaste.Paste.Coordination;
using WPaste.Paste.Writing;
using WPaste.Persistence.History;
using WPaste.Persistence.Images;
using WPaste.Persistence.Retention;
using WPaste.Persistence.Settings;
using WPaste.Platform.Input;
using WPaste.Platform.Windows;
using WPaste.Shortcuts;
using WPaste.Windows.Features.History;
using WPaste.Windows.Views;

namespace WPaste.Windows.AppHost;

public sealed class AppModel : IDisposable
{
    public AppSettings Settings { get; private set; }
    public string? UserNotice { get; set; }
    public HistoryStore History { get; }

    private readonly HistoryRepository _repository;
    private readonly ImageFileStore _imageFileStore = new();
    private readonly SettingsPersistence _settingsPersistence = new();
    private readonly NativeMessageWindow _messageWindow = new();
    private readonly ClipboardMonitor _monitor;
    private readonly GlobalHotkeyService _hotkeys = new();
    private readonly PasteCoordinator _pasteCoordinator;
    private readonly RetentionCleaner _retentionCleaner;
    private readonly HistoryOverlayWindow _overlay;
    private readonly SettingsWindow _settingsWindow;
    private readonly OnboardingWindow _onboardingWindow;
    private readonly KeyboardInputClient _keyboard = new();
    private readonly ForegroundWindowClient _foreground = new();
    private readonly ClipboardWriter _clipboardWriter = new();

    private IForegroundTarget? _pasteTarget;
    private HistoryOverlayWindow? _visibleOverlay;
    private Timer? _maintenanceTimer;

    private AppModel(AppSettings settings, HistoryRepository repository)
    {
        Settings = settings;
        _repository = repository;
        History = new HistoryStore(repository);
        _retentionCleaner = new RetentionCleaner(repository);

        _overlay = new HistoryOverlayWindow(this);
        _settingsWindow = new SettingsWindow(this);
        _onboardingWindow = new OnboardingWindow(this);

        _monitor = new ClipboardMonitor(
            new ClipboardParser(),
            new PrivacyFilter(),
            new ClipboardOptOutProbe(),
            () => Settings,
            ClipboardSnapshotReader.TryReadSnapshot,
            OnClipboardAcceptedAsync);

        _pasteCoordinator = new PasteCoordinator(
            _clipboardWriter,
            _keyboard,
            closeOverlay: () => _visibleOverlay?.HideOverlay(),
            suppressWrite: (fingerprint, until) =>
                _monitor.SuppressWrite(fingerprint, until - DateTimeOffset.UtcNow));
    }

    public static async Task<AppModel> CreateAsync()
    {
        var settings = await new SettingsPersistence().LoadAsync();
        var repository = new HistoryRepository();
        await repository.InitializeAsync();
        var model = new AppModel(settings, repository);
        await model.InitializeAsync();
        return model;
    }

    private async Task InitializeAsync()
    {
        await History.ReloadAsync();
        _ = await _retentionCleaner.RunAsync(Settings);

        _messageWindow.Create();
        _monitor.Start(_messageWindow);
        if (!_hotkeys.TryRegister(_messageWindow))
        {
            UserNotice = "快捷键被占用，请从托盘打开历史。";
        }

        _hotkeys.HistoryHotkeyPressed += (_, _) => ShowHistory();
        _maintenanceTimer = new Timer(
            _ => _ = _retentionCleaner.RunAsync(Settings),
            null,
            TimeSpan.FromHours(6),
            TimeSpan.FromHours(6));

        ApplyLaunchAtLogin();

        if (!Settings.HasCompletedOnboarding)
        {
            _onboardingWindow.ShowWindow();
        }
    }

    public void ShowHistory()
    {
        if (_visibleOverlay?.IsVisible == true)
        {
            _visibleOverlay.HideOverlay();
            return;
        }

        _pasteTarget = _foreground.CaptureCurrent();
        _ = ShowHistoryAsync();
    }

    private async Task ShowHistoryAsync()
    {
        await History.ReloadAsync();
        _visibleOverlay = _overlay;
        _overlay.ShowOverlay();
    }

    public void ShowSettings()
    {
        _visibleOverlay?.HideOverlay();
        _settingsWindow.ShowWindow();
    }

    public void ToggleRecordingPaused()
    {
        Settings.RecordingPaused = !Settings.RecordingPaused;
        _ = PersistSettingsAsync();
        UserNotice = Settings.RecordingPaused ? "已暂停记录剪贴板。" : "已恢复记录剪贴板。";
    }

    public async Task PersistSettingsAsync()
    {
        await _settingsPersistence.SaveAsync(Settings);
        ApplyLaunchAtLogin();
    }

    public void CopyToClipboard(ClipboardItem item, bool plainText)
    {
        var usePlainText = plainText || Settings.DefaultPlainText;
        var result = _pasteCoordinator.Paste(
            item,
            PasteMode.CopyOnly,
            target: null,
            usePlainText,
            closeOverlayAfterWrite: false);

        UserNotice = result.Kind switch
        {
            PasteResultKind.Copied => "已复制到剪贴板。",
            PasteResultKind.Unavailable => "内容不可用，未更改剪贴板。",
            _ => UserNotice
        };
    }

    public async Task DeleteHistoryItemAsync(ClipboardItem item)
    {
        await History.DeleteAsync(item.Id);
    }

    public async Task ClearHistoryAsync()
    {
        await _repository.ClearAsync();
        await History.ReloadAsync();
        UserNotice = "历史已清空。";
    }

    public void Paste(ClipboardItem item, bool plainText)
    {
        var mode = Settings.DefaultPasteBehavior == DefaultPasteBehavior.Automatic
            ? PasteMode.Automatic
            : PasteMode.CopyOnly;
        var usePlainText = plainText || Settings.DefaultPlainText;
        var result = _pasteCoordinator.Paste(
            item,
            mode,
            _pasteTarget,
            usePlainText,
            onPasteHintRequired: () => UserNotice = "部分安全软件可能阻止自动粘贴；内容已复制到剪贴板。");

        UserNotice = result.Kind switch
        {
            PasteResultKind.Pasted => null,
            PasteResultKind.Copied => "已复制到剪贴板。",
            PasteResultKind.CopiedOnly => result.FallbackReason switch
            {
                PasteFallbackReason.InputSimulationFailed => UserNotice ?? "无法发送粘贴按键；内容已复制到剪贴板。",
                PasteFallbackReason.TargetUnavailable => "原应用已退出；内容已复制到剪贴板。",
                PasteFallbackReason.ActivationFailed => "无法恢复原应用；内容已复制到剪贴板。",
                _ => "内容已复制到剪贴板。"
            },
            PasteResultKind.Unavailable => "内容不可用，未更改剪贴板。",
            _ => UserNotice
        };
    }

    public void CompleteOnboarding()
    {
        Settings.HasCompletedOnboarding = true;
        _ = PersistSettingsAsync();
    }

    public void Quit()
    {
        if (Settings.ClearHistoryOnQuit)
        {
            _ = _repository.ClearAsync();
        }

        Dispose();
        Environment.Exit(0);
    }

    private async Task OnClipboardAcceptedAsync(ParsedClipboard parsed, string fingerprint)
    {
        var payload = parsed.Payload;
        if (parsed.Payload is ClipboardPayload.ImagePayload imagePayload &&
            parsed.ImageData is { Length: > 0 } imageData)
        {
            var relativePath = await _imageFileStore.SaveAsync(imageData);
            payload = new ClipboardPayload.ImagePayload(
                imagePayload.Metadata with { RelativePath = relativePath });
        }

        var item = ClipboardItem.Create(payload, fingerprint, parsed.Source);
        await _repository.UpsertAsync(item);
        await History.ReloadAsync();
    }

    private void ApplyLaunchAtLogin()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return;
        }

        LaunchAtLoginService.SetEnabled(Settings.LaunchAtLogin, executablePath);
    }

    public void Dispose()
    {
        _maintenanceTimer?.Dispose();
        _hotkeys.Unregister();
        _monitor.Stop();
        _messageWindow.Dispose();
        _repository.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
