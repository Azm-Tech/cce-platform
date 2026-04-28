using System.Net;
using System.Net.Sockets;
using System.Text;
using CCE.Application.Content;
using CCE.Infrastructure;
using CCE.Infrastructure.Files;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Tests.Files;

public class ClamAvScannerTests
{
    [Fact]
    public async Task Returns_Clean_on_OK_response()
    {
        var (sut, port) = StartStub("stream: OK\0");

        var result = await sut.ScanAsync(new MemoryStream(new byte[] { 1, 2, 3 }), CancellationToken.None);

        result.Should().Be(VirusScanResult.Clean);
        _ = port; // silence unused warning if any
    }

    [Fact]
    public async Task Returns_Infected_on_FOUND_response()
    {
        var (sut, _) = StartStub("stream: Eicar-Test-Signature FOUND\0");

        var result = await sut.ScanAsync(new MemoryStream(new byte[] { 1, 2, 3 }), CancellationToken.None);

        result.Should().Be(VirusScanResult.Infected);
    }

    [Fact]
    public async Task Returns_ScanFailed_on_unrecognized_response()
    {
        var (sut, _) = StartStub("ERROR something\0");

        var result = await sut.ScanAsync(new MemoryStream(new byte[] { 1, 2, 3 }), CancellationToken.None);

        result.Should().Be(VirusScanResult.ScanFailed);
    }

    private static (ClamAvScanner sut, int port) StartStub(string responseText)
    {
#pragma warning disable CA2000 // listener is disposed in the background Task.Run finally block
        var listener = new TcpListener(IPAddress.Loopback, 0);
#pragma warning restore CA2000
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        _ = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                using var stream = client.GetStream();
                var buffer = new byte[8192];
                // Drain the request until 4 zero bytes (terminator) seen.
                while (true)
                {
                    var read = await stream.ReadAsync(buffer).ConfigureAwait(false);
                    if (read == 0) break;
                    if (read >= 4 && buffer[read - 4] == 0 && buffer[read - 3] == 0 && buffer[read - 2] == 0 && buffer[read - 1] == 0)
                    {
                        break;
                    }
                }
                var resp = Encoding.ASCII.GetBytes(responseText);
                await stream.WriteAsync(resp).ConfigureAwait(false);
            }
            catch (SocketException) { /* stub tear-down */ }
            catch (IOException) { /* stub tear-down */ }
            finally
            {
                listener.Stop();
                listener.Dispose();
            }
        });

        var sut = new ClamAvScanner(
            Options.Create(new CceInfrastructureOptions
            {
                SqlConnectionString = "x", RedisConnectionString = "x",
                ClamAvHost = "127.0.0.1", ClamAvPort = port,
            }),
            NullLogger<ClamAvScanner>.Instance);

        return (sut, port);
    }
}
