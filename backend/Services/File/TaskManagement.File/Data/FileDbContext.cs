using Microsoft.EntityFrameworkCore;
using TaskManagement.File.Models;

namespace TaskManagement.File.Data;

public class FileDbContext : DbContext
{
    public FileDbContext(DbContextOptions<FileDbContext> options)
        : base(options)
    {
    }
    // Attachment metadata is stored in the SQL Server Attachments table.
    public DbSet<Attachment> Attachments { get; set; }
}
