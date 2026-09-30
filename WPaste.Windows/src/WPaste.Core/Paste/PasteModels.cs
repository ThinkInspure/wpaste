namespace WPaste.Core.Paste;

public enum PasteMode
{
    Automatic,
    CopyOnly
}

public enum PasteFallbackReason
{
    InputSimulationFailed,
    TargetUnavailable,
    ActivationFailed
}

public enum PasteResultKind
{
    Pasted,
    CopiedOnly,
    Copied,
    Unavailable
}

public sealed record PasteResult(PasteResultKind Kind, PasteFallbackReason? FallbackReason = null);

public interface IForegroundTarget
{
    bool IsRunning { get; }

    bool Activate();
}
