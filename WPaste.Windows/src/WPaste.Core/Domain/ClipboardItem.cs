namespace WPaste.Core.Domain;

public enum ClipboardKind
{
    Text,
    Url,
    Image,
    Files
}

public sealed record ImageMetadata(int Width, int Height, string RelativePath);

public sealed record FileReference(string Path, string DisplayName);

public abstract record ClipboardPayload
{
    public sealed record TextPayload(string Text) : ClipboardPayload;

    public sealed record UrlPayload(Uri Url) : ClipboardPayload;

    public sealed record ImagePayload(ImageMetadata Metadata) : ClipboardPayload;

    public sealed record FilesPayload(IReadOnlyList<FileReference> Files) : ClipboardPayload;
}

public sealed record ClipboardSource(string ProcessName, string? ExePath);

public sealed record ClipboardItem(
    Guid Id,
    ClipboardPayload Payload,
    string Fingerprint,
    ClipboardSource Source,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUsedAt)
{
    public static ClipboardItem Create(
        ClipboardPayload payload,
        string fingerprint,
        ClipboardSource source,
        DateTimeOffset? createdAt = null)
    {
        var now = createdAt ?? DateTimeOffset.UtcNow;
        return new ClipboardItem(Guid.NewGuid(), payload, fingerprint, source, now, now);
    }
}
