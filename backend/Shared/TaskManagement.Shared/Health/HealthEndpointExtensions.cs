using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace TaskManagement.Shared.Health;

public static class HealthEndpointExtensions
{
    public static IEndpointRouteBuilder MapTaskManagementHealth(
        this IEndpointRouteBuilder endpoints,
        string serviceName)
    {
        endpoints.MapGet(
            "/health",
            () => Results.Json(new { status = "healthy", service = serviceName }));

        return endpoints;
    }
}
