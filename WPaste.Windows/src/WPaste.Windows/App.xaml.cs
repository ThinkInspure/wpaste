using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WPaste.Core.Paths;
using WPaste.Windows.AppHost;
using WPaste.Windows.Tray;

namespace WPaste.Windows;

public partial class App : Application
{
    private AppModel? _appModel;
    private TrayIconController? _tray;

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _appModel = await AppModel.CreateAsync();
            _tray = new TrayIconController(_appModel, DispatcherQueue.GetForCurrentThread());
            _tray.Initialize();
        }
        catch (Exception ex)
        {
            LogStartupFailure(ex);
            ShowFatalError(ex);
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LogStartupFailure(e.Exception);
        ShowFatalError(e.Exception);
        e.Handled = true;
    }

    private static void LogStartupFailure(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataRoot);
            var logPath = Path.Combine(AppPaths.DataRoot, "startup.log");
            File.AppendAllText(logPath, $"{DateTimeOffset.Now:O}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // 忽略日志写入失败。
        }
    }

    private static void ShowFatalError(Exception ex)
    {
        var message = ex.ToString();
        if (message.Length > 1000)
        {
            message = message[..1000] + "...";
        }

        MessageBoxW(0, message, "WPaste 启动失败", 0x00000010);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int MessageBoxW(nint hWnd, string text, string caption, uint type);
}
