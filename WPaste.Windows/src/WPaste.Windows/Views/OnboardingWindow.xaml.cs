using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WPaste.Windows.AppHost;
using WPaste.Windows.Ui;

namespace WPaste.Windows.Views;

public sealed class OnboardingWindow : Window
{
    public OnboardingWindow(AppModel appModel)
    {
        Title = "欢迎使用 WPaste";
        var panel = new StackPanel { Spacing = 12, Padding = new Thickness(24), Width = 460 };
        panel.Children.Add(new TextBlock
        {
            Text = "WPaste 会在本机保存剪贴板历史，默认保留 7 天。",
            TextWrapping = TextWrapping.WrapWholeWords
        });
        panel.Children.Add(new TextBlock
        {
            Text = "选择历史条目后会尝试自动粘贴；若被安全软件拦截，内容仍会复制到剪贴板。",
            TextWrapping = TextWrapping.WrapWholeWords
        });

        var button = new Button { Content = "开始使用" };
        button.Click += (_, _) =>
        {
            appModel.CompleteOnboarding();
            Close();
        };
        panel.Children.Add(button);
        Content = panel;
    }

    public void ShowWindow()
    {
        AppWindowHelper.ShowCentered(this, 520, 360);
    }
}
