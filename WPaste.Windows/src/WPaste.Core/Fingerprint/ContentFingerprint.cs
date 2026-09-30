using System.Security.Cryptography;
using System.Text;
using WPaste.Core.Domain;

namespace WPaste.Core.Fingerprint;

/// <summary>
/// 与 macOS ContentFingerprint.swift 算法一致（跨平台去重关键）。
/// </summary>
public static class ContentFingerprint
{
    public static string Make(ClipboardPayload payload)
    {
        var canonical = payload switch
        {
            ClipboardPayload.TextPayload text => $"text\0{NormalizeLineEndings(text.Text)}",
            ClipboardPayload.UrlPayload url => $"url\0{url.Url.AbsoluteUri}",
            ClipboardPayload.ImagePayload image =>
                $"image\0{image.Metadata.Width}x{image.Metadata.Height}\0{image.Metadata.RelativePath}",
            ClipboardPayload.FilesPayload files =>
                $"files\0{string.Join('\0', files.Files.Select(static f => f.Path))}",
            _ => throw new ArgumentOutOfRangeException(nameof(payload))
        };

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    public static string MakeForImageData(ReadOnlySpan<byte> data) =>
        Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
