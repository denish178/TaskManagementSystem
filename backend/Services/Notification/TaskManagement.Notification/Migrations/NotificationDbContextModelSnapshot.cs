using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TaskManagement.Notification.Data;

#nullable disable

namespace TaskManagement.Notification.Migrations;

[DbContext(typeof(NotificationDbContext))]
partial class NotificationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.11")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity("TaskManagement.Notification.Models.UserNotification", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("datetime2");
            b.Property<bool>("IsRead").HasColumnType("bit");
            b.Property<string>("LinkUrl").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("Message").IsRequired().HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            b.Property<string>("Title").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<Guid>("UserId").HasColumnType("uniqueidentifier");
            b.HasKey("Id");
            b.HasIndex("CreatedAtUtc");
            b.HasIndex("UserId", "IsRead");
            b.ToTable("Notifications");
        });
    }
}
