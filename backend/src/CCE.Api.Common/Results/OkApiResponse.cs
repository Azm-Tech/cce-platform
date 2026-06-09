using CCE.Application.Common;
using CCE.Domain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CCE.Api.Common.HttpResults;

/// <summary>
/// Typed <see cref="IResult"/> that wraps a <see cref="Response{T}"/> success envelope
/// and registers <c>200 OK</c> response metadata for Swashbuckle.
/// </summary>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Required by IEndpointMetadataProvider interface.")]
[SuppressMessage("Design", "CA1815:Override equals and operator equals on value types", Justification = "Result wrapper is never compared.")]
public readonly struct OkApiResponse<T> : IResult, IEndpointMetadataProvider
{
    private readonly Response<T> _payload;

    public OkApiResponse(Response<T> payload) => _payload = payload;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        var stamped = _payload with
        {
            TraceId = Activity.Current?.Id ?? string.Empty,
            Timestamp = DateTimeOffset.UtcNow,
        };

        if (stamped.Success)
        {
            return Results.Json(stamped, statusCode: StatusCodes.Status200OK).ExecuteAsync(httpContext);
        }

        var statusCode = stamped.Type switch
        {
            MessageType.NotFound => StatusCodes.Status404NotFound,
            MessageType.Validation => StatusCodes.Status400BadRequest,
            MessageType.Conflict => StatusCodes.Status409Conflict,
            MessageType.Unauthorized => StatusCodes.Status401Unauthorized,
            MessageType.Forbidden => StatusCodes.Status403Forbidden,
            MessageType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Json(stamped, statusCode: statusCode).ExecuteAsync(httpContext);
    }

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Response<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(Response<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status401Unauthorized, typeof(Response<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status403Forbidden, typeof(Response<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status404NotFound, typeof(Response<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status409Conflict, typeof(Response<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status422UnprocessableEntity, typeof(Response<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status500InternalServerError, typeof(Response<T>), ["application/json"]));
    }
}
