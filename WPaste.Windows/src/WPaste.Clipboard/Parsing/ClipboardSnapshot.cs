using WPaste.Core.Domain;

namespace WPaste.Clipboard.Parsing;

public sealed record PixelSize(int Width, int Height);

public sealed record ClipboardSnapshot(
    IReadOnlyList<string> FilePaths,
    ReadOnlyMemory<byte>? ImageData,
    PixelSize? ImageSize,
    string? UrlString,
    string? Text,
    IReadOnlySet<string> DeclaredFormats,
    ClipboardSource Source)
{
    public ClipboardSnapshot(
        ClipboardSource source,
        IReadOnlyList<string>? filePaths = null,
        ReadOnlyMemory<byte>? imageData = null,
        PixelSize? imageSize = null,
        string? urlString = null,
        string? text = null,
        IReadOnlySet<string>? declaredFormats = null)
        : this(
            filePaths ?? [],
            imageData,
            imageSize,
            urlString,
            text,
            declaredFormats ?? new HashSet<string>(),
            source)
    {
    }
}

public sealed record ParsedClipboard(
    ClipboardPayload Payload,
    ClipboardSource Source,
    IReadOnlySet<string> DeclaredFormats,
    ReadOnlyMemory<byte>? ImageData = null);
