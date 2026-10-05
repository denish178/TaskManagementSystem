using Microsoft.EntityFrameworkCore;
using TaskManagement.Task.Models;

namespace TaskManagement.Task.Data;

public class TaskDbContext : DbContext
{
    public TaskDbContext(
        DbContextOptions<TaskDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<SubTask> SubTasks => Set<SubTask>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =====================================================
        // TASK
        // =====================================================

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(x => x.Description)
                .HasMaxLength(5000);

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.Priority)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.CreatedBy)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .IsRequired();

            // Indexes for filtering
            entity.HasIndex(x => x.ProjectId);

            entity.HasIndex(x => x.SprintId);

            entity.HasIndex(x => x.AssigneeId);

            entity.HasIndex(x => x.Status);

            entity.HasIndex(x => x.Priority);

            // Task -> SubTasks
            entity.HasMany(x => x.SubTasks)
                .WithOne(x => x.Task)
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================
        // SUBTASK
        // =====================================================

        modelBuilder.Entity<SubTask>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(x => x.IsCompleted)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();
        });
    }
}