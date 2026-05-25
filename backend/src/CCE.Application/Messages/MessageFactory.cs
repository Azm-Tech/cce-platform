using CCE.Application.Common;
using CCE.Application.Localization;
using CCE.Domain.Common;

namespace CCE.Application.Messages;

/// <summary>
/// Factory for building <see cref="Response{T}"/> instances with localized messages.
/// Takes domain keys (e.g. "USER_NOT_FOUND"), resolves message in the request language
/// from Resources.yaml, and maps to system codes (e.g. "ERR001") via <see cref="SystemCodeMap"/>.
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

    // ─── Convenience shortcuts (Platform Settings domain) ───

    public Response<T> HomepageSettingsNotFound<T>()  => NotFound<T>("HOMEPAGE_SETTINGS_NOT_FOUND");
    public Response<T> AboutSettingsNotFound<T>()     => NotFound<T>("ABOUT_SETTINGS_NOT_FOUND");
    public Response<T> PoliciesSettingsNotFound<T>()  => NotFound<T>("POLICIES_SETTINGS_NOT_FOUND");
    public Response<T> GlossaryEntryNotFound<T>()     => NotFound<T>("GLOSSARY_ENTRY_NOT_FOUND");
    public Response<T> KnowledgePartnerNotFound<T>()  => NotFound<T>("KNOWLEDGE_PARTNER_NOT_FOUND");
    public Response<T> PolicySectionNotFound<T>()     => NotFound<T>("POLICY_SECTION_NOT_FOUND");
    public Response<T> FaqNotFound<T>()               => NotFound<T>("FAQ_NOT_FOUND");
    public Response<T> ContentUpdateFailed<T>()       => BusinessRule<T>("CONTENT_UPDATE_FAILED");

    // ─── Convenience shortcuts (Media domain) ───

    public Response<T> MediaFileNotFound<T>() => NotFound<T>("MEDIA_FILE_NOT_FOUND");
    public Response<T> InvalidFileType<T>()   => BusinessRule<T>("INVALID_FILE_TYPE");
    public Response<T> FileTooLarge<T>()      => BusinessRule<T>("FILE_TOO_LARGE");
    public Response<T> EmptyFile<T>()         => BusinessRule<T>("EMPTY_FILE");

    // ─── Convenience shortcuts (Verification domain) ───

    public Response<T> OtpNotFound<T>()        => NotFound<T>("OTP_NOT_FOUND");
    public Response<T> OtpExpired<T>()         => BusinessRule<T>("OTP_EXPIRED");
    public Response<T> OtpInvalidCode<T>()     => BusinessRule<T>("OTP_INVALID_CODE");
    public Response<T> OtpMaxAttempts<T>()     => BusinessRule<T>("OTP_MAX_ATTEMPTS");
    public Response<T> OtpCooldownActive<T>()  => BusinessRule<T>("OTP_COOLDOWN_ACTIVE");
    public Response<T> OtpInvalidated<T>()     => BusinessRule<T>("OTP_INVALIDATED");

    // ─── Convenience shortcuts (Notification domain) ───

    public Response<T> NotificationTemplateNotFound<T>() => NotFound<T>("TEMPLATE_NOT_FOUND");
    public Response<T> NotificationLogNotFound<T>()    => NotFound<T>("NOTIFICATION_NOT_FOUND");
    public Response<VoidData> NotificationSettingsUpdated() => Ok("NOTIFICATION_SETTINGS_UPDATED");
    public Response<VoidData> NotificationMarkedRead()       => Ok("NOTIFICATION_MARKED_READ");
    public Response<int> NotificationsMarkedRead(int count)  => Ok(count, "NOTIFICATIONS_MARKED_READ");
    public Response<T> NotificationRetried<T>(T data)      => Ok(data, "NOTIFICATION_RETRIED");
    public Response<T> NotificationTemplateCreated<T>(T data) => Ok(data, "NOTIFICATION_TEMPLATE_CREATED");
    public Response<T> NotificationTemplateUpdated<T>(T data) => Ok(data, "NOTIFICATION_TEMPLATE_UPDATED");

    // ─── Private ───

    private Response<T> Fail<T>(string domainKey, MessageType type)
    {
        var code = SystemCodeMap.ToSystemCode(domainKey);
        var msg = Localize(domainKey);
        return Response<T>.Fail(code, msg, type);
    }

    private string Localize(string domainKey) => _l.GetString(domainKey);
}
