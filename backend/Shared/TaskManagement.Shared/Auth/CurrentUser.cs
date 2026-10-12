using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TaskManagement.Shared.Auth;

public static class CurrentUser
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var id =
            user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue("sub");

        if (!Guid.TryParse(id, out var userId))
        {
            throw new UnauthorizedAccessException("User ID claim is missing or invalid.");
        }

        return userId;
    }

    public static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole("Admin");

    public static string? GetEmail(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email)
        ?? user.FindFirstValue(JwtRegisteredClaimNames.Email);

    public static string? GetName(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name);

    public static string? GetRole(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role);
}
