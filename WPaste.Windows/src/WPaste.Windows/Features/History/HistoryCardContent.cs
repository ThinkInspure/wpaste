using WPaste.Core.Domain;

namespace WPaste.Windows.Features.History;

internal static class HistoryCardContent
{
    public static (string Title, string Subtitle) Describe(ClipboardItem item)
    {
        var title = item.Payload switch
        {
            ClipboardPayload.TextPayload text => text.Text,
            ClipboardPayload.UrlPayload url => url.Url.AbsoluteUri,
            ClipboardPayload.ImagePayload image => $"图片 {image.Metadata.Width}×{image.Metadata.Height}",
            ClipboardPayload.FilesPayload files => string.Join(", ", files.Files.Select(file => file.DisplayName)),
            _ => "未知"
        };

        return (title, item.Source.ProcessName);
    }
}
