namespace WPaste.Core.Overlay;

public readonly record struct WorkAreaRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;

    public int Height => Bottom - Top;
}

public readonly record struct OverlayFrame(int X, int Y, int Width, int Height);

public static class OverlayPlacement
{
    public const int DefaultHeight = 220;
    public const int BottomMargin = 16;
    public const int HorizontalMargin = 16;

    public static OverlayFrame Calculate(
        WorkAreaRect workArea,
        int height = DefaultHeight,
        int bottomMargin = BottomMargin,
        int horizontalMargin = HorizontalMargin)
    {
        var availableHeight = Math.Max(0, workArea.Height - bottomMargin);
        var actualHeight = Math.Min(height, availableHeight);
        var width = Math.Max(0, workArea.Width - horizontalMargin * 2);
        var x = workArea.Left + horizontalMargin;
        var y = workArea.Top + availableHeight - actualHeight;
        return new OverlayFrame(x, y, width, actualHeight);
    }
}
