using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using WPaste.Core.Domain;
using WPaste.Core.Overlay;
using WPaste.Windows.AppHost;
using WPaste.Windows.Features.History;
using WPaste.Windows.Ui;

namespace WPaste.Windows.Views;

public sealed partial class HistoryOverlayWindow : Window
{
    private const double CardWidth = 238;
    private const double CardHeight = 220;

    private readonly AppModel _appModel;
    private OverlayNavigation _navigation;
    private readonly Dictionary<Guid, FrameworkElement> _cardElements = new();
    private Grid? _root;
    private ScrollViewer? _scrollViewer;
    private StackPanel? _cardsPanel;
    private TextBox? _searchBox;

    public bool IsVisible { get; private set; }

    private bool _allowDeactivateHide;

    public HistoryOverlayWindow(AppModel appModel)
    {
        _appModel = appModel;
        _navigation = new OverlayNavigation(0);
        Title = "WPaste 历史";
        InitializeComponent();
    }

    private void ConfigureWindowChrome()
    {
        if (AppWindow.Presenter is not OverlappedPresenter presenter)
        {
            return;
        }

        ExtendsContentIntoTitleBar = true;
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsAlwaysOnTop = true;
    }

    private void InitializeComponent()
    {
        _root = new Grid { Padding = new Thickness(16, 12, 16, 12) };
        _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        _searchBox = new TextBox
        {
            PlaceholderText = "搜索历史…",
            Margin = new Thickness(0, 0, 0, 12)
        };
        _searchBox.TextChanged += (_, _) =>
        {
            _appModel.History.Query = _searchBox.Text;
            RefreshCards();
        };
        Grid.SetRow(_searchBox, 0);
        _root.Children.Add(_searchBox);

        _cardsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _cardsPanel
        };
        Grid.SetRow(_scrollViewer, 1);
        _root.Children.Add(_scrollViewer);

        Content = _root;
        Activated += OnWindowActivated;
        _root.KeyDown += OnRootKeyDown;
    }

    public void ShowOverlay()
    {
        _allowDeactivateHide = false;
        ConfigureWindowChrome();
        ApplyPlacement();
        RefreshCards();
        IsVisible = true;
        AppWindow.Show();
        WindowBackdropHelper.TryApply(this);
        Activate();
        _searchBox?.Focus(FocusState.Programmatic);
        DispatcherQueue.TryEnqueue(() => _allowDeactivateHide = true);
    }

    public void HideOverlay()
    {
        IsVisible = false;
        AppWindow.Hide();
    }

    private void ApplyPlacement()
    {
        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
        var work = displayArea.WorkArea;
        var frame = OverlayPlacement.Calculate(
            new WorkAreaRect(work.X, work.Y, work.X + work.Width, work.Y + work.Height));
        AppWindow.MoveAndResize(new RectInt32(frame.X, frame.Y, frame.Width, frame.Height));
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated &&
            _allowDeactivateHide &&
            IsVisible)
        {
            HideOverlay();
            return;
        }

        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            RefreshCards();
        }
    }

    private void RefreshCards()
    {
        if (_cardsPanel is null || _scrollViewer is null)
        {
            return;
        }

        var items = _appModel.History.FilteredItems.ToList();
        _navigation = _navigation.WithItemCount(items.Count);
        _cardsPanel.Children.Clear();
        _cardElements.Clear();

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var card = CreateCard(item, index, items.Count);
            _cardsPanel.Children.Add(card);
            _cardElements[item.Id] = card;
        }

        ScrollToSelected(items);
    }

    private Border CreateCard(ClipboardItem item, int index, int itemCount)
    {
        var (title, subtitle) = HistoryCardContent.Describe(item);
        var isSelected = _navigation.SelectedIndex == index;
        var kindLabel = HistoryCardStyles.KindLabel(item.Payload);

        var header = new Grid
        {
            Background = HistoryCardStyles.HeaderBrush(item.Payload),
            Padding = new Thickness(12, 10, 12, 10),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        header.Children.Add(new TextBlock
        {
            Text = kindLabel,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
        });
        var shortcut = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.White) { Opacity = 0.18 },
            Padding = new Thickness(6, 2, 6, 2),
            CornerRadius = new CornerRadius(999),
            Child = new TextBlock
            {
                Text = $"⌘{index + 1}",
                FontSize = 11,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Opacity = 0.9
            }
        };
        Grid.SetColumn(shortcut, 1);
        header.Children.Add(shortcut);

        var body = new StackPanel
        {
            Padding = new Thickness(12),
            Background = HistoryCardStyles.CardBackgroundBrush(),
            Spacing = 8
        };
        body.Children.Add(new TextBlock
        {
            Text = title,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 4,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });

        var footer = new TextBlock
        {
            Text = subtitle,
            Opacity = 0.65,
            FontSize = 12,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var layout = new StackPanel { Spacing = 0 };
        layout.Children.Add(header);
        layout.Children.Add(body);
        layout.Children.Add(new Border
        {
            Background = HistoryCardStyles.CardBackgroundBrush(),
            Padding = new Thickness(12, 0, 12, 10),
            Child = footer
        });

        var border = new Border
        {
            Width = CardWidth,
            Height = CardHeight,
            CornerRadius = new CornerRadius(12),
            BorderBrush = HistoryCardStyles.CardBorderBrush(isSelected),
            BorderThickness = new Thickness(isSelected ? 3 : 1),
            Background = HistoryCardStyles.CardBackgroundBrush(),
            Child = layout,
            Tag = item
        };

        border.Tapped += (_, _) =>
        {
            _navigation = _navigation.Select(index, itemCount);
            _appModel.Paste(item, plainText: false);
        };

        border.ContextFlyout = CreateContextFlyout(item);
        return border;
    }

    private MenuFlyout CreateContextFlyout(ClipboardItem item)
    {
        var flyout = new MenuFlyout();

        var copyItem = new MenuFlyoutItem { Text = "复制到剪贴板" };
        copyItem.Click += (_, _) => _appModel.CopyToClipboard(item, plainText: false);
        flyout.Items.Add(copyItem);

        var deleteItem = new MenuFlyoutItem { Text = "删除" };
        deleteItem.Click += async (_, _) =>
        {
            await _appModel.DeleteHistoryItemAsync(item);
            RefreshCards();
        };
        flyout.Items.Add(deleteItem);

        return flyout;
    }

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var items = _appModel.History.FilteredItems;
        if (items.Count == 0)
        {
            return;
        }

        switch (e.Key)
        {
            case global::Windows.System.VirtualKey.Escape:
                HideOverlay();
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.Left:
                _navigation = _navigation.MovePrevious(items.Count);
                RefreshCards();
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.Right:
                _navigation = _navigation.MoveNext(items.Count);
                RefreshCards();
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.Enter:
                if (_navigation.SelectedIndex is int index)
                {
                    _appModel.Paste(items[index], plainText: false);
                }

                e.Handled = true;
                break;
        }
    }

    private void ScrollToSelected(IReadOnlyList<ClipboardItem> items)
    {
        if (_navigation.SelectedIndex is not int index || index >= items.Count)
        {
            return;
        }

        if (_cardElements.TryGetValue(items[index].Id, out var element))
        {
            element.StartBringIntoView(new BringIntoViewOptions { HorizontalAlignmentRatio = 0.5 });
        }
    }
}
