using WPaste.Core.Domain;
using WPaste.Core.Paste;
using WPaste.Paste.Writing;
using WPaste.Platform.Input;
using WPaste.Platform.Windows;

namespace WPaste.Paste.Coordination;

public interface IPasteCoordinator
{
    PasteResult Paste(
        ClipboardItem item,
        PasteMode mode,
        IForegroundTarget? target,
        bool plainText,
        Action? onPasteHintRequired = null,
        bool closeOverlayAfterWrite = true);
}

public sealed class PasteCoordinator : IPasteCoordinator
{
    private bool _hasShownPasteHintThisSession;

    private readonly IClipboardWriter _writer;
    private readonly IKeyboardInputClient _keyboard;
    private readonly Action _closeOverlay;
    private readonly Action<string, DateTimeOffset> _suppressWrite;

    public PasteCoordinator(
        IClipboardWriter writer,
        IKeyboardInputClient keyboard,
        Action closeOverlay,
        Action<string, DateTimeOffset> suppressWrite)
    {
        _writer = writer;
        _keyboard = keyboard;
        _closeOverlay = closeOverlay;
        _suppressWrite = suppressWrite;
    }

    public PasteResult Paste(
        ClipboardItem item,
        PasteMode mode,
        IForegroundTarget? target,
        bool plainText,
        Action? onPasteHintRequired = null,
        bool closeOverlayAfterWrite = true)
    {
        _suppressWrite(item.Fingerprint, DateTimeOffset.UtcNow.AddSeconds(2));

        if (!_writer.Write(item.Payload, plainText))
        {
            return new PasteResult(PasteResultKind.Unavailable);
        }

        if (closeOverlayAfterWrite)
        {
            _closeOverlay();
        }

        if (mode == PasteMode.CopyOnly)
        {
            return new PasteResult(PasteResultKind.Copied);
        }

        if (target is not { IsRunning: true })
        {
            return new PasteResult(PasteResultKind.CopiedOnly, PasteFallbackReason.TargetUnavailable);
        }

        if (!target.Activate())
        {
            return new PasteResult(PasteResultKind.CopiedOnly, PasteFallbackReason.ActivationFailed);
        }

        if (!_keyboard.SendPasteChord())
        {
            if (!_hasShownPasteHintThisSession)
            {
                _hasShownPasteHintThisSession = true;
                onPasteHintRequired?.Invoke();
            }

            return new PasteResult(
                PasteResultKind.CopiedOnly,
                PasteFallbackReason.InputSimulationFailed);
        }

        return new PasteResult(PasteResultKind.Pasted);
    }
}
