namespace TaskManagement.File.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType);

    Task<Stream> DownloadAsync(string blobName);

    Task DeleteAsync(string blobName);
}