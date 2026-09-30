using WPaste.Clipboard.Monitoring;
using WPaste.Clipboard.Parsing;
using WPaste.Clipboard.Privacy;
using WPaste.Core.Domain;
using WPaste.Core.Fingerprint;
using WPaste.Platform.Windows;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace WPaste.Clipboard.Monitoring;

public interface IClipboardMonitor
{
    event EventHandler? ClipboardChanged;

    void Start(NativeMessageWindow messageWindow);

    void Stop();

    void SuppressWrite(string fingerprint, TimeSpan duration);

    void PollOnce(DateTimeOffset? now = null);
}

public sealed class ClipboardMonitor : IClipboardMonitor
{
    public event EventHandler? ClipboardChanged;

    private readonly IClipboardParser _parser;
    private readonly IPrivacyFilter _privacyFilter;
    private readonly IClipboardOptOutProbe _optOutProbe;
    private readonly Func<AppSettings> _settings;
    private readonly Func<ClipboardSnapshot?> _snapshotProvider;
    private readonly Func<ParsedClipboard, string, Task> _onAccepted;

    private NativeMessageWindow? _messageWindow;
    private (string Fingerprint, DateTimeOffset Until)? _suppression;

    public ClipboardMonitor(
        IClipboardParser parser,
        IPrivacyFilter privacyFilter,
        IClipboardOptOutProbe optOutProbe,
        Func<AppSettings> settings,
        Func<ClipboardSnapshot?> snapshotProvider,
        Func<ParsedClipboard, string, Task> onAccepted)
    {
        _parser = parser;
        _privacyFilter = privacyFilter;
        _optOutProbe = optOutProbe;
        _settings = settings;
        _snapshotProvider = snapshotProvider;
        _onAccepted = onAccepted;
    }

    public void Start(NativeMessageWindow messageWindow)
    {
        _messageWindow = messageWindow;
        _messageWindow.MessageReceived += OnWindowMessage;
        if (!PInvoke.AddClipboardFormatListener(new HWND(messageWindow.Handle)))
        {
            throw new InvalidOperationException("AddClipboardFormatListener failed.");
        }
    }

    public void Stop()
    {
        if (_messageWindow is null)
        {
            return;
        }

        _messageWindow.MessageReceived -= OnWindowMessage;
        PInvoke.RemoveClipboardFormatListener(new HWND(_messageWindow.Handle));
        _messageWindow = null;
    }

    public void SuppressWrite(string fingerprint, TimeSpan duration)
    {
        _suppression = (fingerprint, DateTimeOffset.UtcNow.Add(duration));
    }

    public void PollOnce(DateTimeOffset? now = null)
    {
        var timestamp = now ?? DateTimeOffset.UtcNow;
        var snapshot = _snapshotProvider();
        if (snapshot is null)
        {
            return;
        }

        var declaredFormats = snapshot.DeclaredFormats
            .Union(_optOutProbe.EnumerateFormatNames())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (_optOutProbe.HasSensitiveOptOut())
        {
            declaredFormats.Add("ExcludeClipboardContentFromMonitorProcessing");
        }

        if (_optOutProbe.HasTransientOptOut())
        {
            declaredFormats.Add("CanIncludeInClipboardHistory");
        }

        snapshot = snapshot with { DeclaredFormats = declaredFormats };
        var parsed = _parser.Parse(snapshot);
        if (parsed is null)
        {
            return;
        }

        parsed = parsed with { DeclaredFormats = declaredFormats };
        if (_privacyFilter.Decide(parsed, _settings()).Kind == PrivacyDecisionKind.Reject)
        {
            return;
        }

        var fingerprint = parsed.ImageData is { Length: > 0 } imageData
            ? ContentFingerprint.MakeForImageData(imageData.Span)
            : ContentFingerprint.Make(parsed.Payload);

        if (_suppression is { } suppression &&
            suppression.Until >= timestamp &&
            suppression.Fingerprint == fingerprint)
        {
            _suppression = null;
            return;
        }

        if (_suppression is { Until: var until } && until < timestamp)
        {
            _suppression = null;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _onAccepted(parsed, fingerprint);
            }
            catch
            {
                // MVP: swallow persistence errors; surfaced via future telemetry.
            }
        });
        ClipboardChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnWindowMessage(uint message, nint wParam, nint lParam)
    {
        if (message == NativeMessageWindow.WmClipboardUpdate)
        {
            PollOnce();
        }
    }
}
