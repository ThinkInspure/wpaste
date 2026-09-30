using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WPaste.Core.Paste;

namespace WPaste.Platform.Windows;

public sealed record ForegroundTarget(nint WindowHandle, uint ProcessId);

public interface IForegroundWindowClient
{
    IForegroundTarget? CaptureCurrent();
}

public sealed class RunningWindowTarget : IForegroundTarget
{
    private readonly ForegroundTarget _target;

    public RunningWindowTarget(ForegroundTarget target)
    {
        _target = target;
    }

    public ForegroundTarget Descriptor => _target;

    public bool IsRunning
    {
        get
        {
            try
            {
                using var process = Process.GetProcessById((int)_target.ProcessId);
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    public bool Activate()
    {
        if (_target.WindowHandle == 0)
        {
            return false;
        }

        unsafe
        {
            var foregroundWindow = PInvoke.GetForegroundWindow();
            uint foregroundProcessId;
            var foregroundThread = PInvoke.GetWindowThreadProcessId(foregroundWindow, &foregroundProcessId);

            uint targetProcessId;
            var targetThread = PInvoke.GetWindowThreadProcessId(
                new HWND(_target.WindowHandle),
                &targetProcessId);
            var currentThread = PInvoke.GetCurrentThreadId();

            var attachedForeground = false;
            var attachedTarget = false;
            try
            {
                if (foregroundThread != currentThread)
                {
                    attachedForeground = PInvoke.AttachThreadInput(currentThread, foregroundThread, true);
                }

                if (targetThread != currentThread && targetThread != foregroundThread)
                {
                    attachedTarget = PInvoke.AttachThreadInput(currentThread, targetThread, true);
                }

                return PInvoke.SetForegroundWindow(new HWND(_target.WindowHandle));
            }
            finally
            {
                if (attachedTarget)
                {
                    PInvoke.AttachThreadInput(currentThread, targetThread, false);
                }

                if (attachedForeground)
                {
                    PInvoke.AttachThreadInput(currentThread, foregroundThread, false);
                }
            }
        }
    }
}

public sealed class ForegroundWindowClient : IForegroundWindowClient
{
    public IForegroundTarget? CaptureCurrent()
    {
        unsafe
        {
            var hwnd = PInvoke.GetForegroundWindow();
            if (hwnd == HWND.Null)
            {
                return null;
            }

            uint processId;
            _ = PInvoke.GetWindowThreadProcessId(hwnd, &processId);
            return new RunningWindowTarget(new ForegroundTarget(hwnd.Value, processId));
        }
    }
}
