using WPaste.Platform.Clipboard;

namespace WPaste.Clipboard.Privacy;

/// <summary>
/// 枚举 Windows 剪贴板 opt-out 格式（AA4 / SP3）。
/// </summary>
public interface IClipboardOptOutProbe
{
    bool HasSensitiveOptOut();

    bool HasTransientOptOut();

    IReadOnlySet<string> EnumerateFormatNames();
}

public sealed class ClipboardOptOutProbe : IClipboardOptOutProbe
{
    private const string ExcludeFromMonitorProcessing = "ExcludeClipboardContentFromMonitorProcessing";
    private const string ClipboardViewerIgnore = "Clipboard Viewer Ignore";
    private const string CanIncludeInClipboardHistory = "CanIncludeInClipboardHistory";
    private const string CanUploadToCloudClipboard = "CanUploadToCloudClipboard";

    public bool HasSensitiveOptOut()
    {
        if (!ClipboardInterop.TryOpenClipboard())
        {
            return false;
        }

        try
        {
            var ids = ClipboardInterop.EnumerateFormatIds();
            var excludeId = ClipboardInterop.RegisterFormat(ExcludeFromMonitorProcessing);
            var viewerIgnoreId = ClipboardInterop.RegisterFormat(ClipboardViewerIgnore);
            return ids.Contains(excludeId) || ids.Contains(viewerIgnoreId);
        }
        finally
        {
            ClipboardInterop.CloseClipboard();
        }
    }

    public bool HasTransientOptOut()
    {
        if (!ClipboardInterop.TryOpenClipboard())
        {
            return false;
        }

        try
        {
            var ids = ClipboardInterop.EnumerateFormatIds();
            return IsOptOutFlag(ids, CanIncludeInClipboardHistory) ||
                   IsOptOutFlag(ids, CanUploadToCloudClipboard);
        }
        finally
        {
            ClipboardInterop.CloseClipboard();
        }
    }

    public IReadOnlySet<string> EnumerateFormatNames()
    {
        if (!ClipboardInterop.TryOpenClipboard())
        {
            return new HashSet<string>();
        }

        try
        {
            return ClipboardInterop.EnumerateFormatNames();
        }
        finally
        {
            ClipboardInterop.CloseClipboard();
        }
    }

    private static bool IsOptOutFlag(HashSet<uint> ids, string formatName)
    {
        var formatId = ClipboardInterop.RegisterFormat(formatName);
        return ids.Contains(formatId) && ClipboardInterop.TryReadOptOutFlag(formatId);
    }
}
