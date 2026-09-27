import AppKit
import AVFoundation
import ImageIO
import SwiftUI

struct TextCardContent: View {
    let text: String

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(text)
                .font(.system(size: 14))
                .lineLimit(7)
                .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
            Text("\(text.count) 个字符")
                .font(.caption)
                .foregroundStyle(.secondary)
                .frame(maxWidth: .infinity, alignment: .trailing)
        }
    }
}

struct URLCardContent: View {
    let url: URL
    let previewsEnabled: Bool
    @State private var preview: LinkPreview?

    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            Image(systemName: "link")
                .font(.system(size: 38))
                .foregroundStyle(.blue)
                .frame(maxWidth: .infinity)
            Text(preview?.title ?? url.host() ?? url.absoluteString)
                .font(.headline)
            Text(url.absoluteString)
                .font(.caption)
                .foregroundStyle(.secondary)
                .lineLimit(3)
        }
        .frame(maxHeight: .infinity, alignment: .top)
        .task(id: url) {
            preview = await LinkPreviewService().preview(for: url, enabled: previewsEnabled)
        }
    }
}

struct ImageCardContent: View {
    let metadata: ImageMetadata

    var body: some View {
        VStack(spacing: 8) {
            if let image = loadImage() {
                Image(nsImage: image)
                    .resizable()
                    .scaledToFit()
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else {
                ContentUnavailableView("预览不可用", systemImage: "photo")
            }
            Text("\(metadata.width) × \(metadata.height)")
                .font(.caption)
                .foregroundStyle(.secondary)
        }
    }

    private func loadImage() -> NSImage? {
        let root = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            .appending(path: "WPaste", directoryHint: .isDirectory)
        return NSImage(contentsOf: root.appending(path: metadata.relativePath))
    }
}

struct FilesCardContent: View {
    let files: [FileReference]
    @State private var thumbnail: CGImage?

    var body: some View {
        VStack(spacing: 10) {
            if let thumbnail {
                FileThumbnailView(image: thumbnail, name: files.first?.displayName ?? "图片")
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else {
                Image(systemName: files.count == 1 ? "doc.fill" : "doc.on.doc.fill")
                    .font(.system(size: 64))
                    .foregroundStyle(.white, .blue)
                    .frame(maxHeight: .infinity)
            }
            Text(files.count == 1 ? files[0].displayName : "多个文件")
                .font(.headline)
                .lineLimit(1)
            if files.contains(where: { !FileManager.default.fileExists(atPath: $0.path) }) {
                Label("文件已不存在", systemImage: "exclamationmark.triangle.fill")
                    .font(.caption)
                    .foregroundStyle(.orange)
            }
        }
        .task(id: files) {
            thumbnail = nil
            guard !files.isEmpty, let file = files.first else { return }
            let image = await Task.detached(priority: .utility) {
                if files.allSatisfy(\.isImage) { return FileImageThumbnail.load(file) }
                if files.allSatisfy(\.isVideo) { return await FileVideoThumbnail.load(file) }
                return nil
            }.value
            guard !Task.isCancelled else { return }
            thumbnail = image
        }
    }
}

private enum FileVideoThumbnail {
    static func load(_ file: FileReference) async -> CGImage? {
        let url = URL(fileURLWithPath: file.path)
        let accessed = url.startAccessingSecurityScopedResource()
        defer { if accessed { url.stopAccessingSecurityScopedResource() } }
        guard (try? url.resourceValues(forKeys: [.isRegularFileKey]))?.isRegularFile == true else { return nil }
        let generator = AVAssetImageGenerator(asset: AVURLAsset(url: url))
        generator.appliesPreferredTrackTransform = true
        generator.maximumSize = CGSize(width: 512, height: 512)
        do {
            return try await generator.image(at: .zero).image
        } catch {
            return nil
        }
    }
}

private enum FileImageThumbnail {
    static func load(_ file: FileReference) -> CGImage? {
        let url = URL(fileURLWithPath: file.path)
        let accessed = url.startAccessingSecurityScopedResource()
        defer { if accessed { url.stopAccessingSecurityScopedResource() } }
        guard (try? url.resourceValues(forKeys: [.isRegularFileKey]))?.isRegularFile == true,
              let source = CGImageSourceCreateWithURL(url as CFURL, [
                kCGImageSourceShouldCache: false
              ] as CFDictionary) else { return nil }
        // Decode only a card-sized thumbnail, with camera orientation applied.
        return CGImageSourceCreateThumbnailAtIndex(source, 0, [
            kCGImageSourceCreateThumbnailFromImageAlways: true,
            kCGImageSourceCreateThumbnailWithTransform: true,
            kCGImageSourceThumbnailMaxPixelSize: 512,
            kCGImageSourceShouldCacheImmediately: true
        ] as CFDictionary)
    }
}

private struct FileThumbnailView: NSViewRepresentable {
    let image: CGImage
    let name: String

    func makeNSView(context: Context) -> NSImageView {
        let view = NSImageView()
        view.imageScaling = .scaleProportionallyUpOrDown
        view.setAccessibilityIdentifier("clipboard-file-preview")
        return view
    }

    func updateNSView(_ view: NSImageView, context: Context) {
        view.image = NSImage(cgImage: image, size: .zero)
        view.setAccessibilityLabel(name)
    }

    func sizeThatFits(_ proposal: ProposedViewSize, nsView: NSImageView, context: Context) -> CGSize? {
        CGSize(width: proposal.width ?? 214, height: proposal.height ?? 120)
    }
}
