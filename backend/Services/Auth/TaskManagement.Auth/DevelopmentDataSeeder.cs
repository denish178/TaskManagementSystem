using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Auth.Data;
using TaskManagement.Auth.Models;

namespace TaskManagement.Auth;

public static class DevelopmentDataSeeder
{
    private static readonly (string Email, string Name, string Role, Guid Id)[] DevUsers =
    [
        ("admin@local.test", "Priya Sharma", "Admin", Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
        ("user@local.test", "Rahul Mehta", "User", Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
        ("ananya.iyer@acme.test", "Ananya Iyer", "User", Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc01")),
        ("vikram.singh@acme.test", "Vikram Singh", "User", Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc02")),
        ("neha.kapoor@acme.test", "Neha Kapoor", "User", Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc03")),
        ("arjun.desai@acme.test", "Arjun Desai", "User", Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc04")),
        ("sofia.martinez@acme.test", "Sofia Martinez", "User", Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc05")),
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
