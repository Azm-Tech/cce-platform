using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Health;

public sealed class AuthenticatedHealthQueryHandler
    : IRequestHandler<AuthenticatedHealthQuery, AuthenticatedHealthResult>
{
    private readonly ISystemClock _clock;

    public AuthenticatedHealthQueryHandler(ISystemClock clock) => _clock = clock;

    public Task<AuthenticatedHealthResult> Handle(
        AuthenticatedHealthQuery request,
        CancellationToken cancellationToken)
    {
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "ar" : request.Locale;

        var result = new AuthenticatedHealthResult(
            Status: "ok",
            User: new AuthenticatedUserInfo(
                Id: request.UserId,
                PreferredUsername: request.PreferredUsername,
                Email: request.Email,
                Upn: request.Upn,
                Groups: request.Groups),
            Locale: locale,
            UtcNow: _clock.UtcNow);

        return Task.FromResult(result);
    }
}
