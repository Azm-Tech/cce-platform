using CCE.Application.Common;
using CCE.Application.Localization;
using CCE.Domain.Common;
using Microsoft.Extensions.Logging;

namespace CCE.Application.Messages;

/// <summary>
/// Factory for building <see cref="Response{T}"/> instances with localized messages.
/// Takes domain keys (e.g. "USER_NOT_FOUND"), resolves message in the request language
/// from Resources.yaml, and maps to system codes (e.g. "ERR001") via <see cref="SystemCodeMap"/>.
/// </summary>
public sealed class MessageFactory
{
    private readonly ILocalizationService _l;
    private readonly ILogger<MessageFactory> _logger;

    public MessageFactory(ILocalizationService l, ILogger<MessageFactory> logger)
    {
        _l = l;
        _logger = logger;
    }

    // ─── Success builders (domain key → CON0xx) ───

    public Response<T> Ok<T>(T data, string domainKey)
    {
        var code = ResolveCode(domainKey);
        var msg = Localize(domainKey);
        return Response<T>.Ok(data, code, msg);
    }

    public Response<VoidData> Ok(string domainKey)
    {
        var code = ResolveCode(domainKey);
        var msg = Localize(domainKey);
        return Response.Ok(code, msg);
    }

    // ─── Failure builders (domain key → ERR0xx) ───

    public Response<T> NotFound<T>(string domainKey)
        => Fail<T>(domainKey, MessageType.NotFound);

    public Response<T> Conflict<T>(string domainKey)
        => Fail<T>(domainKey, MessageType.Conflict);

    public Response<T> Unauthorized<T>(string domainKey)
        => Fail<T>(domainKey, MessageType.Unauthorized);

    public Response<T> Forbidden<T>(string domainKey)
        => Fail<T>(domainKey, MessageType.Forbidden);

    public Response<T> BusinessRule<T>(string domainKey)
        => Fail<T>(domainKey, MessageType.BusinessRule);

    // For domain-level validation that produces named field errors (e.g. business rules on
    // a multi-field object). FluentValidation schema failures go through ExceptionHandlingMiddleware
    // instead and never reach this overload.
    public Response<T> ValidationError<T>(
        string domainKey, IReadOnlyList<FieldError> fieldErrors)
    {
        var code = ResolveCode(domainKey);
        var msg = Localize(domainKey);
        return Response<T>.Fail(code, msg, MessageType.Validation, fieldErrors);
    }

    // ─── Build FieldError with localization (domain key → VAL0xx) ───

    public FieldError Field(string fieldName, string domainKey)
    {
        var code = ResolveCode(domainKey);
        var msg = Localize(domainKey);
        return new FieldError(fieldName, code, msg);
    }

    // ─── Private ───

    private Response<T> Fail<T>(string domainKey, MessageType type)
    {
        var code = ResolveCode(domainKey);
        var msg = Localize(domainKey);
        return Response<T>.Fail(code, msg, type);
    }

    private string ResolveCode(string domainKey)
    {
        var code = SystemCodeMap.ToSystemCode(domainKey);
        if (code == SystemCode.ERR900 && domainKey != MessageKeys.General.INTERNAL_ERROR)
            _logger.LogWarning("Domain key {DomainKey} has no SystemCodeMap entry and fell back to ERR900", domainKey);
        return code;
    }

    private string Localize(string domainKey)
    {
        var result = _l.GetString(domainKey);
        if (result == domainKey)
            _logger.LogWarning("Domain key {DomainKey} has no translation in Resources.yaml", domainKey);
        return result;
    }
}
