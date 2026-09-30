using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WPaste.Core.Domain;

namespace WPaste.Windows.Ui;

internal static class HistoryCardStyles
{
    public static string KindLabel(ClipboardPayload payload) =>
        payload switch
        {
            ClipboardPayload.TextPayload => "文本",
            ClipboardPayload.UrlPayload => "链接",
            ClipboardPayload.ImagePayload => "图片",
            ClipboardPayload.FilesPayload => "文件",
            _ => "未知"
        };

    public static SolidColorBrush HeaderBrush(ClipboardPayload payload) =>
        payload switch
        {
            ClipboardPayload.TextPayload => Brush(0xFF, 0x34, 0xC7, 0x59),
            ClipboardPayload.UrlPayload => Brush(0xFF, 0x58, 0x56, 0xD6),
            ClipboardPayload.ImagePayload => Brush(0xFF, 0x0A, 0x84, 0xFF),
            ClipboardPayload.FilesPayload => Brush(0xFF, 0xFF, 0x95, 0x00),
            _ => Brush(0xFF, 0x8E, 0x8E, 0x93)
        };

    public static SolidColorBrush CardBackgroundBrush() =>
        Application.Current.RequestedTheme == ApplicationTheme.Light
            ? Brush(0xFF, 0xFF, 0xFF, 0xFF)
            : Brush(0xFF, 0x2C, 0x2C, 0x2E);

    public static SolidColorBrush CardBorderBrush(bool selected) =>
        selected
            ? new SolidColorBrush(ColorHelper.FromArgb(255, 10, 132, 255))
            : Brush(0x66, 0xFF, 0xFF, 0xFF);

    private static SolidColorBrush Brush(byte a, byte r, byte g, byte b) =>
        new(ColorHelper.FromArgb(a, r, g, b));
}
