namespace WPaste.Windows.Features.History;

public struct OverlayNavigation
{
    public int? SelectedIndex { get; private set; }

    public OverlayNavigation(int itemCount)
    {
        var count = Math.Max(0, itemCount);
        SelectedIndex = count > 0 ? 0 : null;
    }

    public readonly OverlayNavigation WithItemCount(int count)
    {
        var navigation = this;
        var itemCount = Math.Max(0, count);
        if (itemCount == 0)
        {
            navigation.SelectedIndex = null;
            return navigation;
        }

        navigation.SelectedIndex = Math.Min(SelectedIndex ?? 0, itemCount - 1);
        return navigation;
    }

    public OverlayNavigation MoveNext(int itemCount)
    {
        if (itemCount <= 0)
        {
            return this;
        }

        SelectedIndex = ((SelectedIndex ?? -1) + 1) % itemCount;
        return this;
    }

    public OverlayNavigation MovePrevious(int itemCount)
    {
        if (itemCount <= 0)
        {
            return this;
        }

        SelectedIndex = ((SelectedIndex ?? 0) - 1 + itemCount) % itemCount;
        return this;
    }

    public OverlayNavigation Select(int index, int itemCount)
    {
        if (index < 0 || index >= itemCount)
        {
            return this;
        }

        SelectedIndex = index;
        return this;
    }
}
