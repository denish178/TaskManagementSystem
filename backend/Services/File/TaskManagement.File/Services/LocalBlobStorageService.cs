using Microsoft.Extensions.Options;
using TaskManagement.File.Interfaces;

namespace TaskManagement.File.Services;

/// <summary>
/// Development-only file storage on disk (no Azure Blob account required).
/// </summary>
public class LocalBlobStorageService : IBlobStorageService
{
    private readonly string _rootPath;

    public LocalBlobStorageService(IOptions<LocalFileStorageSettings> settings)
    {
        _rootPath = Path.GetFullPath(
            settings.Value.RootPath ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads"));

        Directory.CreateDirectory(_rootPath);
    }

    private string ResolveBlobPath(string blobName)
    {
        var safeName = Path.GetFileName(blobName);
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, safeName));
        var rootWithSep = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid blob path.");
        }

        return fullPath;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        var safeFileName = Path.GetFileName(fileName);
        var blobName = $"{Guid.NewGuid()}-{safeFileName}";
        var path = ResolveBlobPath(blobName);

        await using var file = System.IO.File.Create(path);
        await fileStream.CopyToAsync(file);

        return blobName;
    }

    public Task<Stream> DownloadAsync(string blobName)
    {
        var path = ResolveBlobPath(blobName);
        if (!System.IO.File.Exists(path))
        {
            throw new FileNotFoundException("Blob not found.", blobName);
        }

        return Task.FromResult<Stream>(System.IO.File.OpenRead(path));
    }

    public Task DeleteAsync(string blobName)
    {
        var path = ResolveBlobPath(blobName);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        return Task.CompletedTask;
    }
}

public class LocalFileStorageSettings
{
    public string? RootPath { get; set; }
}
