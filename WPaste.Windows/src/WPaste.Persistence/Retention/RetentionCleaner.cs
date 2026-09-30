using WPaste.Core.Domain;

namespace WPaste.Persistence.Retention;

public interface IRetentionCleaner
{
    Task<int> RunAsync(AppSettings settings, DateTimeOffset? now = null, CancellationToken cancellationToken = default);
}

public sealed class RetentionCleaner : IRetentionCleaner
{
    private readonly History.HistoryRepository _repository;

    public RetentionCleaner(History.HistoryRepository repository)
    {
        _repository = repository;
    }

    public Task<int> RunAsync(
        AppSettings settings,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default) =>
        _repository.CleanExpiredAsync(settings.Retention, now ?? DateTimeOffset.UtcNow, cancellationToken);
}
