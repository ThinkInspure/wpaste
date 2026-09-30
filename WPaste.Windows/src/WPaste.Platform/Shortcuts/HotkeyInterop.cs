using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WPaste.Platform.Shortcuts;

public static class HotkeyInterop
{
    private const int PrimaryHotkeyId = 1;
    private const int FallbackHotkeyId = 2;

    public static bool RegisterHistoryHotkeys(nint windowHandle)
    {
        var hwnd = new HWND(windowHandle);
        if (PInvoke.RegisterHotKey(
                hwnd,
                PrimaryHotkeyId,
                HOT_KEY_MODIFIERS.MOD_CONTROL | HOT_KEY_MODIFIERS.MOD_SHIFT,
                (uint)VIRTUAL_KEY.VK_V))
        {
            return true;
        }

        return PInvoke.RegisterHotKey(
            hwnd,
            FallbackHotkeyId,
            HOT_KEY_MODIFIERS.MOD_CONTROL | HOT_KEY_MODIFIERS.MOD_SHIFT,
            (uint)VIRTUAL_KEY.VK_INSERT);
    }

    public static void UnregisterHistoryHotkeys(nint windowHandle)
    {
        var hwnd = new HWND(windowHandle);
        PInvoke.UnregisterHotKey(hwnd, PrimaryHotkeyId);
        PInvoke.UnregisterHotKey(hwnd, FallbackHotkeyId);
    }

    public static bool IsHistoryHotkey(int hotkeyId) =>
        hotkeyId is PrimaryHotkeyId or FallbackHotkeyId;
}
