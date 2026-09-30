using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WPaste.Platform.Clipboard;

public static class ClipboardInterop
{
    public const uint CfUnicodeText = 13;
    public const uint CfHDrop = 15;

    public static bool TryOpenClipboard()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (PInvoke.OpenClipboard(default))
            {
                return true;
            }

            Thread.Sleep(10);
        }

        return false;
    }

    public static void CloseClipboard() => PInvoke.CloseClipboard();

    public static bool EmptyClipboard() => PInvoke.EmptyClipboard();

    public static IReadOnlySet<string> EnumerateFormatNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Span<char> buffer = stackalloc char[256];
        uint format = 0;
        while ((format = PInvoke.EnumClipboardFormats(format)) != 0)
        {
            unsafe
            {
                fixed (char* bufferPointer = buffer)
                {
                    var length = PInvoke.GetClipboardFormatName(format, bufferPointer, 256);
                    if (length > 0)
                    {
                        names.Add(new string(bufferPointer, 0, length));
                    }
                }
            }
        }

        return names;
    }

    public static HashSet<uint> EnumerateFormatIds()
    {
        var ids = new HashSet<uint>();
        uint format = 0;
        while ((format = PInvoke.EnumClipboardFormats(format)) != 0)
        {
            ids.Add(format);
        }

        return ids;
    }

    public static uint RegisterFormat(string name) => PInvoke.RegisterClipboardFormat(name);

    public static bool TryReadOptOutFlag(uint formatId)
    {
        unsafe
        {
            var handle = PInvoke.GetClipboardData(formatId);
            if (handle == HANDLE.Null)
            {
                return false;
            }

            var global = new HGLOBAL((void*)handle.Value);
            var size = PInvoke.GlobalSize(global);
            if (size < (nuint)sizeof(uint))
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
                return *(uint*)locked == 0;
            }
            finally
            {
                PInvoke.GlobalUnlock(global);
            }
        }
    }

    public static bool SetUnicodeText(string value)
    {
        var bytes = Encoding.Unicode.GetBytes(value + '\0');
        return SetClipboardBytes(CfUnicodeText, bytes);
    }

    public static bool SetFileDrop(IEnumerable<string> paths)
    {
        var dropFiles = BuildDropFilesStructure(paths);
        return SetClipboardBytes(CfHDrop, dropFiles);
    }

    public static bool SetClipboardBytes(uint format, ReadOnlySpan<byte> bytes)
    {
        unsafe
        {
            var global = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, (nuint)bytes.Length);
            if (global.Value == null)
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
                bytes.CopyTo(new Span<byte>(locked, bytes.Length));
            }
            finally
            {
                PInvoke.GlobalUnlock(global);
            }

            return PInvoke.SetClipboardData(format, new HANDLE((nint)global.Value)) != HANDLE.Null;
        }
    }

    private static byte[] BuildDropFilesStructure(IEnumerable<string> paths)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(20);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(1);

        foreach (var path in paths)
        {
            writer.Write(Encoding.Unicode.GetBytes(path + '\0'));
        }

        writer.Write((ushort)0);
        return stream.ToArray();
    }
}
