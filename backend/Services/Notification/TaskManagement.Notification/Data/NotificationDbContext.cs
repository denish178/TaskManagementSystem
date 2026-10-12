using Microsoft.EntityFrameworkCore;
using TaskManagement.Notification.Models;

namespace TaskManagement.Notification.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserNotification> Notifications => Set<UserNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.LinkUrl).HasMaxLength(500);
            entity.HasIndex(x => new { x.UserId, x.IsRead });
            entity.HasIndex(x => x.CreatedAtUtc);
        });
    }
}
