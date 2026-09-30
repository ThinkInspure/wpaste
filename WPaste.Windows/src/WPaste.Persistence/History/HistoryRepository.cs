using System.Text.Json;
using Microsoft.Data.Sqlite;
using WPaste.Core.Domain;
using WPaste.Core.Paths;
using WPaste.Persistence.Images;

namespace WPaste.Persistence.History;

public interface IHistoryRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<ClipboardItem> UpsertAsync(ClipboardItem item, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClipboardItem>> ListRecentAsync(int limit = 500, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);

    Task<int> CleanExpiredAsync(HistoryRetention retention, DateTimeOffset now, CancellationToken cancellationToken = default);
}

public sealed class HistoryRepository : IHistoryRepository, IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IImageFileStore? _imageFileStore;
    private readonly bool _ownsConnection;

    public HistoryRepository(string? connectionString = null, IImageFileStore? imageFileStore = null)
    {
        _ownsConnection = true;
        _connection = new SqliteConnection(connectionString ?? $"Data Source={AppPaths.DatabasePath}");
        _imageFileStore = imageFileStore ?? new ImageFileStore();
    }

    internal HistoryRepository(SqliteConnection connection, IImageFileStore? imageFileStore = null)
    {
        _ownsConnection = false;
        _connection = connection;
        _imageFileStore = imageFileStore;
    }

    public static HistoryRepository CreateInMemory(IImageFileStore? imageFileStore = null)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return new HistoryRepository(connection, imageFileStore);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

        Directory.CreateDirectory(AppPaths.DataRoot);
        await using var command = _connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS clipboard_items (
                id TEXT PRIMARY KEY,
                fingerprint TEXT NOT NULL UNIQUE,
                kind TEXT NOT NULL,
                text_value TEXT,
                url_value TEXT,
                image_width INTEGER,
                image_height INTEGER,
                image_relative_path TEXT,
                files_json TEXT,
                source_process_name TEXT NOT NULL,
                source_exe_path TEXT,
                created_at TEXT NOT NULL,
                last_used_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_last_used ON clipboard_items(last_used_at DESC);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ClipboardItem> UpsertAsync(ClipboardItem item, CancellationToken cancellationToken = default)
    {
        await using var select = _connection.CreateCommand();
        select.CommandText = "SELECT id, created_at FROM clipboard_items WHERE fingerprint = $fingerprint";
        select.Parameters.AddWithValue("$fingerprint", item.Fingerprint);
        await using var reader = await select.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var existingId = Guid.Parse(reader.GetString(0));
            var createdAt = DateTimeOffset.Parse(reader.GetString(1));
            var updated = item with { Id = existingId, CreatedAt = createdAt };
            await UpdateRowAsync(updated, cancellationToken);
            return updated;
        }

        await InsertRowAsync(item, cancellationToken);
        return item;
    }

    public async Task<IReadOnlyList<ClipboardItem>> ListRecentAsync(
        int limit = 500,
        CancellationToken cancellationToken = default)
    {
        var items = new List<ClipboardItem>();
        await using var command = _connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, fingerprint, kind, text_value, url_value, image_width, image_height,
                   image_relative_path, files_json, source_process_name, source_exe_path,
                   created_at, last_used_at
            FROM clipboard_items
            ORDER BY last_used_at DESC
            LIMIT $limit
            """;
        command.Parameters.AddWithValue("$limit", limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (TryReadItem(reader, out var item))
            {
                items.Add(item);
            }
        }

        return items;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string? imagePath = null;
        await using (var select = _connection.CreateCommand())
        {
            select.CommandText = "SELECT image_relative_path FROM clipboard_items WHERE id = $id";
            select.Parameters.AddWithValue("$id", id.ToString());
            imagePath = await select.ExecuteScalarAsync(cancellationToken) as string;
        }

        await using var delete = _connection.CreateCommand();
        delete.CommandText = "DELETE FROM clipboard_items WHERE id = $id";
        delete.Parameters.AddWithValue("$id", id.ToString());
        await delete.ExecuteNonQueryAsync(cancellationToken);

        if (!string.IsNullOrEmpty(imagePath) && _imageFileStore is not null)
        {
            await _imageFileStore.DeleteAsync(imagePath, cancellationToken);
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        var imagePaths = new List<string>();
        await using (var select = _connection.CreateCommand())
        {
            select.CommandText = "SELECT image_relative_path FROM clipboard_items WHERE image_relative_path IS NOT NULL";
            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (!reader.IsDBNull(0))
                {
                    imagePaths.Add(reader.GetString(0));
                }
            }
        }

        await using var delete = _connection.CreateCommand();
        delete.CommandText = "DELETE FROM clipboard_items";
        await delete.ExecuteNonQueryAsync(cancellationToken);

        if (_imageFileStore is not null)
        {
            foreach (var path in imagePaths)
            {
                await _imageFileStore.DeleteAsync(path, cancellationToken);
            }
        }
    }

    public async Task<int> CleanExpiredAsync(
        HistoryRetention retention,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetRetentionDuration(retention, out var duration))
        {
            return 0;
        }

        var cutoff = now - duration;
        var expiredIds = new List<string>();
        var imagePaths = new List<string>();

        await using (var select = _connection.CreateCommand())
        {
            select.CommandText =
                """
                SELECT id, image_relative_path FROM clipboard_items
                WHERE last_used_at < $cutoff
                """;
            select.Parameters.AddWithValue("$cutoff", cutoff.ToString("O"));
            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                expiredIds.Add(reader.GetString(0));
                if (!reader.IsDBNull(1))
                {
                    imagePaths.Add(reader.GetString(1));
                }
            }
        }

        if (expiredIds.Count == 0)
        {
            return 0;
        }

        await using var delete = _connection.CreateCommand();
        delete.CommandText = $"DELETE FROM clipboard_items WHERE id IN ({string.Join(',', expiredIds.Select((_, i) => $"$id{i}"))})";
        for (var i = 0; i < expiredIds.Count; i++)
        {
            delete.Parameters.AddWithValue($"$id{i}", expiredIds[i]);
        }

        await delete.ExecuteNonQueryAsync(cancellationToken);

        if (_imageFileStore is not null)
        {
            foreach (var path in imagePaths)
            {
                await _imageFileStore.DeleteAsync(path, cancellationToken);
            }
        }

        return expiredIds.Count;
    }

    public async ValueTask DisposeAsync()
    {
        if (_ownsConnection)
        {
            await _connection.DisposeAsync();
        }
    }

    private async Task InsertRowAsync(ClipboardItem item, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO clipboard_items (
                id, fingerprint, kind, text_value, url_value, image_width, image_height,
                image_relative_path, files_json, source_process_name, source_exe_path,
                created_at, last_used_at)
            VALUES (
                $id, $fingerprint, $kind, $text_value, $url_value, $image_width, $image_height,
                $image_relative_path, $files_json, $source_process_name, $source_exe_path,
                $created_at, $last_used_at)
            """;
        BindItem(command, item);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpdateRowAsync(ClipboardItem item, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText =
            """
            UPDATE clipboard_items SET
                kind = $kind,
                text_value = $text_value,
                url_value = $url_value,
                image_width = $image_width,
                image_height = $image_height,
                image_relative_path = $image_relative_path,
                files_json = $files_json,
                source_process_name = $source_process_name,
                source_exe_path = $source_exe_path,
                last_used_at = $last_used_at
            WHERE fingerprint = $fingerprint
            """;
        BindItem(command, item);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void BindItem(SqliteCommand command, ClipboardItem item)
    {
        command.Parameters.AddWithValue("$id", item.Id.ToString());
        command.Parameters.AddWithValue("$fingerprint", item.Fingerprint);
        command.Parameters.AddWithValue("$source_process_name", item.Source.ProcessName);
        command.Parameters.AddWithValue("$source_exe_path", (object?)item.Source.ExePath ?? DBNull.Value);
        command.Parameters.AddWithValue("$created_at", item.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$last_used_at", item.LastUsedAt.ToString("O"));

        switch (item.Payload)
        {
            case ClipboardPayload.TextPayload text:
                command.Parameters.AddWithValue("$kind", "text");
                command.Parameters.AddWithValue("$text_value", text.Text);
                command.Parameters.AddWithValue("$url_value", DBNull.Value);
                command.Parameters.AddWithValue("$image_width", DBNull.Value);
                command.Parameters.AddWithValue("$image_height", DBNull.Value);
                command.Parameters.AddWithValue("$image_relative_path", DBNull.Value);
                command.Parameters.AddWithValue("$files_json", DBNull.Value);
                break;
            case ClipboardPayload.UrlPayload url:
                command.Parameters.AddWithValue("$kind", "url");
                command.Parameters.AddWithValue("$text_value", DBNull.Value);
                command.Parameters.AddWithValue("$url_value", url.Url.AbsoluteUri);
                command.Parameters.AddWithValue("$image_width", DBNull.Value);
                command.Parameters.AddWithValue("$image_height", DBNull.Value);
                command.Parameters.AddWithValue("$image_relative_path", DBNull.Value);
                command.Parameters.AddWithValue("$files_json", DBNull.Value);
                break;
            case ClipboardPayload.ImagePayload image:
                command.Parameters.AddWithValue("$kind", "image");
                command.Parameters.AddWithValue("$text_value", DBNull.Value);
                command.Parameters.AddWithValue("$url_value", DBNull.Value);
                command.Parameters.AddWithValue("$image_width", image.Metadata.Width);
                command.Parameters.AddWithValue("$image_height", image.Metadata.Height);
                command.Parameters.AddWithValue("$image_relative_path", image.Metadata.RelativePath);
                command.Parameters.AddWithValue("$files_json", DBNull.Value);
                break;
            case ClipboardPayload.FilesPayload files:
                command.Parameters.AddWithValue("$kind", "files");
                command.Parameters.AddWithValue("$text_value", DBNull.Value);
                command.Parameters.AddWithValue("$url_value", DBNull.Value);
                command.Parameters.AddWithValue("$image_width", DBNull.Value);
                command.Parameters.AddWithValue("$image_height", DBNull.Value);
                command.Parameters.AddWithValue("$image_relative_path", DBNull.Value);
                command.Parameters.AddWithValue("$files_json", JsonSerializer.Serialize(files.Files));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(item));
        }
    }

    private static bool TryReadItem(SqliteDataReader reader, out ClipboardItem item)
    {
        item = null!;
        try
        {
            var id = Guid.Parse(reader.GetString(0));
            var fingerprint = reader.GetString(1);
            var kind = reader.GetString(2);
            var source = new ClipboardSource(reader.GetString(9), reader.IsDBNull(10) ? null : reader.GetString(10));
            var createdAt = DateTimeOffset.Parse(reader.GetString(11));
            var lastUsedAt = DateTimeOffset.Parse(reader.GetString(12));

            ClipboardPayload payload = kind switch
            {
                "text" => new ClipboardPayload.TextPayload(reader.GetString(3)),
                "url" => new ClipboardPayload.UrlPayload(new Uri(reader.GetString(4))),
                "image" => new ClipboardPayload.ImagePayload(new ImageMetadata(
                    reader.GetInt32(5),
                    reader.GetInt32(6),
                    reader.GetString(7))),
                "files" => new ClipboardPayload.FilesPayload(
                    JsonSerializer.Deserialize<List<FileReference>>(reader.GetString(8)) ?? []),
                _ => throw new InvalidDataException("Corrupt record.")
            };

            item = new ClipboardItem(id, payload, fingerprint, source, createdAt, lastUsedAt);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetRetentionDuration(HistoryRetention retention, out TimeSpan duration)
    {
        duration = retention switch
        {
            HistoryRetention.OneDay => TimeSpan.FromDays(1),
            HistoryRetention.SevenDays => TimeSpan.FromDays(7),
            HistoryRetention.OneMonth => TimeSpan.FromDays(30),
            HistoryRetention.OneYear => TimeSpan.FromDays(365),
            HistoryRetention.Forever => TimeSpan.Zero,
            _ => TimeSpan.Zero
        };

        return retention != HistoryRetention.Forever;
    }
}
