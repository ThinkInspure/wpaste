namespace WPaste.Clipboard.Privacy;

public enum PrivacyRejectionReason
{
    RecordingPaused,
    IgnoredApplication,
    PasswordManager,
    SensitiveContent,
    TransientContent
}

public enum PrivacyDecisionKind
{
    Allow,
    Reject
}

public sealed record PrivacyDecision(PrivacyDecisionKind Kind, PrivacyRejectionReason? Reason = null)
{
    public static PrivacyDecision Allow { get; } = new(PrivacyDecisionKind.Allow);

    public static PrivacyDecision Reject(PrivacyRejectionReason reason) =>
        new(PrivacyDecisionKind.Reject, reason);
}
