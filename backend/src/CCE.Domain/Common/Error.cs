using System.Text.Json.Serialization;

namespace CCE.Domain.Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    BusinessRule,
    Internal
}

public sealed record Error(
    string Code,
    string MessageAr,
    string MessageEn,
    ErrorType Type = ErrorType.Internal,
    IDictionary<string, string[]>? Details = null);
