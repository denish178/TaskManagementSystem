using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Notification.Controllers;
using TaskManagement.Notification.Data;
using TaskManagement.Notification.DTOs;
using TaskManagement.Notification.Hubs;
using TaskManagement.Notification.Models;

namespace TaskManagement.Notification.Tests;

public class NotificationsControllerTests
{
    private static readonly Guid UserId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static NotificationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new NotificationDbContext(options);
    }

    private static Mock<IHubContext<NotificationHub>> CreateHubMock()
    {
        var clients = new Mock<IHubClients>();
        var group = new Mock<IClientProxy>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(group.Object);

        var hub = new Mock<IHubContext<NotificationHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);
        return hub;
    }

    private static NotificationsController CreateController(
        NotificationDbContext context,
        ClaimsPrincipal user)
    {
        var controller = new NotificationsController(context, CreateHubMock().Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user },
        };
        return controller;
    }

    private static ClaimsPrincipal UserPrincipal(Guid userId, string role = "User") =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
            ],
            "TestAuth"));

    [Fact]
    public async Task List_ReturnsOnlyCurrentUserNotifications()
    {
        await using var context = CreateContext();
        context.Notifications.AddRange(
            new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                Title = "Mine",
                Message = "Hi",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow,
            },
            new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Title = "Other",
                Message = "Nope",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow,
            });
        await context.SaveChangesAsync();

        var controller = CreateController(context, UserPrincipal(UserId));

        var result = await controller.List();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<NotificationResponse>>(ok.Value);
        Assert.Single(list);
        Assert.Equal("Mine", list.First().Title);
    }

    [Fact]
    public async Task UnreadCount_ReturnsCorrectCount()
    {
        await using var context = CreateContext();
        context.Notifications.AddRange(
            new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                Title = "A",
                Message = "",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow,
            },
            new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                Title = "B",
                Message = "",
                IsRead = true,
                CreatedAtUtc = DateTime.UtcNow,
            });
        await context.SaveChangesAsync();

        var controller = CreateController(context, UserPrincipal(UserId));

        var result = await controller.UnreadCount();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Create_AsAdmin_WithEmptyTitle_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, UserPrincipal(UserId, "Admin"));

        var result = await controller.Create(
            new CreateNotificationRequest(UserId, "  ", "msg", null));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task MarkRead_WrongUser_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var notificationId = Guid.NewGuid();
        context.Notifications.Add(new UserNotification
        {
            Id = notificationId,
            UserId = Guid.NewGuid(),
            Title = "X",
            Message = "",
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, UserPrincipal(UserId));

        var result = await controller.MarkRead(notificationId);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
