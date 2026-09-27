import AppKit
import AVFoundation
import CoreVideo
import SwiftUI
import Testing
@testable import WPaste

@MainActor
struct ClipboardCardViewTests {
    @Test func cardShowsInstalledSourceApplicationIconInBottomLeft() throws {
        try withCard(source: .init(bundleIdentifier: "com.apple.finder", name: "Finder")) { view in
            let iconView = try #require(sourceIcon(in: view))
            let image = try #require(iconView.image)
            #expect(image.isValid)
            #expect(iconView.toolTip == "Finder")
            let frame = iconView.convert(iconView.bounds, to: view)
            #expect(frame.midX < view.bounds.midX)
            let distanceFromBottom = view.isFlipped ? view.bounds.maxY - frame.midY : frame.midY
            #expect(distanceFromBottom < 28)
        }
    }

    @Test(arguments: [nil, "", "com.wpaste.missing-application.\(UUID().uuidString)"] as [String?])
    func cardOmitsIconWhenSourceApplicationCannotBeResolved(bundleIdentifier: String?) throws {
        // A familiar display name must not be used to guess an unrelated app's icon.
        try withCard(source: .init(bundleIdentifier: bundleIdentifier, name: "Finder")) { view in
            #expect(sourceIcon(in: view) == nil)
        }
    }

    @Test(arguments: [NSBitmapImageRep.FileType.png, .jpeg])
    func finderImageFileShowsBoundedThumbnailAndStillPastesAsFile(format: NSBitmapImageRep.FileType) async throws {
        let root = FileManager.default.temporaryDirectory.appending(path: UUID().uuidString)
        try FileManager.default.createDirectory(at: root, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: root) }
        let url = root.appending(path: format == .png ? "图片 sample.png" : "图片 sample.JPG")
        let colorSpace = try #require(CGColorSpace(name: CGColorSpace.sRGB))
        let context = try #require(CGContext(
            data: nil, width: 1200, height: 600, bitsPerComponent: 8, bytesPerRow: 0,
            space: colorSpace,
            bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue
        ))
        context.setFillColor(red: 1, green: 0, blue: 0, alpha: 1)
        context.fill(CGRect(x: 0, y: 0, width: 1200, height: 600))
        let bitmap = NSBitmapImageRep(cgImage: try #require(context.makeImage()))
        try #require(bitmap.representation(using: format, properties: [:])).write(to: url)
        let input = NSPasteboard.withUniqueName()
        let output = NSPasteboard.withUniqueName()
        defer { input.releaseGlobally(); output.releaseGlobally() }
        #expect(input.writeObjects([url as NSURL]))
        let snapshot = try #require(SystemPasteboardClient(pasteboard: input).snapshot())
        let parsed = try #require(ClipboardParser().parse(snapshot))
        guard case let .files(files) = parsed.payload else {
            Issue.record("Finder copies must retain their file payload")
            return
        }
        try await withFileCard(files: files) { view in
            let preview = try #require(filePreview(in: view))
            let image = try #require(preview.image)
            var rect = CGRect(origin: .zero, size: image.size)
            let cgImage = try #require(image.cgImage(forProposedRect: &rect, context: nil, hints: nil))
            #expect(cgImage.width <= 512)
            #expect(cgImage.height <= 512)
            #expect(cgImage.width == cgImage.height * 2)
            let result = NSBitmapImageRep(cgImage: cgImage)
            let pixel = try #require(result.colorAt(x: cgImage.width / 2, y: cgImage.height / 2)?.usingColorSpace(.sRGB))
            // AppKit may color-manage the thumbnail for the attached display.
            // Verify the fixture stays visibly red, rather than an icon or blank image.
            #expect(pixel.redComponent > 0.8)
            #expect(pixel.greenComponent < 0.3)
            #expect(pixel.blueComponent < 0.3)
        }
        #expect(SystemPasteboardWriter(pasteboard: output).write(parsed.payload, asPlainText: false))
        #expect((output.readObjects(forClasses: [NSURL.self], options: [.urlReadingFileURLsOnly: true]) as? [URL]) == [url])
    }

    @Test func videoFileShowsFirstFrameThumbnail() async throws {
        let root = FileManager.default.temporaryDirectory.appending(path: UUID().uuidString)
        try FileManager.default.createDirectory(at: root, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: root) }
        let url = root.appending(path: "视频 sample.mov")
        try await makeSolidBlueVideo(at: url, width: 640, height: 360)
        try await withFileCard(files: [.init(path: url.path, displayName: "视频 sample.mov")]) { view in
            let preview = try #require(filePreview(in: view))
            let image = try #require(preview.image)
            var rect = CGRect(origin: .zero, size: image.size)
            let cgImage = try #require(image.cgImage(forProposedRect: &rect, context: nil, hints: nil))
            #expect(cgImage.width <= 512)
            #expect(cgImage.height <= 512)
            // 16:9 aspect ratio must survive the thumbnail cap.
            let ratio = Double(cgImage.width) / Double(cgImage.height)
            #expect(abs(ratio - 16.0 / 9.0) < 0.05)
            let result = NSBitmapImageRep(cgImage: cgImage)
            let pixel = try #require(result.colorAt(x: cgImage.width / 2, y: cgImage.height / 2)?.usingColorSpace(.sRGB))
            // Verify the fixture stays visibly blue, rather than an icon or blank image.
            #expect(pixel.blueComponent > 0.6)
            #expect(pixel.redComponent < 0.4)
            #expect(pixel.greenComponent < 0.4)
        }
    }

    @Test func missingCorruptAndNonImageFilesKeepFallback() async throws {
        let root = FileManager.default.temporaryDirectory.appending(path: UUID().uuidString)
        try FileManager.default.createDirectory(at: root, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: root) }
        for name in ["missing.png", "corrupt.jpg", "notes.txt", "broken.mov"] {
            let url = root.appending(path: name)
            if name != "missing.png" { try Data("not an image".utf8).write(to: url) }
            try await withFileCard(files: [.init(path: url.path, displayName: name)]) { view in
                #expect(filePreview(in: view) == nil)
            }
        }
    }

    private func filePreview(in view: NSView) -> NSImageView? {
        if let image = view as? NSImageView, image.accessibilityIdentifier() == "clipboard-file-preview" {
            return image
        }
        return view.subviews.lazy.compactMap { filePreview(in: $0) }.first
    }

    private func withFileCard(files: [FileReference], check: (NSView) throws -> Void) async throws {
        let item = ClipboardItem(payload: .files(files), fingerprint: "files", source: .init(bundleIdentifier: "com.apple.finder", name: "Finder"))
        let view = NSHostingView(rootView: ClipboardCardView(item: item, index: 0, isSelected: false))
        view.frame = NSRect(x: 0, y: 0, width: 238, height: 246)
        let window = NSWindow(contentRect: view.frame, styleMask: [.borderless], backing: .buffered, defer: false)
        window.contentView = view
        defer { window.orderOut(nil); window.contentView = nil }
        view.layoutSubtreeIfNeeded()
        for _ in 0..<50 {
            try await Task.sleep(for: .milliseconds(20))
            view.layoutSubtreeIfNeeded()
            if filePreview(in: view) != nil { break }
        }
        try check(view)
    }

    private func makeSolidBlueVideo(at url: URL, width: Int, height: Int) async throws {
        let writer = try AVAssetWriter(url: url, fileType: .mov)
        let input = AVAssetWriterInput(mediaType: .video, outputSettings: [
            AVVideoCodecKey: AVVideoCodecType.h264,
            AVVideoWidthKey: width,
            AVVideoHeightKey: height
        ])
        let adaptor = AVAssetWriterInputPixelBufferAdaptor(assetWriterInput: input, sourcePixelBufferAttributes: [
            kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32BGRA
        ])
        #expect(writer.canAdd(input))
        writer.add(input)
        #expect(writer.startWriting())
        writer.startSession(atSourceTime: .zero)

        var pixelBuffer: CVPixelBuffer?
        #expect(CVPixelBufferCreate(kCFAllocatorDefault, width, height, kCVPixelFormatType_32BGRA, nil, &pixelBuffer) == kCVReturnSuccess)
        let buffer = try #require(pixelBuffer)
        CVPixelBufferLockBaseAddress(buffer, [])
        let base = try #require(CVPixelBufferGetBaseAddress(buffer))
        let bytesPerRow = CVPixelBufferGetBytesPerRow(buffer)
        for row in 0..<height {
            for column in 0..<width {
                let pixel = base.advanced(by: row * bytesPerRow + column * 4).assumingMemoryBound(to: UInt8.self)
                pixel[0] = 255 // B
                pixel[1] = 30  // G
                pixel[2] = 30  // R
                pixel[3] = 255 // A
            }
        }
        CVPixelBufferUnlockBaseAddress(buffer, [])

        for frame in 0..<6 {
            while !input.isReadyForMoreMediaData {
                try await Task.sleep(for: .milliseconds(10))
            }
            #expect(adaptor.append(buffer, withPresentationTime: CMTime(value: CMTimeValue(frame), timescale: 30)))
        }
        input.markAsFinished()
        await writer.finishWriting()
        #expect(writer.status == .completed)
    }

    private func sourceIcon(in view: NSView) -> NSImageView? {
        if let imageView = view as? NSImageView,
           imageView.accessibilityIdentifier() == "clipboard-source-icon" {
            return imageView
        }
        return view.subviews.lazy.compactMap { sourceIcon(in: $0) }.first
    }

    private func withCard(source: ClipboardSource, check: (NSView) throws -> Void) throws {
        let item = ClipboardItem(payload: .text("Example"), fingerprint: "example", source: source)
        let view = NSHostingView(rootView: ClipboardCardView(item: item, index: 0, isSelected: false))
        view.frame = NSRect(x: 0, y: 0, width: 238, height: 246)
        let window = NSWindow(contentRect: view.frame, styleMask: [.borderless], backing: .buffered, defer: false)
        window.contentView = view
        defer { window.orderOut(nil); window.contentView = nil }
        view.layoutSubtreeIfNeeded()
        try check(view)
    }
}
