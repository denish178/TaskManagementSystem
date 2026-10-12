using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using TaskManagement.Comment.Controllers;
using TaskManagement.Comment.DTOs;
using TaskManagement.Comment.Hubs;
using TaskManagement.Comment.Repositories;
using CommentModel = TaskManagement.Comment.Models.Comment;

namespace TaskManagement.Comment.Tests;

public class CommentsControllerTests
{
    private static readonly Guid UserId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid TaskId =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static CommentsController CreateController(
        Mock<ICommentRepository> repository,
        Mock<IHubContext<CommentHub>>? hub = null)
    {
        hub ??= CreateHubMock();

        var controller = new CommentsController(repository.Object, hub.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())],
                        "TestAuth")),
            },
        };

        return controller;
    }

    private static Mock<IHubContext<CommentHub>> CreateHubMock()
    {
        var clients = new Mock<IHubClients>();
        var group = new Mock<IClientProxy>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(group.Object);

        var hub = new Mock<IHubContext<CommentHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);
        return hub;
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        var repo = new Mock<ICommentRepository>();
        repo
            .Setup(r => r.CreateAsync(It.IsAny<CommentModel>()))
            .ReturnsAsync((CommentModel c) => c);

        var controller = CreateController(repo);

        var result = await controller.Create(new CreateCommentRequest
        {
            TaskId = TaskId,
            Content = "Hello",
        });

        Assert.IsType<CreatedAtActionResult>(result);
        repo.Verify(r => r.CreateAsync(It.Is<CommentModel>(c =>
            c.TaskId == TaskId && c.UserId == UserId && c.Content == "Hello")), Times.Once);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        var repo = new Mock<ICommentRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((CommentModel?)null);

        var controller = CreateController(repo);

        var result = await controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_OtherUsersComment_ReturnsForbid()
    {
        var commentId = Guid.NewGuid();
        var repo = new Mock<ICommentRepository>();
        repo.Setup(r => r.GetByIdAsync(commentId)).ReturnsAsync(new CommentModel
        {
            Id = commentId,
            TaskId = TaskId,
            UserId = Guid.NewGuid(),
            Content = "x",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        var controller = CreateController(repo);

        var result = await controller.Update(
            commentId,
            new UpdateCommentRequest { Content = "updated" });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public void CommentHub_TaskGroup_UsesStableFormat()
    {
        var id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        Assert.Equal($"task:{id}", CommentHub.TaskGroup(id));
    }
}
