using CCE.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace CCE.Api.Common.Results;

internal static class MessageTypeStatusCodes
{
    internal static int ToStatusCode(MessageType type) => type switch
    {
        MessageType.NotFound     => StatusCodes.Status404NotFound,
        MessageType.Validation   => StatusCodes.Status400BadRequest,
        MessageType.Conflict     => StatusCodes.Status409Conflict,
        MessageType.Unauthorized => StatusCodes.Status401Unauthorized,
        MessageType.Forbidden    => StatusCodes.Status403Forbidden,
        MessageType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        _                        => StatusCodes.Status500InternalServerError,
    };
}
