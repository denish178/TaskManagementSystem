using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManagement.Auth.Data;
using TaskManagement.Auth.DTOs;
using TaskManagement.Auth.Models;

namespace TaskManagement.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    private readonly IConfiguration _configuration;

    public AuthController(
        AuthDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<User>();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            return Conflict(new
            {
                message = "Email is already registered."
            });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Created("", new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured.");

        var jwtIssuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer is not configured.");

        var jwtAudience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT Audience is not configured.");

        var expiresInMinutes = int.Parse(
            _configuration["Jwt:ExpiresInMinutes"] ?? "60");

        var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, user.Email),
        new(ClaimTypes.Name, user.Name),
        new(ClaimTypes.Role, user.Role)
    };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return Ok(new
        {
            token = tokenString,
            user = new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                role = user.Role
            }
        });
    }

    [HttpGet("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(
    ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new
            {
                message = "Invalid user identity."
            });
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user ID."
            });
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        return Ok(new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role,
            createdAt = user.CreatedAt
        });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);

        if (user is null)
            return NotFound(new { message = "User not found." });

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email && u.Id != userId);

        if (emailExists)
            return Conflict(new { message = "Email is already in use." });

        user.Name = request.Name;
        user.Email = request.Email;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role
        });
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);

        if (user is null)
            return NotFound(new { message = "User not found." });

        var passwordHasher = new PasswordHasher<User>();

        var passwordResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.CurrentPassword);

        if (passwordResult == PasswordVerificationResult.Failed)
            return BadRequest(new { message = "Current password is incorrect." });

        if (request.NewPassword != request.ConfirmNewPassword)
            return BadRequest(new { message = "New passwords do not match." });

        user.PasswordHash = passwordHasher.HashPassword(
            user,
            request.NewPassword);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Password changed successfully."
        });
    }

    [HttpGet("users/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return NotFound(new { message = "User not found." });

        return Ok(new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role
        });
    }

    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .AsNoTracking()
            .Select(user => new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                role = user.Role
            })
            .ToListAsync();

        return Ok(users);
    }
}