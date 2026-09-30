using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace WPaste.Windows.Ui;

internal static class WindowBackdropHelper
{
    public static void TryApply(Window window)
    {
        try
        {
            if (MicaController.IsSupported())
            {
                window.SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
                return;
            }

            if (DesktopAcrylicController.IsSupported())
            {
                window.SystemBackdrop = new DesktopAcrylicBackdrop();
            }
        }
        catch
        {
            // 部分 Win10 环境在窗口尚未稳定时设置 SystemBackdrop 会失败；忽略并保留默认背景。
        }
    }
}
