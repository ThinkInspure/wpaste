using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace WPaste.Windows.Ui;

internal static class AppWindowHelper
{
    public static void ShowCentered(Window window, int width, int height)
    {
        var displayArea = DisplayArea.GetFromWindowId(window.AppWindow.Id, DisplayAreaFallback.Primary);
        var work = displayArea.WorkArea;
        var x = work.X + Math.Max(0, (work.Width - width) / 2);
        var y = work.Y + Math.Max(0, (work.Height - height) / 2);
        window.AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        window.AppWindow.Show();
        WindowBackdropHelper.TryApply(window);
        window.Activate();
    }
}
