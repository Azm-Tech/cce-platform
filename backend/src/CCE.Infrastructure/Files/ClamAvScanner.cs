using System.Net.Sockets;
using System.Text;
using CCE.Application.Content;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Files;

/// <summary>
/// ClamAV TCP INSTREAM client. Implements the protocol manually to avoid an external dep.
/// Protocol (per https://docs.clamav.net/manual/Usage/Scanning.html#instream):
/// 1. Send <c>zINSTREAM\0</c>.
/// 2. Send framed chunks: 4-byte big-endian length + bytes.
/// 3. Send 4-byte zero terminator.
/// 4. Read response (line of text).
///    - Contains <c>OK</c> → Clean.
///    - Contains <c>FOUND</c> → Infected.
///    - Anything else (or transport error) → ScanFailed.
/// </summary>
public sealed class ClamAvScanner : IClamAvScanner
{
    private static readonly TimeSpan IoTimeout = TimeSpan.FromSeconds(30);
    private const int ChunkSize = 8192;

    private readonly string _host;
    private readonly int _port;
    private readonly ILogger<ClamAvScanner> _logger;

    public ClamAvScanner(IOptions<CceInfrastructureOptions> options, ILogger<ClamAvScanner> logger)
    {
        _host = options.Value.ClamAvHost;
        _port = options.Value.ClamAvPort;
        _logger = logger;
    }

    public async Task<VirusScanResult> ScanAsync(Stream content, CancellationToken ct)
    {
        try
        {
            using var tcp = new TcpClient { ReceiveTimeout = (int)IoTimeout.TotalMilliseconds, SendTimeout = (int)IoTimeout.TotalMilliseconds };
            await tcp.ConnectAsync(_host, _port, ct).ConfigureAwait(false);
            using var stream = tcp.GetStream();

            // Step 1: zINSTREAM\0
            var preamble = Encoding.ASCII.GetBytes("zINSTREAM\0");
            await stream.WriteAsync(preamble, ct).ConfigureAwait(false);

            // Step 2: chunks
            var buffer = new byte[ChunkSize];
            var lengthFrame = new byte[4];
            int read;
            while ((read = await content.ReadAsync(buffer.AsMemory(0, ChunkSize), ct).ConfigureAwait(false)) > 0)
            {
                BigEndianWriteInt32(lengthFrame, read);
                await stream.WriteAsync(lengthFrame, ct).ConfigureAwait(false);
                await stream.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            }

            // Step 3: zero-length terminator
            BigEndianWriteInt32(lengthFrame, 0);
            await stream.WriteAsync(lengthFrame, ct).ConfigureAwait(false);

            // Step 4: read response
            var responseBuffer = new byte[1024];
            var responseLength = await stream.ReadAsync(responseBuffer, ct).ConfigureAwait(false);
            var response = Encoding.ASCII.GetString(responseBuffer, 0, responseLength).Trim('\0', ' ', '\n', '\r');

            if (response.Contains("OK", System.StringComparison.Ordinal) && !response.Contains("FOUND", System.StringComparison.Ordinal))
            {
                return VirusScanResult.Clean;
            }
            if (response.Contains("FOUND", System.StringComparison.Ordinal))
            {
                _logger.LogWarning("ClamAV reported infection: {Response}", response);
                return VirusScanResult.Infected;
            }
            _logger.LogWarning("ClamAV returned unexpected response: {Response}", response);
            return VirusScanResult.ScanFailed;
        }
        catch (System.Exception ex) when (ex is SocketException or System.IO.IOException or System.OperationCanceledException)
        {
            _logger.LogError(ex, "ClamAV scan failed (transport error)");
            return VirusScanResult.ScanFailed;
        }
    }

    private static void BigEndianWriteInt32(byte[] dest, int value)
    {
        dest[0] = (byte)((value >> 24) & 0xFF);
        dest[1] = (byte)((value >> 16) & 0xFF);
        dest[2] = (byte)((value >> 8) & 0xFF);
        dest[3] = (byte)(value & 0xFF);
    }
}
