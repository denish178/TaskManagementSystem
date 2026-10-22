using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManagement.Comment.Controllers;
using TaskManagement.Comment.DTOs;
using TaskManagement.Comment.Repositories;
using TaskManagement.Comment.Services;
using FeedbackModel = TaskManagement.Comment.Models.TaskCompletionFeedback;

namespace TaskManagement.Comment.Tests;

public class CompletionFeedbackControllerTests
{
    private static readonly Guid UserId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid TaskId =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static CompletionFeedbackController CreateController(
        Mock<ICompletionFeedbackRepository> repository,
        Mock<ITaskFeedbackPolicyClient> policy,
        string role = "User")
    {
        var controller = new CompletionFeedbackController(
            repository.Object,
            policy.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
                            new Claim(ClaimTypes.Role, role),
                        ],
                        "TestAuth")),
            },
        };
        return controller;
    }

    [Fact]
    public async Task Upsert_WhenTaskNotDone_ReturnsBadRequest()
    {
        var repo = new Mock<ICompletionFeedbackRepository>();
        var policy = new Mock<ITaskFeedbackPolicyClient>();
        policy
            .Setup(p => p.GetTaskContextAsync(TaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskFeedbackContext
            {
                TaskId = TaskId,
                Status = "InProgress",
                CompletedByUserId = UserId,
                CanAccess = true,
            });

        var controller = CreateController(repo, policy);

        var result = await controller.Upsert(
            TaskId,
            new SubmitCompletionFeedbackRequest { Content = "Done well" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upsert_WhenDoneAndCompleter_SavesToRepository()
    {
        var repo = new Mock<ICompletionFeedbackRepository>();
        repo
            .Setup(r => r.UpsertAsync(It.IsAny<FeedbackModel>()))
            .ReturnsAsync((FeedbackModel f) => f);

        var policy = new Mock<ITaskFeedbackPolicyClient>();
        policy
            .Setup(p => p.GetTaskContextAsync(TaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskFeedbackContext
            {
                TaskId = TaskId,
                Status = "Done",
                CompletedByUserId = UserId,
                CanAccess = true,
            });

        var controller = CreateController(repo, policy);

        var result = await controller.Upsert(
            TaskId,
            new SubmitCompletionFeedbackRequest { Content = "Shipped on time" });

        Assert.IsType<OkObjectResult>(result);
        repo.Verify(
            r => r.UpsertAsync(It.Is<FeedbackModel>(f =>
                f.TaskId == TaskId && f.Content == "Shipped on time")),
            Times.Once);
    }
}
