using WPaste.Clipboard.Parsing;
using WPaste.Core.Domain;

namespace WPaste.Clipboard.Privacy;

public interface IPrivacyFilter
{
    PrivacyDecision Decide(ParsedClipboard candidate, AppSettings settings);
}

public sealed class PrivacyFilter : IPrivacyFilter
{
    private static readonly string[] PasswordManagerExeNames =
    [
        "1password.exe",
        "bitwarden.exe",
        "lastpass.exe",
        "dashlane.exe",
        "keepass.exe",
        "keepassxc.exe"
    ];

    private static readonly HashSet<string> SensitiveFormatNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ExcludeClipboardContentFromMonitorProcessing",
        "Clipboard Viewer Ignore"
    };

    private static readonly HashSet<string> TransientFormatNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CanIncludeInClipboardHistory",
        "CanUploadToCloudClipboard"
    };

    public PrivacyDecision Decide(ParsedClipboard candidate, AppSettings settings)
    {
        if (settings.RecordingPaused)
        {
            return PrivacyDecision.Reject(PrivacyRejectionReason.RecordingPaused);
        }

        if (IsIgnoredApplication(candidate.Source, settings))
        {
            return PrivacyDecision.Reject(PrivacyRejectionReason.IgnoredApplication);
        }

        if (IsPasswordManager(candidate.Source))
        {
            return PrivacyDecision.Reject(PrivacyRejectionReason.PasswordManager);
        }

        if (settings.IgnoreSensitiveContent &&
            candidate.DeclaredFormats.Overlaps(SensitiveFormatNames))
        {
            return PrivacyDecision.Reject(PrivacyRejectionReason.SensitiveContent);
        }

        if (settings.IgnoreTransientContent &&
            candidate.DeclaredFormats.Overlaps(TransientFormatNames))
        {
            return PrivacyDecision.Reject(PrivacyRejectionReason.TransientContent);
        }

        return PrivacyDecision.Allow;
    }

    private static bool IsIgnoredApplication(ClipboardSource source, AppSettings settings)
    {
        if (settings.IgnoredExeNames.Count == 0)
        {
            return false;
        }

        var exeName = GetExeFileName(source);
        return settings.IgnoredExeNames.Contains(exeName, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsPasswordManager(ClipboardSource source)
    {
        var exeName = GetExeFileName(source);
        return PasswordManagerExeNames.Contains(exeName, StringComparer.OrdinalIgnoreCase);
    }

    private static string GetExeFileName(ClipboardSource source)
    {
        if (!string.IsNullOrWhiteSpace(source.ExePath))
        {
            return Path.GetFileName(source.ExePath);
        }

        return source.ProcessName;
    }
}
