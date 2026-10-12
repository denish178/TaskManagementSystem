using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TaskManagement.Shared.Auth;

namespace TaskManagement.Shared.Tests;

public class CurrentUserTests
{
    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "TestAuth"));

    [Fact]
    public void GetUserId_FromNameIdentifier_ReturnsGuid()
    {
        var id = Guid.NewGuid();
        var user = Principal(new Claim(ClaimTypes.NameIdentifier, id.ToString()));

        Assert.Equal(id, CurrentUser.GetUserId(user));
    }

    [Fact]
    public void GetUserId_FromSubClaim_ReturnsGuid()
    {
        var id = Guid.NewGuid();
        var user = Principal(new Claim(JwtRegisteredClaimNames.Sub, id.ToString()));

        Assert.Equal(id, CurrentUser.GetUserId(user));
    }

    [Fact]
    public void GetUserId_MissingClaim_Throws()
    {
        var user = Principal();
        Assert.Throws<UnauthorizedAccessException>(() => CurrentUser.GetUserId(user));
    }

    [Fact]
    public void IsAdmin_WhenInRole_ReturnsTrue()
    {
        var user = Principal(new Claim(ClaimTypes.Role, "Admin"));
        Assert.True(CurrentUser.IsAdmin(user));
    }

    [Fact]
    public void GetEmail_ReturnsClaimValue()
    {
        var user = Principal(new Claim(ClaimTypes.Email, "a@b.com"));
        Assert.Equal("a@b.com", CurrentUser.GetEmail(user));
    }
}
