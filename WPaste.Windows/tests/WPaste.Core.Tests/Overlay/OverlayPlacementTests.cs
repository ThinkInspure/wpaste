using WPaste.Core.Overlay;

namespace WPaste.Core.Tests.Overlay;

public class OverlayPlacementTests
{
    [Fact]
    public void PlacesPanelAboveBottomMarginWithinWorkArea()
    {
        var workArea = new WorkAreaRect(100, 50, 1540, 850);

        var frame = OverlayPlacement.Calculate(workArea, height: 220);

        Assert.Equal(116, frame.X);
        Assert.Equal(614, frame.Y);
        Assert.Equal(1408, frame.Width);
        Assert.Equal(220, frame.Height);
    }

    [Fact]
    public void ClampsHeightWhenWorkAreaIsShorterThanRequested()
    {
        var workArea = new WorkAreaRect(0, 0, 1920, 120);

        var frame = OverlayPlacement.Calculate(workArea, height: 220);

        Assert.Equal(104, frame.Height);
        Assert.Equal(0, frame.Y);
    }
}
