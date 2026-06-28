using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CCE.Api.Common.SignalR;

/// <summary>
/// Registers SignalR with a Redis backplane so hub messages fan out across every process that shares the
/// broker — both API instances and <c>CCE.Worker</c> (which publishes notifications but serves no clients).
/// Without the backplane a message published in one process only reaches clients connected to that same
/// process.
///
/// <para>The backplane reuses <c>Infrastructure:RedisConnectionString</c> and the project's existing
/// connection parsing (<c>AbortOnConnectFail=false</c>, so a Redis outage degrades rather than crashes).
/// When no Redis connection is configured, SignalR runs in-process (fine for single-process dev/tests).</para>
/// </summary>
public static class SignalRRegistration
{
    public static IServiceCollection AddCceSignalR(this IServiceCollection services, IConfiguration configuration)
    {
        var builder = services.AddSignalR()
            .AddJsonProtocol(o =>
                o.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

        var redisConnectionString = configuration["Infrastructure:RedisConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            var channelPrefix = configuration["SignalR:ChannelPrefix"] ?? "cce-signalr";
            builder.AddStackExchangeRedis(options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal(channelPrefix);
                // Build the multiplexer the same way as the cache/output-cache path so the rediss:// URI
                // parses identically and a startup outage doesn't take the host down.
                options.ConnectionFactory = async writer =>
                {
                    var config = ConfigurationOptions.Parse(redisConnectionString);
                    config.AbortOnConnectFail = false;
                    return await ConnectionMultiplexer.ConnectAsync(config, writer).ConfigureAwait(false);
                };
            });
        }

        return services;
    }
}
