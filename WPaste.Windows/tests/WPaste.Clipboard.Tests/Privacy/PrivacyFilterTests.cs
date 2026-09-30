using WPaste.Clipboard.Parsing;
using WPaste.Clipboard.Privacy;
using WPaste.Core.Domain;

namespace WPaste.Clipboard.Tests.Privacy;

public class PrivacyFilterTests
{
    private static readonly ClipboardPayload TextPayload = new ClipboardPayload.TextPayload("secret");

    [Fact]
    public void PauseRejectsEveryCapture()
    {
        var settings = new AppSettings { RecordingPaused = true };
        var candidate = CreateCandidate(new ClipboardSource("文本编辑", @"C:\Windows\System32\notepad.exe"));

        Assert.Equal(
            PrivacyRejectionReason.RecordingPaused,
            new PrivacyFilter().Decide(candidate, settings).Reason);
    }

    [Fact]
    public void IgnoredAppsAndPasswordManagersAreRejected()
    {
        var settings = new AppSettings();
        settings.IgnoredExeNames.Add("private.exe");

        var ignored = CreateCandidate(new ClipboardSource("Private", @"C:\Apps\private.exe"));
        var passwordManager = CreateCandidate(new ClipboardSource("1Password", @"C:\Apps\1Password.exe"));

        Assert.Equal(
            PrivacyRejectionReason.IgnoredApplication,
            new PrivacyFilter().Decide(ignored, settings).Reason);
        Assert.Equal(
            PrivacyRejectionReason.PasswordManager,
            new PrivacyFilter().Decide(passwordManager, settings).Reason);
    }

    [Fact]
    public void ConfidentialAndTransientTypesRespectSettings()
    {
        var confidential = CreateCandidate(
            new ClipboardSource("未知应用", null),
            declaredFormats: new HashSet<string> { "ExcludeClipboardContentFromMonitorProcessing" });
        var transient = CreateCandidate(
            new ClipboardSource("未知应用", null),
            declaredFormats: new HashSet<string> { "CanIncludeInClipboardHistory" });

        Assert.Equal(
            PrivacyRejectionReason.SensitiveContent,
            new PrivacyFilter().Decide(confidential, new AppSettings()).Reason);
        Assert.Equal(
            PrivacyRejectionReason.TransientContent,
            new PrivacyFilter().Decide(transient, new AppSettings()).Reason);
    }

    [Fact]
    public void OrdinaryContentIsAllowed()
    {
        var candidate = CreateCandidate(new ClipboardSource("文本编辑", @"C:\Windows\System32\notepad.exe"));
        Assert.Equal(PrivacyDecisionKind.Allow, new PrivacyFilter().Decide(candidate, new AppSettings()).Kind);
    }

    private static ParsedClipboard CreateCandidate(
        ClipboardSource source,
        IReadOnlySet<string>? declaredFormats = null) =>
        new(TextPayload, source, declaredFormats ?? new HashSet<string>());
}
