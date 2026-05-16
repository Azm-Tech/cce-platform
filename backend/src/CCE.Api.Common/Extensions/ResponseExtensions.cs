using CCE.Application.Common;
using CCE.Domain.Common;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace CCE.Api.Common.Extensions;

public static class ResponseExtensions
{
    /// <summary>
    /// Maps a <see cref="Response{T}"/> to an <see cref="IResult"/> with correct HTTP status,
    /// injecting traceId and timestamp.
    /// </summary>
    public static IResult ToHttpResult<T>(this Response<T> response, int successStatusCode = StatusCodes.Status200OK)
    {
        var stamped = response with
        {
            TraceId = Activity.Current?.Id ?? string.Empty,
            Timestamp = DateTimeOffset.UtcNow,
        };

        if (stamped.Success)
        {
            return successStatusCode switch
            {
                StatusCodes.Status204NoContent => Results.NoContent(),
                _ => Results.Json(stamped, statusCode: successStatusCode),
            };
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

        return Results.Json(stamped, statusCode: statusCode);
    }

    public static IResult ToCreatedHttpResult<T>(this Response<T> response)
        => response.ToHttpResult(StatusCodes.Status201Created);

    public static IResult ToNoContentHttpResult(this Response<VoidData> response)
        => response.ToHttpResult(StatusCodes.Status204NoContent);
}
