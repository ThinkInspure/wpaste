using WPaste.Clipboard.Parsing;
using WPaste.Core.Domain;
using WPaste.Core.Fingerprint;

namespace WPaste.Clipboard.Tests.Parsing;

public class ClipboardParserTests
{
    private static readonly ClipboardSource UnknownSource = new("未知应用", null);

    [Fact]
    public void FilesTakePriorityOverOtherRepresentations()
    {
        var snapshot = new ClipboardSnapshot(
            source: new ClipboardSource("Finder", "C:\\Windows\\explorer.exe"),
            filePaths: [@"C:\tmp\report.pdf"],
            imageData: new byte[] { 1, 2, 3 },
            imageSize: new PixelSize(12, 8),
            urlString: "https://example.com",
            text: "fallback");

        var parsed = new ClipboardParser().Parse(snapshot);

        Assert.NotNull(parsed);
        var files = Assert.IsType<ClipboardPayload.FilesPayload>(parsed!.Payload);
        Assert.Single(files.Files);
        Assert.Equal(@"C:\tmp\report.pdf", files.Files[0].Path);
        Assert.Equal("report.pdf", files.Files[0].DisplayName);
    }

    [Fact]
    public void ParserRecognizesImageUrlAndText()
    {
        var image = new ClipboardParser().Parse(new ClipboardSnapshot(
            source: UnknownSource,
            imageData: new byte[] { 9, 8 },
            imageSize: new PixelSize(20, 10)));

        var url = new ClipboardParser().Parse(new ClipboardSnapshot(
            source: UnknownSource,
            urlString: " HTTPS://Example.COM/path "));

        var text = new ClipboardParser().Parse(new ClipboardSnapshot(
            source: UnknownSource,
            text: "hello"));

        var imagePayload = Assert.IsType<ClipboardPayload.ImagePayload>(image!.Payload);
        Assert.Equal(20, imagePayload.Metadata.Width);
        Assert.Equal(10, imagePayload.Metadata.Height);

        var urlPayload = Assert.IsType<ClipboardPayload.UrlPayload>(url!.Payload);
        Assert.Equal("https://example.com/path", urlPayload.Url.AbsoluteUri);

        var textPayload = Assert.IsType<ClipboardPayload.TextPayload>(text!.Payload);
        Assert.Equal("hello", textPayload.Text);
    }

    [Fact]
    public void EmptyClipboardIsIgnored()
    {
        Assert.Null(new ClipboardParser().Parse(new ClipboardSnapshot(UnknownSource)));
    }

    [Fact]
    public void SemanticallyEqualTextHasStableFingerprint()
    {
        var first = ContentFingerprint.Make(new ClipboardPayload.TextPayload("line 1\r\nline 2"));
        var second = ContentFingerprint.Make(new ClipboardPayload.TextPayload("line 1\nline 2"));

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
    }
}
