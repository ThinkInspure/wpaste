using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using WPaste.Clipboard.Parsing;
using WPaste.Core.Domain;
using WPaste.Platform.Clipboard;

namespace WPaste.Clipboard.Monitoring;

public static class ClipboardSnapshotReader
{
    public static ClipboardSnapshot? TryReadSnapshot()
    {
        if (!ClipboardInterop.TryOpenClipboard())
        {
            return null;
        }

        try
        {
            var declaredFormats = ClipboardInterop.EnumerateFormatNames();
            var source = ReadSource();
            var filePaths = ReadFilePaths();
            if (filePaths.Count > 0)
            {
                return new ClipboardSnapshot(filePaths, null, null, null, null, declaredFormats, source);
            }

            if (TryReadImage(out var imageData, out var imageSize))
            {
                return new ClipboardSnapshot([], imageData, imageSize, null, null, declaredFormats, source);
            }

            var text = ReadUnicodeText();
            if (!string.IsNullOrWhiteSpace(text) && ClipboardParser.TryNormalizeUrl(text, out _))
            {
                return new ClipboardSnapshot([], null, null, text, text, declaredFormats, source);
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                return new ClipboardSnapshot([], null, null, null, text, declaredFormats, source);
            }

            return null;
        }
        finally
        {
            ClipboardInterop.CloseClipboard();
        }
    }

    private static ClipboardSource ReadSource()
    {
        unsafe
        {
            var hwnd = PInvoke.GetForegroundWindow();
            if (hwnd == HWND.Null)
            {
                return new ClipboardSource("未知应用", null);
            }

            uint processId;
            _ = PInvoke.GetWindowThreadProcessId(hwnd, &processId);
            try
            {
                using var process = Process.GetProcessById((int)processId);
                return new ClipboardSource(process.ProcessName, process.MainModule?.FileName);
            }
            catch
            {
                return new ClipboardSource("未知应用", null);
            }
        }
    }

    private static List<string> ReadFilePaths()
    {
        var paths = new List<string>();
        unsafe
        {
            var handle = PInvoke.GetClipboardData(ClipboardInterop.CfHDrop);
            if (handle == HANDLE.Null)
            {
                return paths;
            }

            var hDrop = new HDROP(handle.Value);
            var count = PInvoke.DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
            Span<char> buffer = stackalloc char[1024];
            for (uint index = 0; index < count; index++)
            {
                fixed (char* bufferPointer = buffer)
                {
                    var length = PInvoke.DragQueryFile(hDrop, index, bufferPointer, (uint)buffer.Length);
                    if (length > 0)
                    {
                        paths.Add(new string(bufferPointer, 0, (int)length));
                    }
                }
            }
        }

        return paths;
    }

    private static string? ReadUnicodeText()
    {
        unsafe
        {
            var handle = PInvoke.GetClipboardData(ClipboardInterop.CfUnicodeText);
            if (handle == HANDLE.Null)
            {
                return null;
            }

            var global = new HGLOBAL((void*)handle.Value);
            var locked = PInvoke.GlobalLock(global);
            if (locked == null)
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringUni((IntPtr)locked);
            }
            finally
            {
                PInvoke.GlobalUnlock(global);
            }
        }
    }

    private static bool TryReadImage(out ReadOnlyMemory<byte> imageData, out PixelSize size)
    {
        imageData = default;
        size = new PixelSize(0, 0);

        var pngFormat = ClipboardInterop.RegisterFormat("PNG");
        unsafe
        {
            var pngHandle = PInvoke.GetClipboardData(pngFormat);
            if (pngHandle != HANDLE.Null && TryReadGlobalBytes(pngHandle, out imageData, out _))
            {
                size = new PixelSize(0, 0);
                return true;
            }

            var dibHandle = PInvoke.GetClipboardData(8);
            if (dibHandle != HANDLE.Null && TryReadGlobalBytes(dibHandle, out imageData, out var bytes))
            {
                if (bytes.Length >= 16)
                {
                    var width = BitConverter.ToInt32(bytes, 4);
                    var height = Math.Abs(BitConverter.ToInt32(bytes, 8));
                    size = new PixelSize(width, height);
                }

                return true;
            }
        }

        return false;
    }

    private static unsafe bool TryReadGlobalBytes(HANDLE handle, out ReadOnlyMemory<byte> imageData, out byte[] bytes)
    {
        imageData = default;
        bytes = [];
        var global = new HGLOBAL((void*)handle.Value);
        var byteCount = (int)PInvoke.GlobalSize(global);
        if (byteCount <= 0)
        {
            return false;
        }

        var locked = PInvoke.GlobalLock(global);
        if (locked == null)
        {
            return false;
        }

        try
        {
            bytes = new byte[byteCount];
            Marshal.Copy((IntPtr)locked, bytes, 0, byteCount);
            imageData = bytes;
            return true;
        }
        finally
        {
            PInvoke.GlobalUnlock(global);
        }
    }
}
