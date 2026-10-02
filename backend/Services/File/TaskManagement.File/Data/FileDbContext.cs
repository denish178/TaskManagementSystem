using Microsoft.EntityFrameworkCore;
using TaskManagement.File.Models;

namespace TaskManagement.File.Data;

public class FileDbContext : DbContext
{
    public FileDbContext(DbContextOptions<FileDbContext> options)
        : base(options)
    {
    }

    public DbSet<Attachment> Attachments { get; set; }
}