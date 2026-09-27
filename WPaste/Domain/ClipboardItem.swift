import Foundation
import UniformTypeIdentifiers

enum ClipboardPayload: Equatable, Sendable {
    case text(String)
    case url(URL)
    case image(ImageMetadata)
    case files([FileReference])

    var isImage: Bool {
        switch self {
        case .image: true
        case let .files(files): !files.isEmpty && files.allSatisfy(\.isImage)
        default: false
        }
    }

    var kindLabel: String {
        switch self {
        case .text:
            "文本"
        case .url:
            "链接"
        case .image:
            "图片"
        case let .files(files):
            isImage ? (files.count == 1 ? "图片" : "\(files.count) 张图片") : "\(files.count) 个文件"
        }
    }
}

struct ImageMetadata: Codable, Equatable, Sendable {
    let width: Int
    let height: Int
    let relativePath: String
}

struct FileReference: Codable, Equatable, Sendable {
    let path: String
    let displayName: String
    var bookmarkData: Data?

    var isImage: Bool {
        let extensionName = URL(fileURLWithPath: path).pathExtension.lowercased()
        return UTType(filenameExtension: extensionName)?.conforms(to: .image) == true
    }

    var isVideo: Bool {
        let extensionName = URL(fileURLWithPath: path).pathExtension.lowercased()
        return UTType(filenameExtension: extensionName)?.conforms(to: .movie) == true
    }

    init(path: String, displayName: String, bookmarkData: Data? = nil) {
        self.path = path
        self.displayName = displayName
        self.bookmarkData = bookmarkData
    }
}

struct ClipboardSource: Codable, Equatable, Sendable {
    let bundleIdentifier: String?
    let name: String
}

struct ClipboardItem: Identifiable, Equatable, Sendable {
    let id: UUID
    var payload: ClipboardPayload
    var fingerprint: String
    var source: ClipboardSource
    var createdAt: Date
    var lastUsedAt: Date

    init(
        id: UUID = UUID(),
        payload: ClipboardPayload,
        fingerprint: String,
        source: ClipboardSource,
        createdAt: Date = .now,
        lastUsedAt: Date = .now
    ) {
        self.id = id
        self.payload = payload
        self.fingerprint = fingerprint
        self.source = source
        self.createdAt = createdAt
        self.lastUsedAt = lastUsedAt
    }

    static func == (lhs: ClipboardItem, rhs: ClipboardItem) -> Bool {
        lhs.id == rhs.id
    }
}
