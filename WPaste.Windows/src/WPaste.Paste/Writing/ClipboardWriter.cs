using WPaste.Core.Domain;
using WPaste.Core.Paths;
using WPaste.Platform.Clipboard;

namespace WPaste.Paste.Writing;

public interface IClipboardWriter
{
    bool Write(ClipboardPayload payload, bool plainText);
}

public sealed class ClipboardWriter : IClipboardWriter
{
    public bool Write(ClipboardPayload payload, bool plainText)
    {
        if (!ClipboardInterop.TryOpenClipboard())
        {
            return false;
        }

        try
        {
            ClipboardInterop.EmptyClipboard();
            return plainText
                ? ClipboardInterop.SetUnicodeText(PlainTextRepresentation(payload))
                : WritePayload(payload);
        }
        finally
        {
            ClipboardInterop.CloseClipboard();
        }
    }

    private static bool WritePayload(ClipboardPayload payload) =>
        payload switch
        {
            ClipboardPayload.TextPayload text => ClipboardInterop.SetUnicodeText(text.Text),
            ClipboardPayload.UrlPayload url => ClipboardInterop.SetUnicodeText(url.Url.AbsoluteUri),
            ClipboardPayload.ImagePayload image => WriteImage(image.Metadata),
            ClipboardPayload.FilesPayload files => WriteFiles(files.Files),
            _ => false
        };

    private static bool WriteImage(ImageMetadata metadata)
    {
        var imagePath = Path.Combine(AppPaths.ImagesDirectory, metadata.RelativePath);
        if (!File.Exists(imagePath))
        {
            return false;
        }

        var bytes = File.ReadAllBytes(imagePath);
        return ClipboardInterop.SetClipboardBytes(8, bytes);
    }

    private static bool WriteFiles(IReadOnlyList<FileReference> files)
    {
        if (files.Count == 0 || files.Any(file => !File.Exists(file.Path)))
        {
            return false;
        }

        return ClipboardInterop.SetFileDrop(files.Select(file => file.Path));
    }

    internal static string PlainTextRepresentation(ClipboardPayload payload) =>
        payload switch
        {
            ClipboardPayload.TextPayload text => text.Text,
            ClipboardPayload.UrlPayload url => url.Url.AbsoluteUri,
            ClipboardPayload.ImagePayload image =>
                $"图片 {image.Metadata.Width} × {image.Metadata.Height}",
            ClipboardPayload.FilesPayload files =>
                string.Join('\n', files.Files.Select(file => file.Path)),
            _ => string.Empty
        };
}
