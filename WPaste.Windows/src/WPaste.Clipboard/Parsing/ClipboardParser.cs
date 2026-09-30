using WPaste.Core.Domain;

namespace WPaste.Clipboard.Parsing;

public interface IClipboardParser
{
    ParsedClipboard? Parse(ClipboardSnapshot snapshot);
}

public sealed class ClipboardParser : IClipboardParser
{
    public ParsedClipboard? Parse(ClipboardSnapshot snapshot)
    {
        if (snapshot.FilePaths.Count > 0)
        {
            var files = snapshot.FilePaths
                .Select(path => new FileReference(path, Path.GetFileName(path)))
                .ToList();
            return new ParsedClipboard(
                new ClipboardPayload.FilesPayload(files),
                snapshot.Source,
                snapshot.DeclaredFormats);
        }

        if (snapshot.ImageData is { Length: > 0 } imageData && snapshot.ImageSize is { } size)
        {
            return new ParsedClipboard(
                new ClipboardPayload.ImagePayload(new ImageMetadata(size.Width, size.Height, string.Empty)),
                snapshot.Source,
                snapshot.DeclaredFormats,
                imageData);
        }

        if (snapshot.UrlString is { } rawUrl &&
            TryNormalizeUrl(rawUrl, out var url))
        {
            return new ParsedClipboard(
                new ClipboardPayload.UrlPayload(url),
                snapshot.Source,
                snapshot.DeclaredFormats);
        }

        if (snapshot.Text is { Length: > 0 } text)
        {
            return new ParsedClipboard(
                new ClipboardPayload.TextPayload(text),
                snapshot.Source,
                snapshot.DeclaredFormats);
        }

        return null;
    }

    internal static bool TryNormalizeUrl(string rawValue, out Uri url)
    {
        url = null!;
        var value = rawValue.Trim();
        if (!Uri.TryCreate(value, UriKind.Absolute, out var candidate))
        {
            return false;
        }

        var scheme = candidate.Scheme.ToLowerInvariant();
        if (scheme is not "http" and not "https" || string.IsNullOrEmpty(candidate.Host))
        {
            return false;
        }

        var builder = new UriBuilder(candidate)
        {
            Scheme = scheme,
            Host = candidate.Host.ToLowerInvariant()
        };
        url = builder.Uri;
        return true;
    }
}
