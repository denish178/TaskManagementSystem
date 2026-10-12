using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Notification.Hubs;
using TaskManagement.Notification.Data;
using TaskManagement.Notification.DTOs;
using TaskManagement.Notification.Models;
using TaskManagement.Shared.Auth;

namespace TaskManagement.Notification.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationDbContext _context;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationsController(
        NotificationDbContext context,
        IHubContext<NotificationHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool unreadOnly = false)
    {
        var userId = CurrentUser.GetUserId(User);

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(50)
            .Select(n => new NotificationResponse(
                n.Id,
                n.UserId,
                n.Title,
                n.Message,
                n.LinkUrl,
                n.IsRead,
                n.CreatedAtUtc))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = CurrentUser.GetUserId(User);
        var count = await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        return Ok(new { count });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateNotificationRequest request)
    {
        if (request.UserId == Guid.Empty)
        {
            return BadRequest(new { message = "UserId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Title is required." });
        }

        var notification = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = request.Title.Trim(),
            Message = request.Message?.Trim() ?? "",
            LinkUrl = request.LinkUrl,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var payload = new NotificationResponse(
            notification.Id,
            notification.UserId,
            notification.Title,
            notification.Message,
            notification.LinkUrl,
            notification.IsRead,
            notification.CreatedAtUtc);

        await _hub.Clients
            .Group(NotificationHub.UserGroup(notification.UserId))
            .SendAsync("NotificationReceived", payload);

        return CreatedAtAction(
            nameof(List),
            new NotificationResponse(
                notification.Id,
                notification.UserId,
                notification.Title,
                notification.Message,
                notification.LinkUrl,
                notification.IsRead,
                notification.CreatedAtUtc));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = CurrentUser.GetUserId(User);

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null)
        {
            return NotFound(new { message = "Notification not found." });
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = CurrentUser.GetUserId(User);

        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(n => n.IsRead, true));

        return NoContent();
    }
}
