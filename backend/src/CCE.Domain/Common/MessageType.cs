using System.Text.Json.Serialization;

namespace CCE.Domain.Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageType
{
    Success,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    BusinessRule,
    Internal
}
