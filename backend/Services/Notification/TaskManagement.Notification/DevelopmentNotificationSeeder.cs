using Microsoft.EntityFrameworkCore;
using TaskManagement.Notification.Data;
using TaskManagement.Notification.Models;

namespace TaskManagement.Notification;

public static class DevelopmentNotificationSeeder
{
    private static readonly Guid AdminId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid UserId =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        await context.Database.MigrateAsync();

        if (await context.Notifications.AnyAsync())
        {
            return;
        }

        context.Notifications.AddRange(
            new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = AdminId,
                Title = "Welcome to TaskFlow",
                Message = "Use the assistant bubble to ask about tasks in your workspace.",
                LinkUrl = "/dashboard",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            },
            new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                Title = "Check My Workspace",
                Message = "Tasks assigned to you appear under My Workspace and My Tasks.",
                LinkUrl = "/my-workspace",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            });

        await context.SaveChangesAsync();
    }
}
