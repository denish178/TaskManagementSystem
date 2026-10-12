using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Auth.Data;
using TaskManagement.Auth.Models;

namespace TaskManagement.Auth;

public static class DevelopmentDataSeeder
{
    private static readonly (string Email, string Name, string Role, Guid Id)[] DevUsers =
    [
        ("admin@local.test", "Local Admin", "Admin", Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
        ("user@local.test", "Local User", "User", Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var adminPassword = Environment.GetEnvironmentVariable("TMS_DEV_ADMIN_PASSWORD");
        var userPassword = Environment.GetEnvironmentVariable("TMS_DEV_USER_PASSWORD");
        if (string.IsNullOrWhiteSpace(adminPassword) || string.IsNullOrWhiteSpace(userPassword))
        {
            return;
        }

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await context.Database.MigrateAsync();

        var hasher = new PasswordHasher<User>();

        foreach (var (email, name, role, id) in DevUsers)
        {
            var password = email.StartsWith("admin@", StringComparison.OrdinalIgnoreCase)
                ? adminPassword
                : userPassword;

            var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existing is null)
            {
                var user = new User
                {
                    Id = id,
                    Name = name,
                    Email = email,
                    Role = role,
                    CreatedAt = DateTime.UtcNow,
                };
                user.PasswordHash = hasher.HashPassword(user, password);
                context.Users.Add(user);
            }
            else
            {
                existing.Name = name;
                existing.Role = role;
                existing.PasswordHash = hasher.HashPassword(existing, password);
            }
        }

        await context.SaveChangesAsync();
    }
}
