using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using TaskManagement.File.Interfaces;

namespace TaskManagement.File.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public BlobStorageService(IOptions<AzureBlobSettings> settings)
    {
        var blobServiceClient =
            new BlobServiceClient(settings.Value.ConnectionString);

        _containerClient =
            blobServiceClient.GetBlobContainerClient(
                settings.Value.ContainerName);
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType)
    {
        await _containerClient.CreateIfNotExistsAsync();

        var safeFileName = Path.GetFileName(fileName);

        var blobName =
            $"{Guid.NewGuid()}-{safeFileName}";

        var blobClient =
            _containerClient.GetBlobClient(blobName);

        var headers = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        await blobClient.UploadAsync(
            fileStream,
            new BlobUploadOptions
            {
                HttpHeaders = headers
            });

        return blobName;
    }

    public async Task<Stream> DownloadAsync(string blobName)
    {
        var blobClient =
            _containerClient.GetBlobClient(blobName);

        var response =
            await blobClient.DownloadStreamingAsync();

        return response.Value.Content;
    }

    public async Task DeleteAsync(string blobName)
    {
        var blobClient =
            _containerClient.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync();
    }
}