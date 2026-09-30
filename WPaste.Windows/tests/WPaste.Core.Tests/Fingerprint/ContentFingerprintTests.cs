using WPaste.Core.Domain;
using WPaste.Core.Fingerprint;

namespace WPaste.Core.Tests.Fingerprint;

public class ContentFingerprintTests
{
    [Fact]
    public void Text_NormalizesLineEndings()
    {
        var payload = new ClipboardPayload.TextPayload("a\r\nb");
        var fingerprint = ContentFingerprint.Make(payload);

        Assert.Equal(
            ContentFingerprint.Make(new ClipboardPayload.TextPayload("a\nb")),
            fingerprint);
    }

    [Fact]
    public void Files_PreservesDropOrder()
    {
        var ordered = new ClipboardPayload.FilesPayload(
        [
            new FileReference(@"C:\a.txt", "a.txt"),
            new FileReference(@"C:\b.txt", "b.txt")
        ]);

        var reversed = new ClipboardPayload.FilesPayload(
        [
            new FileReference(@"C:\b.txt", "b.txt"),
            new FileReference(@"C:\a.txt", "a.txt")
        ]);

        Assert.NotEqual(ContentFingerprint.Make(ordered), ContentFingerprint.Make(reversed));
    }
}
