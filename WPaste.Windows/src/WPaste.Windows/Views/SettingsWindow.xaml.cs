using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WPaste.Core.Domain;
using WPaste.Windows.AppHost;
using WPaste.Windows.Ui;

namespace WPaste.Windows.Views;

public sealed class SettingsWindow : Window
{
    private readonly AppModel _appModel;
    private ComboBox? _retentionBox;
    private ComboBox? _pasteBehaviorBox;
    private ToggleSwitch? _plainTextSwitch;
    private ToggleSwitch? _sensitiveSwitch;
    private ToggleSwitch? _transientSwitch;
    private ToggleSwitch? _recordingSwitch;
    private ToggleSwitch? _clearOnQuitSwitch;
    private ToggleSwitch? _launchAtLoginSwitch;

    public SettingsWindow(AppModel appModel)
    {
        _appModel = appModel;
        Title = "WPaste 设置";
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var panel = new StackPanel { Spacing = 12, Padding = new Thickness(20), Width = 420 };

        _pasteBehaviorBox = new ComboBox { Header = "默认粘贴行为", Width = 280 };
        _pasteBehaviorBox.Items.Add("自动粘贴");
        _pasteBehaviorBox.Items.Add("仅复制");
        _pasteBehaviorBox.SelectedIndex = _appModel.Settings.DefaultPasteBehavior == DefaultPasteBehavior.Automatic ? 0 : 1;

        _plainTextSwitch = CreateToggle("默认纯文本粘贴", _appModel.Settings.DefaultPlainText);
        _retentionBox = new ComboBox { Header = "保留期限", Width = 280 };
        foreach (var label in new[] { "1 天", "1 周", "1 个月", "1 年", "永久" })
        {
            _retentionBox.Items.Add(label);
        }

        _retentionBox.SelectedIndex = _appModel.Settings.Retention switch
        {
            HistoryRetention.OneDay => 0,
            HistoryRetention.SevenDays => 1,
            HistoryRetention.OneMonth => 2,
            HistoryRetention.OneYear => 3,
            HistoryRetention.Forever => 4,
            _ => 1
        };

        _sensitiveSwitch = CreateToggle("忽略敏感内容", _appModel.Settings.IgnoreSensitiveContent);
        _transientSwitch = CreateToggle("忽略瞬时内容", _appModel.Settings.IgnoreTransientContent);
        _recordingSwitch = CreateToggle("暂停记录", _appModel.Settings.RecordingPaused);
        _clearOnQuitSwitch = CreateToggle("退出时清空历史", _appModel.Settings.ClearHistoryOnQuit);
        _launchAtLoginSwitch = CreateToggle("登录时启动", _appModel.Settings.LaunchAtLogin);

        var saveButton = new Button { Content = "保存" };
        saveButton.Click += async (_, _) => await SaveAsync();

        var clearButton = new Button { Content = "清空历史" };
        clearButton.Click += async (_, _) => await _appModel.ClearHistoryAsync();

        panel.Children.Add(_pasteBehaviorBox);
        panel.Children.Add(_plainTextSwitch);
        panel.Children.Add(_retentionBox);
        panel.Children.Add(_sensitiveSwitch);
        panel.Children.Add(_transientSwitch);
        panel.Children.Add(_recordingSwitch);
        panel.Children.Add(_clearOnQuitSwitch);
        panel.Children.Add(_launchAtLoginSwitch);
        panel.Children.Add(saveButton);
        panel.Children.Add(clearButton);

        Content = new ScrollViewer { Content = panel };
    }

    public void ShowWindow()
    {
        AppWindowHelper.ShowCentered(this, 480, 640);
    }

    private static ToggleSwitch CreateToggle(string header, bool isOn) =>
        new() { Header = header, IsOn = isOn };

    private async Task SaveAsync()
    {
        _appModel.Settings.DefaultPasteBehavior =
            _pasteBehaviorBox?.SelectedIndex == 0 ? DefaultPasteBehavior.Automatic : DefaultPasteBehavior.CopyOnly;
        _appModel.Settings.DefaultPlainText = _plainTextSwitch?.IsOn ?? false;
        _appModel.Settings.Retention = _retentionBox?.SelectedIndex switch
        {
            0 => HistoryRetention.OneDay,
            1 => HistoryRetention.SevenDays,
            2 => HistoryRetention.OneMonth,
            3 => HistoryRetention.OneYear,
            4 => HistoryRetention.Forever,
            _ => HistoryRetention.SevenDays
        };
        _appModel.Settings.IgnoreSensitiveContent = _sensitiveSwitch?.IsOn ?? true;
        _appModel.Settings.IgnoreTransientContent = _transientSwitch?.IsOn ?? true;
        _appModel.Settings.RecordingPaused = _recordingSwitch?.IsOn ?? false;
        _appModel.Settings.ClearHistoryOnQuit = _clearOnQuitSwitch?.IsOn ?? false;
        _appModel.Settings.LaunchAtLogin = _launchAtLoginSwitch?.IsOn ?? false;
        await _appModel.PersistSettingsAsync();
        _appModel.UserNotice = "设置已保存。";
    }
}
