namespace WPaste.Core.Domain;

public enum HistoryRetention
{
    OneDay,
    SevenDays,
    OneMonth,
    OneYear,
    Forever
}

public enum DefaultPasteBehavior
{
    Automatic,
    CopyOnly
}

public sealed class AppSettings
{
    public bool LaunchAtLogin { get; set; }
    public bool SoundEnabled { get; set; } = true;
    public DefaultPasteBehavior DefaultPasteBehavior { get; set; } = DefaultPasteBehavior.Automatic;
    public bool DefaultPlainText { get; set; }
    public HistoryRetention Retention { get; set; } = HistoryRetention.SevenDays;
    public bool HideDuringScreenSharing { get; set; } = true;
    public bool LinkPreviewsEnabled { get; set; } = true;
    public bool IgnoreSensitiveContent { get; set; } = true;
    public bool IgnoreTransientContent { get; set; } = true;
    public HashSet<string> IgnoredExeNames { get; set; } = [];
    public bool ClearHistoryOnQuit { get; set; }
    public bool RecordingPaused { get; set; }
    public bool HasCompletedOnboarding { get; set; }

    public static AppSettings Default { get; } = new();
}
