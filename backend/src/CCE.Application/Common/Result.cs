using CCE.Domain.Common;
using System.Text.Json.Serialization;

namespace CCE.Application.Common;

/// <summary>
/// Discriminated result type for handler returns. Replaces returning null (not-found)
/// and throwing exceptions for expected business failures.
/// Designed to serialize cleanly with System.Text.Json.
/// </summary>
public sealed record Result<T>
{
    [JsonInclude]
    public bool IsSuccess { get; private init; }

    [JsonInclude]
    public T? Data { get; private init; }

    [JsonInclude]
    public Error? Error { get; private init; }

    // Public parameterless constructor so System.Text.Json can instantiate
    // the record during serialization (records create temp instances).
    public Result() { }

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static Result<T> Failure(Error error) => new() { IsSuccess = false, Error = error };

    /// <summary>Allow implicit conversion from T for clean handler returns.</summary>
    public static implicit operator Result<T>(T data) => Success(data);

    /// <summary>Allow implicit conversion from Error for clean handler returns.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);
}

/// <summary>
/// Non-generic companion for void commands that return no data on success.
/// </summary>
public static class Result
{
    private static readonly Result<Unit> SuccessUnit = Result<Unit>.Success(Unit.Value);

    public static Result<Unit> Success() => SuccessUnit;
    public static Result<Unit> Failure(Error error) => Result<Unit>.Failure(error);
}

/// <summary>Unit type for commands that return no data.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = default;
}
