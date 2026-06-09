using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CCE.Api.Common.Health;

/// <summary>
/// Lightweight readiness check for RabbitMQ: a short, bounded TCP connect to the broker host/port.
/// A TCP probe (rather than a full AMQP client handshake) is used deliberately so we don't pull a second
/// RabbitMQ.Client version into the build alongside MassTransit's, and so the check never blocks startup.
/// Registered only when <c>Messaging:Transport=RabbitMQ</c>.
/// </summary>
public sealed class RabbitMqTcpHealthCheck : IHealthCheck
{
    private readonly string _host;
    private readonly int _port;

    public RabbitMqTcpHealthCheck(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(_host, _port, cts.Token).ConfigureAwait(false);
            return client.Connected
                ? HealthCheckResult.Healthy($"RabbitMQ reachable at {_host}:{_port}.")
                : HealthCheckResult.Unhealthy($"RabbitMQ not reachable at {_host}:{_port}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"RabbitMQ not reachable at {_host}:{_port}.", ex);
        }
    }
}
