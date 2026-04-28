namespace CCE.Application.Content;

public enum VirusScanResult
{
    Clean,
    Infected,
    ScanFailed,
}

public interface IClamAvScanner
{
    Task<VirusScanResult> ScanAsync(Stream content, CancellationToken ct);
}
