namespace TaskManagement.File.Interfaces;

// Abstraction for Azure Blob Storage operations used by the File Service.

public interface IBlobStorageService
{
    // Uploads a file and returns the generated blob name.
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType);

    // Downloads a blob as a stream.
    Task<Stream> DownloadAsync(string blobName);

    // Deletes a blob from storage.
    Task DeleteAsync(string blobName);
}
