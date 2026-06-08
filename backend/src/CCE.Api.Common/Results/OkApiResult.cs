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
/// Typed <see cref="IResult"/> that wraps a <see cref="Result{T}"/> success envelope
/// and registers <c>200 OK</c> response metadata for Swashbuckle.
/// </summary>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Required by IEndpointMetadataProvider interface.")]
[SuppressMessage("Design", "CA1815:Override equals and operator equals on value types", Justification = "Result wrapper is never compared.")]
public readonly struct OkApiResult<T> : IResult, IEndpointMetadataProvider
{
    private readonly Result<T> _payload;

    public OkApiResult(Result<T> payload) => _payload = payload;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        if (_payload.IsSuccess)
        {
            return Results.Json(_payload, statusCode: StatusCodes.Status200OK).ExecuteAsync(httpContext);
        }

        var statusCode = _payload.Error!.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Json(_payload, statusCode: statusCode).ExecuteAsync(httpContext);
    }

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Result<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(Result<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status401Unauthorized, typeof(Result<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status403Forbidden, typeof(Result<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status404NotFound, typeof(Result<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status409Conflict, typeof(Result<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status422UnprocessableEntity, typeof(Result<T>), ["application/json"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status500InternalServerError, typeof(Result<T>), ["application/json"]));
    }
}
