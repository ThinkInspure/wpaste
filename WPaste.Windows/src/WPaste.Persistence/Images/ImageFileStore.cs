using WPaste.Core.Paths;

namespace WPaste.Persistence.Images;

public interface IImageFileStore
{
    Task<string> SaveAsync(ReadOnlyMemory<byte> pngData, CancellationToken cancellationToken = default);

    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

    bool Exists(string relativePath);
}

public sealed class ImageFileStore : IImageFileStore
{
    private readonly string _imagesRoot;

    public ImageFileStore(string? imagesRoot = null)
    {
        _imagesRoot = imagesRoot ?? AppPaths.ImagesDirectory;
    }

    public Task<string> SaveAsync(ReadOnlyMemory<byte> pngData, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_imagesRoot);
        var fileName = $"{Guid.NewGuid():N}.png";
        var absolutePath = Path.Combine(_imagesRoot, fileName);
        File.WriteAllBytes(absolutePath, pngData.ToArray());
        return Task.FromResult(Path.Combine("Images", fileName).Replace('\\', '/'));
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = ResolvePath(relativePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    public bool Exists(string relativePath)
    {
        try
        {
            return File.Exists(ResolvePath(relativePath));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private string ResolvePath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath) || relativePath.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid relative path.", nameof(relativePath));
        }

        var absolutePath = Path.GetFullPath(Path.Combine(AppPaths.DataRoot, relativePath));
        if (!absolutePath.StartsWith(AppPaths.DataRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid relative path.", nameof(relativePath));
        }

        return absolutePath;
    }
}
