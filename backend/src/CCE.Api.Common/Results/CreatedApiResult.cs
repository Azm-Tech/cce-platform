using CCE.Application.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CCE.Api.Common.Results;

/// <summary>
/// Typed <see cref="IResult"/> that wraps a <see cref="Response{T}"/> created envelope
/// and registers <c>201 Created</c> response metadata for Swashbuckle.
/// </summary>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Required by IEndpointMetadataProvider interface.")]
[SuppressMessage("Design", "CA1815:Override equals and operator equals on value types", Justification = "Result wrapper is never compared.")]
public readonly struct CreatedApiResult<T> : IResult, IEndpointMetadataProvider
{
    private readonly Response<T> _payload;

    public CreatedApiResult(Response<T> payload) => _payload = payload;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        var correlationId = httpContext.Items.TryGetValue(Middleware.CorrelationIdMiddleware.ItemKey, out var cid)
            ? cid?.ToString() ?? string.Empty
            : string.Empty;

        var stamped = _payload with
        {
            TraceId = Activity.Current?.Id ?? string.Empty,
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.UtcNow,
        };

        if (stamped.Success)
            return TypedResults.Json(stamped, options: EnvelopeWriter.JsonOptions, statusCode: StatusCodes.Status201Created).ExecuteAsync(httpContext);

        return TypedResults.Json(stamped, options: EnvelopeWriter.JsonOptions, statusCode: MessageTypeStatusCodes.ToStatusCode(stamped.Type)).ExecuteAsync(httpContext);
    }

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status201Created, typeof(Response<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(Response<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status401Unauthorized, typeof(Response<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status403Forbidden, typeof(Response<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status404NotFound, typeof(Response<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status409Conflict, typeof(Response<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status422UnprocessableEntity, typeof(Response<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status500InternalServerError, typeof(Response<T>), ["application/json"]));
    }
}
