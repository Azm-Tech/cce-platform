using CCE.Application.Common;
using CCE.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace CCE.Api.Common.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// Maps a <see cref="Result{T}"/> to an <see cref="IResult"/> with the correct HTTP status.
    /// </summary>
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return successStatusCode switch
            {
                StatusCodes.Status201Created => Results.Created((string?)null, result),
                StatusCodes.Status204NoContent => Results.NoContent(),
                _ => Results.Json(result, statusCode: successStatusCode)
            };
        }

        var statusCode = result.Error!.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Json(result, statusCode: statusCode);
    }

    /// <summary>Shorthand for 201 Created.</summary>
    public static IResult ToCreatedHttpResult<T>(this Result<T> result)
        => result.ToHttpResult(StatusCodes.Status201Created);

    /// <summary>Shorthand for 204 NoContent (void commands).</summary>
    public static IResult ToNoContentHttpResult(this Result<Unit> result)
        => result.ToHttpResult(StatusCodes.Status204NoContent);
}
