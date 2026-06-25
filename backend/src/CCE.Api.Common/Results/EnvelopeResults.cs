using CCE.Application.Messages;
using Microsoft.AspNetCore.Http;

namespace CCE.Api.Common.Results;

/// <summary>
/// Enveloped equivalents of the raw <c>Results.Unauthorized()</c>, <c>Results.NotFound()</c>,
/// <c>Results.BadRequest()</c> helpers. Use in endpoints that short-circuit before reaching
/// a MediatR handler (e.g. when <c>ICurrentUserAccessor.GetUserId()</c> is empty).
/// </summary>
public static class EnvelopeResults
{
    /// <summary>401 Unauthorized — enveloped with ERR901 / UNAUTHORIZED_ACCESS.</summary>
    public static IResult Unauthorized()
        => new EnvelopeResult(StatusCodes.Status401Unauthorized, MessageKeys.General.UNAUTHORIZED);

    /// <summary>403 Forbidden — enveloped with ERR902 / FORBIDDEN_ACCESS.</summary>
    public static IResult Forbidden()
        => new EnvelopeResult(StatusCodes.Status403Forbidden, MessageKeys.General.FORBIDDEN);

    /// <summary>404 Not Found — enveloped with ERR903 / RESOURCE_NOT_FOUND_GENERIC by default.</summary>
    public static IResult NotFound(string domainKey = MessageKeys.General.RESOURCE_NOT_FOUND_GENERIC)
        => new EnvelopeResult(StatusCodes.Status404NotFound, domainKey);

    /// <summary>400 Bad Request — enveloped with ERR904 / BAD_REQUEST by default.</summary>
    public static IResult BadRequest(string domainKey = MessageKeys.General.BAD_REQUEST)
        => new EnvelopeResult(StatusCodes.Status400BadRequest, domainKey);

    /// <summary>409 Conflict — pass CONCURRENCY_CONFLICT or DUPLICATE_VALUE explicitly.</summary>
    public static IResult Conflict(string domainKey)
        => new EnvelopeResult(StatusCodes.Status409Conflict, domainKey);
}
