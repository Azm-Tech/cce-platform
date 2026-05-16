using CCE.Application.Localization;
using CCE.Domain.Common;
using System.Text.Json.Serialization;

namespace CCE.Application.Common;

/// <summary>
/// Unified API response envelope. Every endpoint returns this shape.
/// Replaces <see cref="Result{T}"/> with proper success messages and error arrays.
/// Code field uses ERR0xx/CON0xx/VAL0xx numbering.
/// </summary>
public sealed record Response<T>
{
    [JsonInclude] public bool Success { get; private init; }
    [JsonInclude] public string Code { get; private init; } = string.Empty;
    [JsonInclude] public LocalizedMessage Message { get; private init; } = new("", "");
    [JsonInclude] public T? Data { get; private init; }
    [JsonInclude] public IReadOnlyList<FieldError> Errors { get; private init; } = [];
    [JsonInclude] public string TraceId { get; init; } = string.Empty;
    [JsonInclude] public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Not serialized — used internally to select HTTP status.</summary>
    [JsonIgnore] public MessageType Type { get; private init; } = MessageType.Success;

    public Response() { }

    // ─── Success Factories ───

    public static Response<T> Ok(T data, string code, LocalizedMessage message) => new()
    {
        Success = true,
        Code = code,
        Message = message,
        Data = data,
        Type = MessageType.Success,
    };

    /// <summary>Shorthand for void commands that return no data.</summary>
    public static Response<VoidData> Ok(string code, LocalizedMessage message) => new()
    {
        Success = true,
        Code = code,
        Message = message,
        Data = VoidData.Instance,
        Type = MessageType.Success,
    };

    // ─── Failure Factories ───

    public static Response<T> Fail(string code, LocalizedMessage message, MessageType type) => new()
    {
        Success = false,
        Code = code,
        Message = message,
        Type = type,
    };

    public static Response<T> Fail(
        string code, LocalizedMessage message, MessageType type, IReadOnlyList<FieldError> errors) => new()
    {
        Success = false,
        Code = code,
        Message = message,
        Type = type,
        Errors = errors,
    };
}

/// <summary>Placeholder type for commands that return no data.</summary>
public sealed record VoidData
{
    public static readonly VoidData Instance = new();
    private VoidData() { }
}

/// <summary>Non-generic companion for void commands.</summary>
public static class Response
{
    public static Response<VoidData> Ok(string code, LocalizedMessage message)
        => Response<VoidData>.Ok(code, message);

    public static Response<VoidData> Fail(string code, LocalizedMessage message, MessageType type)
        => Response<VoidData>.Fail(code, message, type);
}
