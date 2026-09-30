using WPaste.Core.Domain;
using WPaste.Persistence.History;

namespace WPaste.Persistence.Tests.History;

public class HistoryRepositoryTests : IAsyncLifetime
{
    private HistoryRepository _repository = null!;

    private static readonly ClipboardSource Source = new("文本编辑", @"C:\Windows\System32\notepad.exe");

    public async Task InitializeAsync()
    {
        _repository = HistoryRepository.CreateInMemory();
        await _repository.InitializeAsync();
    }

    public Task DisposeAsync() => _repository.DisposeAsync().AsTask();

    [Fact]
    public async Task DuplicateFingerprintUpdatesExistingItemAndMovesItFirst()
    {
        var start = DateTimeOffset.FromUnixTimeSeconds(1_000);
        await _repository.UpsertAsync(CreateItem("first", "same", start));
        await _repository.UpsertAsync(CreateItem("other", "other", start.AddSeconds(1)));
        await _repository.UpsertAsync(CreateItem("first updated", "same", start.AddSeconds(2)));

        var items = await _repository.ListRecentAsync();
        Assert.Equal(2, items.Count);
        Assert.Equal(["same", "other"], items.Select(static item => item.Fingerprint));
        Assert.Equal("first updated", ((ClipboardPayload.TextPayload)items[0].Payload).Text);
    }

    [Fact]
    public async Task SevenDayRetentionRemovesOnlyExpiredItems()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1_000_000);
        await _repository.UpsertAsync(CreateItem("expired", "old", now.AddSeconds(-604_801)));
        await _repository.UpsertAsync(CreateItem("kept", "new", now.AddSeconds(-604_799)));

        var removed = await _repository.CleanExpiredAsync(HistoryRetention.SevenDays, now);

        Assert.Equal(1, removed);
        var items = await _repository.ListRecentAsync();
        Assert.Equal(["new"], items.Select(static item => item.Fingerprint));
    }

    [Fact]
    public async Task ForeverRetentionDoesNotDeleteItems()
    {
        await _repository.UpsertAsync(CreateItem("kept", "kept", DateTimeOffset.MinValue));
        var removed = await _repository.CleanExpiredAsync(HistoryRetention.Forever, DateTimeOffset.UtcNow);
        Assert.Equal(0, removed);
        Assert.Single(await _repository.ListRecentAsync());
    }

    private static ClipboardItem CreateItem(string text, string fingerprint, DateTimeOffset timestamp) =>
        ClipboardItem.Create(
            new ClipboardPayload.TextPayload(text),
            fingerprint,
            Source,
            timestamp);
}
