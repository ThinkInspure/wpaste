using WPaste.Core.Domain;
using WPaste.Persistence.History;

namespace WPaste.Windows.Features.History;

public sealed class HistoryStore
{
    private readonly HistoryRepository _repository;

    public HistoryStore(HistoryRepository repository)
    {
        _repository = repository;
    }

    public string Query { get; set; } = string.Empty;

    public IReadOnlyList<ClipboardItem> Items { get; private set; } = [];

    public IReadOnlyList<ClipboardItem> FilteredItems =>
        Items.Where(MatchesQuery).ToList();

    public async Task ReloadAsync()
    {
        Items = await _repository.ListRecentAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
        await ReloadAsync();
    }

    private bool MatchesQuery(ClipboardItem item)
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            return true;
        }

        var needle = Query.Trim();
        return GetSearchText(item).Contains(needle, StringComparison.CurrentCultureIgnoreCase);
    }

    private static string GetSearchText(ClipboardItem item)
    {
        var payloadText = item.Payload switch
        {
            ClipboardPayload.TextPayload text => text.Text,
            ClipboardPayload.UrlPayload url => url.Url.AbsoluteUri,
            ClipboardPayload.ImagePayload image => $"{image.Metadata.Width} {image.Metadata.Height}",
            ClipboardPayload.FilesPayload files => string.Join(' ', files.Files.Select(file => file.DisplayName)),
            _ => string.Empty
        };

        return $"{payloadText} {item.Source.ProcessName} {item.Source.ExePath}";
    }
}
