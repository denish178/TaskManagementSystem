using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TaskManagement.Shared.RateLimiting;

public static class RateLimitingExtensions
{
    public const string LoginPolicy = "login";
    public const string ChatPolicy = "chat";

    public static IServiceCollection AddTaskManagementRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(LoginPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 15,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(ChatPolicy, context =>
            {
                var userId =
                    context.User?.FindFirst("sub")?.Value
                    ?? context.User?.FindFirst(
                        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    userId,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    });
            });
        });

        return services;
    }

    public static IApplicationBuilder UseTaskManagementRateLimiting(
        this IApplicationBuilder app)
    {
        return app.UseRateLimiter();
    }
}
