using WPaste.Core.Domain;
using WPaste.Core.Paste;
using WPaste.Paste.Coordination;
using WPaste.Paste.Writing;
using WPaste.Platform.Input;

namespace WPaste.Paste.Tests.Coordination;

public class PasteCoordinatorTests
{
    private static readonly ClipboardSource Source = new("文本编辑", @"C:\Windows\System32\notepad.exe");

    [Fact]
    public void AutomaticPasteWritesClosesActivatesAndSendsInOrder()
    {
        var events = new List<string>();
        var writer = new RecordingClipboardWriter(events);
        var keyboard = new RecordingKeyboardInputClient(events);
        var target = new RecordingForegroundTarget(isRunning: true, events);
        var coordinator = CreateCoordinator(events, writer, keyboard);

        var item = ClipboardItem.Create(
            new ClipboardPayload.TextPayload("hello"),
            "hello",
            Source);

        var result = coordinator.Paste(item, PasteMode.Automatic, target, plainText: false);

        Assert.Equal(PasteResultKind.Pasted, result.Kind);
        Assert.Equal(["suppress", "write", "close", "activate", "paste"], events);
    }

    [Fact]
    public void FailedPasteSimulationFallsBackToCopyOnlyOncePerSession()
    {
        var events = new List<string>();
        var hints = 0;
        var writer = new RecordingClipboardWriter(events);
        var keyboard = new RecordingKeyboardInputClient(events, succeed: false);
        var coordinator = CreateCoordinator(events, writer, keyboard);
        var item = ClipboardItem.Create(
            new ClipboardPayload.TextPayload("hello"),
            "hello",
            Source);
        var target = new RecordingForegroundTarget(isRunning: true, events);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var result = coordinator.Paste(
                item,
                PasteMode.Automatic,
                target,
                plainText: false,
                onPasteHintRequired: () => hints++);

            Assert.Equal(PasteResultKind.CopiedOnly, result.Kind);
            Assert.Equal(PasteFallbackReason.InputSimulationFailed, result.FallbackReason);
        }

        Assert.Equal(1, hints);
        Assert.Equal(3, events.Count(static value => value == "write"));
        Assert.DoesNotContain("paste", events);
    }

    [Fact]
    public void CopyOnlyModeSkipsActivationAndPaste()
    {
        var events = new List<string>();
        var coordinator = CreateCoordinator(
            events,
            new RecordingClipboardWriter(events),
            new RecordingKeyboardInputClient(events));
        var item = ClipboardItem.Create(
            new ClipboardPayload.UrlPayload(new Uri("https://example.com")),
            "url",
            Source);

        var result = coordinator.Paste(
            item,
            PasteMode.CopyOnly,
            new RecordingForegroundTarget(isRunning: true, events),
            plainText: true);

        Assert.Equal(PasteResultKind.Copied, result.Kind);
        Assert.Equal(["suppress", "write", "close"], events);
    }

    [Fact]
    public void CopyOnlyCanKeepOverlayOpen()
    {
        var events = new List<string>();
        var coordinator = CreateCoordinator(
            events,
            new RecordingClipboardWriter(events),
            new RecordingKeyboardInputClient(events));
        var item = ClipboardItem.Create(
            new ClipboardPayload.TextPayload("hello"),
            "hello",
            Source);

        var result = coordinator.Paste(
            item,
            PasteMode.CopyOnly,
            target: null,
            plainText: false,
            closeOverlayAfterWrite: false);

        Assert.Equal(PasteResultKind.Copied, result.Kind);
        Assert.Equal(["suppress", "write"], events);
    }

    [Fact]
    public void ExitedTargetLeavesContentCopied()
    {
        var events = new List<string>();
        var coordinator = CreateCoordinator(
            events,
            new RecordingClipboardWriter(events),
            new RecordingKeyboardInputClient(events));
        var item = ClipboardItem.Create(
            new ClipboardPayload.TextPayload("hello"),
            "hello",
            Source);

        var result = coordinator.Paste(
            item,
            PasteMode.Automatic,
            new RecordingForegroundTarget(isRunning: false, events),
            plainText: false);

        Assert.Equal(PasteResultKind.CopiedOnly, result.Kind);
        Assert.Equal(PasteFallbackReason.TargetUnavailable, result.FallbackReason);
    }

    private static PasteCoordinator CreateCoordinator(
        List<string> events,
        IClipboardWriter writer,
        IKeyboardInputClient keyboard) =>
        new(
            writer,
            keyboard,
            closeOverlay: () => events.Add("close"),
            suppressWrite: (_, _) => events.Add("suppress"));

    private sealed class RecordingClipboardWriter(List<string> events) : IClipboardWriter
    {
        public bool Write(ClipboardPayload payload, bool plainText)
        {
            events.Add("write");
            return true;
        }
    }

    private sealed class RecordingKeyboardInputClient(List<string> events, bool succeed = true) : IKeyboardInputClient
    {
        public bool SendPasteChord()
        {
            if (succeed)
            {
                events.Add("paste");
            }

            return succeed;
        }
    }

    private sealed class RecordingForegroundTarget(bool isRunning, List<string> events) : IForegroundTarget
    {
        public bool IsRunning => isRunning;

        public bool Activate()
        {
            events.Add("activate");
            return true;
        }
    }
}
