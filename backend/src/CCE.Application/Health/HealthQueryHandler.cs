using CCE.Domain.Common;
using MediatR;
using System.Reflection;

namespace CCE.Application.Health;

public sealed class HealthQueryHandler : IRequestHandler<HealthQuery, HealthResult>
{
    private readonly ISystemClock _clock;

    public HealthQueryHandler(ISystemClock clock) => _clock = clock;

    public Task<HealthResult> Handle(HealthQuery request, CancellationToken cancellationToken)
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "0.0.0";

        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "ar" : request.Locale;

        var result = new HealthResult(
            Status: "ok",
            Version: version,
            Locale: locale,
            UtcNow: _clock.UtcNow);

        return Task.FromResult(result);
    }
}
