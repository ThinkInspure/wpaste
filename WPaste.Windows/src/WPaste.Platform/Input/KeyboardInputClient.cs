using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace WPaste.Platform.Input;

public interface IKeyboardInputClient
{
    bool SendPasteChord();
}

public sealed class KeyboardInputClient : IKeyboardInputClient
{
    public bool SendPasteChord()
    {
        Span<INPUT> inputs = stackalloc INPUT[4];
        inputs[0] = CreateKeyInput(VIRTUAL_KEY.VK_CONTROL, false);
        inputs[1] = CreateKeyInput(VIRTUAL_KEY.VK_V, false);
        inputs[2] = CreateKeyInput(VIRTUAL_KEY.VK_V, true);
        inputs[3] = CreateKeyInput(VIRTUAL_KEY.VK_CONTROL, true);

        unsafe
        {
            fixed (INPUT* inputPointer = inputs)
            {
                var sent = PInvoke.SendInput((uint)inputs.Length, inputPointer, sizeof(INPUT));
                return sent == inputs.Length;
            }
        }
    }

    private static INPUT CreateKeyInput(VIRTUAL_KEY key, bool keyUp)
    {
        return new INPUT
        {
            type = INPUT_TYPE.INPUT_KEYBOARD,
            Anonymous = new INPUT._Anonymous_e__Union
            {
                ki = new KEYBDINPUT
                {
                    wVk = key,
                    dwFlags = keyUp ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP : 0
                }
            }
        };
    }
}
