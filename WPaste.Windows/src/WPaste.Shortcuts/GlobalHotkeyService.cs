using WPaste.Platform.Shortcuts;
using WPaste.Platform.Windows;

namespace WPaste.Shortcuts;

public interface IGlobalHotkeyService
{
    event EventHandler? HistoryHotkeyPressed;

    bool TryRegister(NativeMessageWindow messageWindow);

    void Unregister();
}

public sealed class GlobalHotkeyService : IGlobalHotkeyService
{
    public event EventHandler? HistoryHotkeyPressed;

    private NativeMessageWindow? _messageWindow;

    public bool TryRegister(NativeMessageWindow messageWindow)
    {
        _messageWindow = messageWindow;
        _messageWindow.MessageReceived += OnWindowMessage;
        return HotkeyInterop.RegisterHistoryHotkeys(messageWindow.Handle);
    }

    public void Unregister()
    {
        if (_messageWindow is null)
        {
            return;
        }

        _messageWindow.MessageReceived -= OnWindowMessage;
        HotkeyInterop.UnregisterHistoryHotkeys(_messageWindow.Handle);
        _messageWindow = null;
    }

    private void OnWindowMessage(uint message, nint wParam, nint lParam)
    {
        if (message != NativeMessageWindow.WmHotkey)
        {
            return;
        }

        if (HotkeyInterop.IsHistoryHotkey((int)wParam))
        {
            HistoryHotkeyPressed?.Invoke(this, EventArgs.Empty);
        }
    }
}
