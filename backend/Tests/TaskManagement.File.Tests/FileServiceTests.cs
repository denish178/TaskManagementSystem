using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.File.Controllers;
using TaskManagement.File.Data;
using TaskManagement.File.Interfaces;
using TaskManagement.File.Models;

namespace TaskManagement.File.Tests;

public class FileServiceTests
{
    private static FileDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FileDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FileDbContext(options);
    }

    private static IFormFile CreateFile(
        string fileName,
        string content = "test file content",
        string contentType = "text/plain")
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);

        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task Upload_WithInvalidExtension_ReturnsBadRequest()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var blobService = new Mock<IBlobStorageService>();

        var controller = new FilesController(
            blobService.Object,
            dbContext);

        var file = CreateFile(
            "malicious.exe",
            "fake executable",
            "application/octet-stream");

        // Act
        var result = await controller.Upload(
            file,
            taskId: 1,
            uploadedBy: 100);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Contains(
            ".exe",
            badRequest.Value?.ToString());

        blobService.Verify(
            x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Upload_WithEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var blobService = new Mock<IBlobStorageService>();

        var controller = new FilesController(
            blobService.Object,
            dbContext);

        var emptyStream = new MemoryStream();

        var file = new FormFile(
            emptyStream,
            0,
            0,
            "file",
            "empty.txt");

        // Act
        var result = await controller.Upload(
            file,
            taskId: 1,
            uploadedBy: 100);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(
            "File is required.",
            badRequest.Value);

        blobService.Verify(
            x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Upload_WithValidFile_ReturnsOkAndSavesMetadata()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var blobService = new Mock<IBlobStorageService>();

        blobService
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync("test-blob.txt");

        var controller = new FilesController(
            blobService.Object,
            dbContext);

        var file = CreateFile(
            "test.txt",
            "Hello testing",
            "text/plain");

        // Act
        var result = await controller.Upload(
            file,
            taskId: 10,
            uploadedBy: 100);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var attachment =
            Assert.IsType<Attachment>(okResult.Value);

        Assert.Equal(10, attachment.TaskId);
        Assert.Equal(100, attachment.UploadedBy);
        Assert.Equal("test.txt", attachment.FileName);
        Assert.Equal("test-blob.txt", attachment.BlobName);
        Assert.Equal("text/plain", attachment.ContentType);

        var savedAttachment =
            await dbContext.Attachments.FirstAsync();

        Assert.Equal("test.txt", savedAttachment.FileName);

        blobService.Verify(
            x => x.UploadAsync(
                It.IsAny<Stream>(),
                "test.txt",
                "text/plain"),
            Times.Once);
    }

    [Fact]
    public async Task GetById_WhenAttachmentDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var blobService = new Mock<IBlobStorageService>();

        var controller = new FilesController(
            blobService.Object,
            dbContext);

        // Act
        var result = await controller.GetById(999);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(
            "Attachment not found.",
            notFound.Value);
    }

    [Fact]
    public async Task GetById_WhenAttachmentExists_ReturnsOk()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        dbContext.Attachments.Add(new Attachment
        {
            Id = 1,
            TaskId = 10,
            FileName = "test.txt",
            BlobName = "blob-test.txt",
            ContentType = "text/plain",
            FileSize = 100,
            UploadedBy = 100,
            UploadedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var blobService = new Mock<IBlobStorageService>();

        var controller = new FilesController(
            blobService.Object,
            dbContext);

        // Act
        var result = await controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var attachment =
            Assert.IsType<Attachment>(okResult.Value);

        Assert.Equal(1, attachment.Id);
        Assert.Equal("test.txt", attachment.FileName);
    }

    [Fact]
    public async Task Delete_WhenAttachmentDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var blobService = new Mock<IBlobStorageService>();

        var controller = new FilesController(
            blobService.Object,
            dbContext);

        // Act
        var result = await controller.Delete(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);

        blobService.Verify(
            x => x.DeleteAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Delete_WhenAttachmentExists_DeletesBlobAndMetadata()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        dbContext.Attachments.Add(new Attachment
        {
            Id = 1,
            TaskId = 10,
            FileName = "test.txt",
            BlobName = "blob-test.txt",
            ContentType = "text/plain",
            FileSize = 100,
            UploadedBy = 100,
            UploadedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var blobService = new Mock<IBlobStorageService>();

        var controller = new FilesController(
            blobService.Object,
            dbContext);

        // Act
        var result = await controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);

        blobService.Verify(
            x => x.DeleteAsync("blob-test.txt"),
            Times.Once);

        var attachment =
            await dbContext.Attachments.FindAsync(1);

        Assert.Null(attachment);
    }
}