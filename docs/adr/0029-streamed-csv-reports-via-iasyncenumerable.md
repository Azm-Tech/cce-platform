# ADR-0029: Streamed CSV reports via IAsyncEnumerable<T>

**Status:** Accepted
**Date:** 2026-04-29
**Sub-project:** 03 — Internal API

## Context

The Internal API exposes 8 admin report endpoints (e.g. `/api/admin/reports/users-registrations.csv`). Reports can span the full lifetime of the platform, potentially returning tens of thousands of rows. Loading the entire result set into memory before streaming the HTTP response body would cause unacceptable peak memory consumption on the API server and long time-to-first-byte for the client.

MediatR's standard `IRequest<TResponse>` pattern buffers `TResponse` in memory before returning it to the endpoint. This is incompatible with row-by-row streaming.

## Decision

Each report is backed by a dedicated Application-layer service (not a MediatR handler) that exposes:

```csharp
IAsyncEnumerable<TRow> QueryAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
```

The endpoint maps this directly to an HTTP response using a custom `CsvStreamResult` helper that:
1. Sets `Content-Type: text/csv; charset=utf-8`.
2. Writes a UTF-8 BOM (`0xEF 0xBB 0xBF`) as the first three bytes so Excel on Windows auto-detects the encoding without a manual import wizard.
3. Uses **CsvHelper** (`CsvWriter`) to serialize each `TRow` as it is yielded by the `IAsyncEnumerable`, writing directly to `HttpContext.Response.Body` without any intermediate buffer.

Column names are controlled by `[Name("…")]` attributes on the `TRow` record, keeping the mapping close to the data contract.

MediatR is deliberately bypassed for these endpoints. The service is resolved directly from `HttpContext.RequestServices` (or via constructor injection in the endpoint delegate). This is explicitly noted in each endpoint file.

## Consequences

- ✅ Memory usage per request is O(1) — only one `TRow` and its CSV bytes are in memory at a time.
- ✅ Time-to-first-byte is minimal; the client can begin saving/parsing before the server finishes querying.
- ✅ CsvHelper handles quoting, escaping, and Unicode correctly, including multi-byte characters in Arabic content.
- ✅ UTF-8 BOM ensures Excel compatibility without manual import steps.
- ⚠ Bypassing MediatR means these code paths skip any MediatR pipeline behaviors (e.g. logging, validation). Report services must be individually tested and must not modify state.
- ⚠ If the DB connection drops mid-stream, the HTTP response headers have already been sent (200 OK). The client receives a truncated CSV. A `Content-Length` header cannot be set in advance. This is a known limitation of streaming responses; clients should validate row counts against a separate count endpoint if data integrity is critical.
