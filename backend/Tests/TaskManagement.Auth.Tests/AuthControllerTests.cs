using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaskManagement.Auth.Controllers;
using TaskManagement.Auth.Data;
using TaskManagement.Auth.DTOs;
using TaskManagement.Auth.Models;

namespace TaskManagement.Auth.Tests;

public class AuthControllerTests
{
    private static IConfiguration CreateJwtConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "unit-test-jwt-signing-key-32-chars-min",
                    ["Jwt:Issuer"] = "TaskManagement.Auth",
                    ["Jwt:Audience"] = "TaskManagementSystem",
                    ["Jwt:ExpiresInMinutes"] = "60",
                })
            .Build();

    private static AuthDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuthDbContext(options);
    }

    private static AuthController CreateController(AuthDbContext context, ClaimsPrincipal? user = null)
    {
        var controller = new AuthController(context, CreateJwtConfig());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user ?? new ClaimsPrincipal(new ClaimsIdentity()),
            },
        };
        return controller;
    }

    private static async Task<User> SeedUserAsync(AuthDbContext context, string email = "user@test.com")
    {
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = email,
            Role = "User",
            CreatedAt = DateTime.UtcNow,
        };
        user.PasswordHash = hasher.HashPassword(user, "Password123!");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Register_NewEmail_ReturnsCreated()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.Register(new RegisterRequest
        {
            Name = "Alice",
            Email = "alice@test.com",
            Password = "Password123!",
        });

        var created = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(created.Value);
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        await using var context = CreateContext();
        await SeedUserAsync(context, "dup@test.com");
        var controller = CreateController(context);

        var result = await controller.Register(new RegisterRequest
        {
            Name = "Bob",
            Email = "dup@test.com",
            Password = "Password123!",
        });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        await using var context = CreateContext();
        await SeedUserAsync(context);
        var controller = CreateController(context);

        var result = await controller.Login(new LoginRequest
        {
            Email = "user@test.com",
            Password = "wrong-password",
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        await using var context = CreateContext();
        await SeedUserAsync(context);
        var controller = CreateController(context);

        var result = await controller.Login(new LoginRequest
        {
            Email = "user@test.com",
            Password = "Password123!",
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetProfile_AuthenticatedUser_ReturnsProfile()
    {
        await using var context = CreateContext();
        var user = await SeedUserAsync(context);

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())],
            "TestAuth");

        var controller = CreateController(
            context,
            new ClaimsPrincipal(identity));

        var result = await controller.GetProfile();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        var user = await SeedUserAsync(context);

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())],
            "TestAuth");

        var controller = CreateController(
            context,
            new ClaimsPrincipal(identity));

        var result = await controller.ChangePassword(new ChangePasswordRequest
        {
            CurrentPassword = "wrong",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!",
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
