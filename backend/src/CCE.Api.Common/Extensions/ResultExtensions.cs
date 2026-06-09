using CCE.Api.Common.HttpResults;
using CCE.Application.Common;
using CCE.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace CCE.Api.Common.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// Maps a <see cref="Result{T}"/> to a typed <see cref="IResult"/> with the correct HTTP status
    /// and registers Swashbuckle metadata.
    /// </summary>
    public static OkApiResult<T> ToHttpResult<T>(this Result<T> result)
        => new(result);

    /// <summary>Shorthand for 201 Created with Swashbuckle metadata.</summary>
    public static CreatedApiResult<T> ToCreatedHttpResult<T>(this Result<T> result)
        => new(result);

    /// <summary>Shorthand for 204 No Content (void commands) with Swashbuckle metadata.</summary>
    public static NoContentApiResult ToNoContentHttpResult(this Result<Unit> result)
        => new(result);
}
