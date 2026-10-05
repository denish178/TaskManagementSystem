using Microsoft.EntityFrameworkCore;
using TaskManagement.Project.Models;
using ProjectModel = TaskManagement.Project.Models.Project;

namespace TaskManagement.Project.Data;

public class ProjectDbContext : DbContext
{
    public ProjectDbContext(
        DbContextOptions<ProjectDbContext> options)
        : base(options)
    {
    }

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    public DbSet<ProjectModel> Projects => Set<ProjectModel>();

    public DbSet<Sprint> Sprints => Set<Sprint>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // -------------------------------
        // Team
        // -------------------------------
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.CreatedBy)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasMany(x => x.Members)
                .WithOne(x => x.Team)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Projects)
                .WithOne(x => x.Team)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------------------
        // TeamMember
        // -------------------------------
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Role)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.JoinedAt)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.TeamId,
                x.UserId
            })
            .IsUnique();
        });

        // -------------------------------
        // Project
        // -------------------------------
        modelBuilder.Entity<ProjectModel>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            entity.Property(x => x.CreatedBy)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasIndex(x => x.TeamId);

            entity.HasMany(x => x.Sprints)
                .WithOne(x => x.Project)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------------------
        // Sprint
        // -------------------------------
        modelBuilder.Entity<Sprint>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Goal)
                .HasMaxLength(2000);

            entity.Property(x => x.StartDate)
                .IsRequired();

            entity.Property(x => x.EndDate)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasIndex(x => x.ProjectId);
        });
    }
}