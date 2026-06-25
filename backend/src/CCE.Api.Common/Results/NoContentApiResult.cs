using CCE.Application.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CCE.Api.Common.Results;

/// <summary>
/// Typed <see cref="IResult"/> that wraps a <see cref="Response{VoidData}"/> no-content envelope
/// and registers <c>204 No Content</c> response metadata for Swashbuckle.
/// </summary>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Required by IEndpointMetadataProvider interface.")]
[SuppressMessage("Design", "CA1815:Override equals and operator equals on value types", Justification = "Result wrapper is never compared.")]
public readonly struct NoContentApiResult : IResult, IEndpointMetadataProvider
{
    private readonly Response<VoidData> _payload;

    public NoContentApiResult(Response<VoidData> payload) => _payload = payload;

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
            return TypedResults.NoContent().ExecuteAsync(httpContext);

        return TypedResults.Json(stamped, options: EnvelopeWriter.JsonOptions, statusCode: MessageTypeStatusCodes.ToStatusCode(stamped.Type)).ExecuteAsync(httpContext);
    }

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status204NoContent, typeof(void), []));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(Response<VoidData>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status401Unauthorized, typeof(Response<VoidData>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status403Forbidden, typeof(Response<VoidData>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status404NotFound, typeof(Response<VoidData>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status409Conflict, typeof(Response<VoidData>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status422UnprocessableEntity, typeof(Response<VoidData>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status500InternalServerError, typeof(Response<VoidData>), ["application/json"]));
    }
}
