using CCE.Api.Common.Results;
using CCE.Application.Common;
using CCE.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace CCE.Api.Common.Extensions;

public static class ResponseExtensions
{
    /// <summary>
    /// Maps a <see cref="Response{T}"/> to a typed <see cref="IResult"/> with correct HTTP status,
    /// injecting traceId and timestamp, and registering Swashbuckle metadata.
    /// </summary>
    public static OkApiResult<T> ToHttpResult<T>(this Response<T> response)
        => new(response);

    /// <summary>Shorthand for 201 Created with Swashbuckle metadata.</summary>
    public static CreatedApiResult<T> ToCreatedHttpResult<T>(this Response<T> response)
        => new(response);

    /// <summary>Shorthand for 204 No Content with Swashbuckle metadata.</summary>
    public static NoContentApiResult ToNoContentHttpResult(this Response<VoidData> response)
        => new(response);
}
