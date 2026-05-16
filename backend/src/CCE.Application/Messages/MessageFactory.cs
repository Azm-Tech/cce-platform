using CCE.Application.Common;
using CCE.Application.Localization;
using CCE.Domain.Common;

namespace CCE.Application.Messages;

/// <summary>
/// Factory for building <see cref="Response{T}"/> instances with localized messages.
/// Takes domain keys (e.g. "USER_NOT_FOUND"), resolves bilingual message from Resources.yaml,
/// and maps to system codes (e.g. "ERR001") via <see cref="SystemCodeMap"/>.
/// </summary>
public sealed class MessageFactory
{
    private readonly ILocalizationService _l;

    public MessageFactory(ILocalizationService l) => _l = l;

    // ─── Success builders (domain key → CON0xx) ───

    public Response<T> Ok<T>(T data, string domainKey)
    {
        var code = SystemCodeMap.ToSystemCode(domainKey);
        var msg = Localize(domainKey);
        return Response<T>.Ok(data, code, msg);
    }

    public Response<VoidData> Ok(string domainKey)
    {
        var code = SystemCodeMap.ToSystemCode(domainKey);
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

    public Response<T> ValidationError<T>(
        string domainKey, IReadOnlyList<FieldError> fieldErrors)
    {
        var code = SystemCodeMap.ToSystemCode(domainKey);
        var msg = Localize(domainKey);
        return Response<T>.Fail(code, msg, MessageType.Validation, fieldErrors);
    }

    // ─── Build FieldError with localization (domain key → VAL0xx) ───

    public FieldError Field(string fieldName, string domainKey)
    {
        var code = SystemCodeMap.ToSystemCode(domainKey);
        var msg = Localize(domainKey);
        return new FieldError(fieldName, code, msg);
    }

    // ─── Convenience shortcuts (Identity domain) ───

    public Response<T> UserNotFound<T>()      => NotFound<T>("USER_NOT_FOUND");
    public Response<T> EmailExists<T>()       => Conflict<T>("EMAIL_EXISTS");
    public Response<T> InvalidCredentials<T>() => Unauthorized<T>("INVALID_CREDENTIALS");
    public Response<T> NotAuthenticated<T>()  => Unauthorized<T>("NOT_AUTHENTICATED");

    // ─── Convenience shortcuts (Content domain) ───

    public Response<T> NewsNotFound<T>()      => NotFound<T>("NEWS_NOT_FOUND");
    public Response<T> EventNotFound<T>()     => NotFound<T>("EVENT_NOT_FOUND");
    public Response<T> PageNotFound<T>()      => NotFound<T>("PAGE_NOT_FOUND");
    public Response<T> CategoryNotFound<T>()  => NotFound<T>("CATEGORY_NOT_FOUND");

    // ─── Private ───

    private Response<T> Fail<T>(string domainKey, MessageType type)
    {
        var code = SystemCodeMap.ToSystemCode(domainKey);
        var msg = Localize(domainKey);
        return Response<T>.Fail(code, msg, type);
    }

    private LocalizedMessage Localize(string domainKey)
    {
        var raw = _l.GetLocalizedMessage(domainKey);
        return new LocalizedMessage(raw.Ar, raw.En);
    }
}
