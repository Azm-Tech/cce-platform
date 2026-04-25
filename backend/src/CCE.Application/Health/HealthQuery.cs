using MediatR;

namespace CCE.Application.Health;

/// <summary>
/// Anonymous health query. Returns <see cref="HealthResult"/>.
/// Locale falls back to <c>"ar"</c> (Arabic, the default per spec) when null/empty.
/// </summary>
/// <param name="Locale">Requested locale (typically from <c>Accept-Language</c> header in the API).</param>
public sealed record HealthQuery(string? Locale) : IRequest<HealthResult>;
