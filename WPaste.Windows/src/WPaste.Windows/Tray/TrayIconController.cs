using H.NotifyIcon;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WPaste.Windows.AppHost;
using WPaste.Windows.Ui;

namespace WPaste.Windows.Tray;

public sealed class TrayIconController : IDisposable
{
    private readonly AppModel _appModel;
    private readonly DispatcherQueue _dispatcher;
    private TaskbarIcon? _icon;

    public TrayIconController(AppModel appModel, DispatcherQueue dispatcher)
    {
        _appModel = appModel;
        _dispatcher = dispatcher;
    }

    public void Initialize()
    {
        _icon = new TaskbarIcon
        {
            ToolTipText = "WPaste",
            IconSource = ResolveIconSource(),
            ContextMenuMode = ContextMenuMode.PopupMenu,
            DoubleClickCommand = TrayCommandFactory.Create(_dispatcher, _appModel.ShowHistory)
        };

        var menu = new MenuFlyout();
        AddMenuItem(menu, "打开历史", _appModel.ShowHistory);
        AddMenuItem(menu, "暂停/恢复记录", _appModel.ToggleRecordingPaused);
        AddMenuItem(menu, "设置", _appModel.ShowSettings);
        AddMenuItem(menu, "退出", _appModel.Quit);
        _icon.ContextFlyout = menu;

        try
        {
            _icon.ForceCreate();
        }
        catch
        {
            _icon.IconSource = CreateFallbackIconSource();
            _icon.ForceCreate();
        }
    }

    private static ImageSource ResolveIconSource()
    {
        foreach (var relativePath in new[] { "Assets\\WPaste.ico" })
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, relativePath);
            if (!File.Exists(iconPath))
            {
                continue;
            }

            try
            {
                var uri = new Uri(Path.GetFullPath(iconPath));
                return new BitmapImage(uri);
            }
            catch
            {
                // 文件损坏或格式不匹配时回退到程序生成图标。
            }
        }

        return CreateFallbackIconSource();
    }

    private static GeneratedIconSource CreateFallbackIconSource() =>
        new()
        {
            Text = "W",
            Foreground = new SolidColorBrush(Colors.White),
            Background = new SolidColorBrush(ColorHelper.FromArgb(255, 88, 86, 214)),
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        };

    private void AddMenuItem(MenuFlyout menu, string label, Action action)
    {
        menu.Items.Add(new MenuFlyoutItem
        {
            Text = label,
            Command = TrayCommandFactory.Create(_dispatcher, action)
        });
    }

    public void Dispose()
    {
        _icon?.Dispose();
    }
}
