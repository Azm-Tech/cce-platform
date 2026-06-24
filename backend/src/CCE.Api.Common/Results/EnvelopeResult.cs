using Microsoft.AspNetCore.Http;

namespace CCE.Api.Common.Results;

/// <summary>
/// <see cref="IResult"/> that writes a failure envelope with the correct HTTP status.
/// Used by <see cref="EnvelopeResults"/> factory methods for endpoints that need to return
/// an enveloped error without going through a MediatR handler.
/// </summary>
internal sealed class EnvelopeResult : IResult
{
    private readonly int _statusCode;
    private readonly string _domainKey;

    public EnvelopeResult(int statusCode, string domainKey)
    {
        _statusCode = statusCode;
        _domainKey = domainKey;
    }

    public Task ExecuteAsync(HttpContext httpContext)
        => EnvelopeWriter.WriteAsync(httpContext, _statusCode, _domainKey);
}
