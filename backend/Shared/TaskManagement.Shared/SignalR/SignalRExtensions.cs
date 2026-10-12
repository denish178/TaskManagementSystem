using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace TaskManagement.Shared.SignalR;

public static class SignalRExtensions
{
    public static IServiceCollection AddTaskManagementSignalR(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var builder = services.AddSignalR();

        var redisConfiguration =
            configuration["SignalR:Redis:Configuration"];

        if (!string.IsNullOrWhiteSpace(redisConfiguration))
        {
            var prefix =
                configuration["SignalR:Redis:ChannelPrefix"] ?? "TMS";
            builder.AddStackExchangeRedis(redisConfiguration, options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal(prefix);
            });
        }

        return services;
    }
}
