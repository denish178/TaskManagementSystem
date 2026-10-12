using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.File.Controllers;
using TaskManagement.File.Data;
using TaskManagement.File.Interfaces;
using TaskManagement.File.Models;
using TaskManagement.File.Services;
using System.Security.Claims;
namespace TaskManagement.File.Tests;

public class FileServiceTests
{
    private static readonly Guid SampleTaskId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid SampleUserId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static FileDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FileDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FileDbContext(options);
    }

    private static FilesController CreateController(
        IBlobStorageService blobService,
        FileDbContext dbContext,
        ITaskAccessClient? taskAccess = null)
    {
        var access = taskAccess ?? CreateTaskAccessMock().Object;
        var controller = new FilesController(blobService, dbContext, access);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.NameIdentifier, SampleUserId.ToString()),
                            new Claim(ClaimTypes.Role, "Admin"),
                        ],
                        "TestAuth")),
            },
        };
        return controller;
    }

    private static Mock<ITaskAccessClient> CreateTaskAccessMock()
    {
        var mock = new Mock<ITaskAccessClient>();
        mock
            .Setup(x => x.CanAccessTaskAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return mock;
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
        await using var dbContext = CreateDbContext();

        var blobService = new Mock<IBlobStorageService>();

        var controller = CreateController(blobService.Object, dbContext);

        var file = CreateFile(
            "malicious.exe",
            "fake executable",
            "application/octet-stream");

        var result = await controller.Upload(
            file,
            taskId: SampleTaskId,
            uploadedBy: SampleUserId);

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
        await using var dbContext = CreateDbContext();

        var blobService = new Mock<IBlobStorageService>();

        var controller = CreateController(blobService.Object, dbContext);

        var emptyStream = new MemoryStream();

        var file = new FormFile(
            emptyStream,
            0,
            0,
            "file",
            "empty.txt");

        var result = await controller.Upload(
            file,
            taskId: SampleTaskId,
            uploadedBy: SampleUserId);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Contains(
            "File is required.",
            badRequest.Value?.ToString());

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
        await using var dbContext = CreateDbContext();

        var blobService = new Mock<IBlobStorageService>();

        blobService
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync("test-blob.txt");

        var controller = CreateController(blobService.Object, dbContext);

        var file = CreateFile(
            "test.txt",
            "Hello testing",
            "text/plain");

        var result = await controller.Upload(
            file,
            taskId: SampleTaskId,
            uploadedBy: SampleUserId);

        var okResult = Assert.IsType<OkObjectResult>(result);

        var attachment =
            Assert.IsType<Attachment>(okResult.Value);

        Assert.Equal(SampleTaskId, attachment.TaskId);
        Assert.Equal(SampleUserId, attachment.UploadedBy);
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
        await using var dbContext = CreateDbContext();

        var blobService = new Mock<IBlobStorageService>();

        var controller = CreateController(blobService.Object, dbContext);

        var result = await controller.GetById(999);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Contains(
            "Attachment not found.",
            notFound.Value?.ToString());
    }

    [Fact]
    public async Task GetById_WhenAttachmentExists_ReturnsOk()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Attachments.Add(new Attachment
        {
            Id = 1,
            TaskId = SampleTaskId,
            FileName = "test.txt",
            BlobName = "blob-test.txt",
            ContentType = "text/plain",
            FileSize = 100,
            UploadedBy = SampleUserId,
            UploadedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var blobService = new Mock<IBlobStorageService>();

        var controller = CreateController(blobService.Object, dbContext);

        var result = await controller.GetById(1);

        var okResult = Assert.IsType<OkObjectResult>(result);

        var attachment =
            Assert.IsType<Attachment>(okResult.Value);

        Assert.Equal(1, attachment.Id);
        Assert.Equal("test.txt", attachment.FileName);
    }

    [Fact]
    public async Task Delete_WhenAttachmentDoesNotExist_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();

        var blobService = new Mock<IBlobStorageService>();

        var controller = CreateController(blobService.Object, dbContext);

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundObjectResult>(result);

        blobService.Verify(
            x => x.DeleteAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Delete_WhenAttachmentExists_DeletesBlobAndMetadata()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Attachments.Add(new Attachment
        {
            Id = 1,
            TaskId = SampleTaskId,
            FileName = "test.txt",
            BlobName = "blob-test.txt",
            ContentType = "text/plain",
            FileSize = 100,
            UploadedBy = SampleUserId,
            UploadedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var blobService = new Mock<IBlobStorageService>();

        var controller = CreateController(blobService.Object, dbContext);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);

        blobService.Verify(
            x => x.DeleteAsync("blob-test.txt"),
            Times.Once);

        var attachment =
            await dbContext.Attachments.FindAsync(1);

        Assert.Null(attachment);
    }
}
