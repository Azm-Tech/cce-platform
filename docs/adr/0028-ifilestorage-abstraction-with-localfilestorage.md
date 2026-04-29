# ADR-0028: IFileStorage abstraction with LocalFileStorage

**Status:** Accepted
**Date:** 2026-04-29
**Sub-project:** 03 — Internal API

## Context

Admin users upload binary assets (images, PDFs, documents) through the Internal API. The upload path must support:
- Local development without cloud credentials.
- Test isolation (no real disk writes in unit tests).
- A future swap to cloud blob storage (S3 or Azure Blob) in Sub-project 8 without touching application-layer code.

Additionally, all uploaded content must be scanned for malware before being made available.

## Decision

An `IFileStorage` interface is defined in `CCE.Application.Common.Interfaces` with three operations:

```csharp
Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct);
Task<Stream> OpenReadAsync(string key, CancellationToken ct);
Task DeleteAsync(string key, CancellationToken ct);
```

The `key` returned by `SaveAsync` is a stable, path-like identifier (e.g. `assets/2026/04/29/<guid>-filename.pdf`). Callers store this key in the `AssetFile.StoragePath` column.

`LocalFileStorage` (registered in `CCE.Infrastructure` when `FileStorage:Provider == "local"`) writes to the directory specified by `FileStorage:LocalUploadsRoot` (defaults to `./uploads` in dev). The directory is created on startup if absent.

Virus scanning is handled by `IClamAvScanner`, a separate interface with a single `ScanAsync(Stream, ct)` method that connects to ClamAV via TCP (`ClamAv:Host`, `ClamAv:Port`). The asset upload command calls `IClamAvScanner.ScanAsync` before `IFileStorage.SaveAsync`. If the scan returns `Infected`, the command throws a `FileInfectedException` and the upload is rejected with HTTP 422.

## Consequences

- ✅ Application layer is fully decoupled from storage mechanism — handlers depend only on `IFileStorage`.
- ✅ In unit tests, `IFileStorage` is substituted with NSubstitute; no disk I/O required.
- ✅ Sub-project 8 can register `S3FileStorage` or `AzureBlobFileStorage` by swapping the Infrastructure registration with zero application-layer changes.
- ✅ Virus scan happens synchronously at upload time — infected files never reach storage.
- ⚠ `LocalFileStorage` writes to the local filesystem; uploaded files are not replicated and will be lost if the dev container is recreated. This is acceptable for development only.
- ⚠ Synchronous ClamAV TCP scan adds latency proportional to file size. Large uploads (>50 MB) may approach the default HTTP timeout. A streaming scan or async queue may be warranted in Sub-project 8.
