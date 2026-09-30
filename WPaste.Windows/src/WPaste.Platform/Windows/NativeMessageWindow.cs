using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WPaste.Platform.Windows;

public sealed class NativeMessageWindow : IDisposable
{
    public const uint WmClipboardUpdate = 0x031D;
    public const uint WmHotkey = 0x0312;

    private readonly WNDPROC _wndProc;
    private HWND _hwnd;
    private bool _disposed;
    private string? _className;

    public NativeMessageWindow()
    {
        _wndProc = WindowProc;
    }

    public nint Handle => _hwnd.Value;

    public event Action<uint, nint, nint>? MessageReceived;

    public void Create()
    {
        if (_hwnd != HWND.Null)
        {
            return;
        }

        unsafe
        {
            _className = $"WPasteMsg_{Guid.NewGuid():N}";
            fixed (char* classNamePointer = _className)
            {
                var instance = PInvoke.GetModuleHandle(default(PCWSTR));
                var classInfo = new WNDCLASSEXW
                {
                    cbSize = (uint)sizeof(WNDCLASSEXW),
                    lpfnWndProc = _wndProc,
                    hInstance = instance,
                    lpszClassName = classNamePointer
                };

                PInvoke.RegisterClassEx(in classInfo);
                _hwnd = PInvoke.CreateWindowEx(
                    WINDOW_EX_STYLE.WS_EX_TOOLWINDOW,
                    classNamePointer,
                    default,
                    WINDOW_STYLE.WS_OVERLAPPED,
                    0,
                    0,
                    0,
                    0,
                    new HWND(-3),
                    default,
                    instance,
                    null);
            }
        }

        if (_hwnd == HWND.Null)
        {
            throw new InvalidOperationException("Failed to create message window.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_hwnd != HWND.Null)
        {
            PInvoke.DestroyWindow(_hwnd);
            _hwnd = HWND.Null;
        }
    }

    private LRESULT WindowProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        MessageReceived?.Invoke(msg, (nint)wParam.Value, (nint)lParam.Value);
        return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
    }
}
