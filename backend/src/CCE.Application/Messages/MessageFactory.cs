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

    // ─── Convenience shortcuts (Identity domain) ───

    public Response<T> UserNotFound<T>()       => NotFound<T>(MessageKeys.Identity.USER_NOT_FOUND);
    public Response<T> InterestUpserted<T>(T data) => Ok(data, MessageKeys.Identity.INTEREST_UPSERTED);
    public Response<T> EmailExists<T>()        => Conflict<T>(MessageKeys.Identity.EMAIL_EXISTS);
    public Response<T> InvalidCredentials<T>() => Unauthorized<T>(MessageKeys.Identity.INVALID_CREDENTIALS);
    public Response<T> NotAuthenticated<T>()     => Unauthorized<T>(MessageKeys.Identity.NOT_AUTHENTICATED);
    public Response<T> AccountDeactivated<T>()   => Forbidden<T>(MessageKeys.Identity.ACCOUNT_DEACTIVATED);
    public Response<T> ContactNotVerified<T>()   => Forbidden<T>(MessageKeys.Identity.CONTACT_NOT_VERIFIED);

    // ─── Convenience shortcuts (Content domain) ───

    public Response<T> NewsNotFound<T>()      => NotFound<T>(MessageKeys.Content.NEWS_NOT_FOUND);
    public Response<T> EventNotFound<T>()     => NotFound<T>(MessageKeys.Content.EVENT_NOT_FOUND);
    public Response<T> ResourceNotFound<T>()  => NotFound<T>(MessageKeys.Content.RESOURCE_NOT_FOUND);
    public Response<T> PageNotFound<T>()      => NotFound<T>(MessageKeys.Content.PAGE_NOT_FOUND);
    public Response<T> TopicNotFound<T>()     => NotFound<T>(MessageKeys.Community.TOPIC_NOT_FOUND);
    public Response<T> CannotFollowSelf<T>()  => ValidationError<T>(
        MessageKeys.Community.CANNOT_FOLLOW_SELF,
        new[] { Field("userId", MessageKeys.Community.CANNOT_FOLLOW_SELF) });
    public Response<T> CategoryNotFound<T>()  => NotFound<T>(MessageKeys.Content.CATEGORY_NOT_FOUND);
    public Response<T> AssetNotFound<T>()     => NotFound<T>(MessageKeys.Content.ASSET_NOT_FOUND);
    public Response<T> AssetNotClean<T>()     => BusinessRule<T>(MessageKeys.Content.ASSET_NOT_CLEAN);

    // ─── Convenience shortcuts (Identity / Expert domain) ───

    public Response<T> ExpertRequestNotFound<T>() => NotFound<T>(MessageKeys.Identity.EXPERT_REQUEST_NOT_FOUND);

    // ─── Convenience shortcuts (Platform Settings domain) ───

    public Response<T> HomepageSettingsNotFound<T>()  => NotFound<T>(MessageKeys.PlatformSettings.HOMEPAGE_SETTINGS_NOT_FOUND);
    public Response<T> AboutSettingsNotFound<T>()     => NotFound<T>(MessageKeys.PlatformSettings.ABOUT_SETTINGS_NOT_FOUND);
    public Response<T> PoliciesSettingsNotFound<T>()  => NotFound<T>(MessageKeys.PlatformSettings.POLICIES_SETTINGS_NOT_FOUND);
    public Response<T> GlossaryEntryNotFound<T>()     => NotFound<T>(MessageKeys.PlatformSettings.GLOSSARY_ENTRY_NOT_FOUND);
    public Response<T> KnowledgePartnerNotFound<T>()  => NotFound<T>(MessageKeys.PlatformSettings.KNOWLEDGE_PARTNER_NOT_FOUND);
    public Response<T> PolicySectionNotFound<T>()     => NotFound<T>(MessageKeys.PlatformSettings.POLICY_SECTION_NOT_FOUND);
    public Response<T> ContentUpdateFailed<T>()       => BusinessRule<T>(MessageKeys.PlatformSettings.CONTENT_UPDATE_FAILED);

    // ─── Convenience shortcuts (Media domain) ───

    public Response<T> MediaFileNotFound<T>() => NotFound<T>(MessageKeys.Media.MEDIA_FILE_NOT_FOUND);
    public Response<T> InvalidFileType<T>()   => BusinessRule<T>(MessageKeys.Media.INVALID_FILE_TYPE);
    public Response<T> FileTooLarge<T>()      => BusinessRule<T>(MessageKeys.Media.FILE_TOO_LARGE);
    public Response<T> EmptyFile<T>()         => BusinessRule<T>(MessageKeys.Media.EMPTY_FILE);

    // ─── Convenience shortcuts (Verification domain) ───

    public Response<T> OtpNotFound<T>()          => NotFound<T>(MessageKeys.Verification.OTP_NOT_FOUND);
    public Response<T> OtpExpired<T>()           => BusinessRule<T>(MessageKeys.Verification.OTP_EXPIRED);
    public Response<T> OtpInvalidCode<T>()       => BusinessRule<T>(MessageKeys.Verification.OTP_INVALID_CODE);
    public Response<T> OtpMaxAttempts<T>()       => BusinessRule<T>(MessageKeys.Verification.OTP_MAX_ATTEMPTS);
    public Response<T> OtpCooldownActive<T>()    => BusinessRule<T>(MessageKeys.Verification.OTP_COOLDOWN_ACTIVE);
    public Response<T> OtpInvalidated<T>()       => BusinessRule<T>(MessageKeys.Verification.OTP_INVALIDATED);
    public Response<T> ContactAlreadyTaken<T>()  => Conflict<T>(MessageKeys.Verification.CONTACT_ALREADY_TAKEN);
    public Response<VoidData> EmailUpdated()     => Ok(MessageKeys.Verification.EMAIL_UPDATED);
    public Response<VoidData> PhoneUpdated()     => Ok(MessageKeys.Verification.PHONE_UPDATED);

    // ─── Convenience shortcuts (Country domain) ───

    public Response<T> CountryNotFound<T>()               => NotFound<T>(MessageKeys.Country.COUNTRY_NOT_FOUND);
    public Response<T> CountryProfileNotFound<T>()        => NotFound<T>(MessageKeys.Country.COUNTRY_PROFILE_NOT_FOUND);
    public Response<T> NoCountryAssigned<T>()             => NotFound<T>(MessageKeys.Country.NO_COUNTRY_ASSIGNED);
    public Response<T> CountryScopeForbidden<T>()         => Forbidden<T>(MessageKeys.Country.COUNTRY_SCOPE_FORBIDDEN);
    public Response<T> CountryContentRequestNotFound<T>()    => NotFound<T>(MessageKeys.Content.COUNTRY_RESOURCE_REQUEST_NOT_FOUND);
    public Response<T> CountryRequestProcessed<T>(T data)    => Ok(data, MessageKeys.Content.COUNTRY_REQUEST_PROCESSED);
    public Response<T> CountryRequestProcessingFailed<T>()   => BusinessRule<T>(MessageKeys.Content.COUNTRY_REQUEST_PROCESSING_FAILED);
    public Response<T> KapsarcDataUnavailable<T>()           => BusinessRule<T>(MessageKeys.Country.KAPSARC_DATA_UNAVAILABLE);
    public Response<T> KapsarcSnapshotRefreshed<T>(T data)   => Ok(data, MessageKeys.Country.KAPSARC_SNAPSHOT_REFRESHED);

    // ─── Convenience shortcuts (InteractiveMaps domain) ───

    public Response<T> MapNotFound<T>() => NotFound<T>(MessageKeys.InteractiveMaps.MAP_NOT_FOUND);
    public Response<VoidData> MapCreated() => Ok(MessageKeys.InteractiveMaps.MAP_CREATED);
    public Response<VoidData> MapUpdated() => Ok(MessageKeys.InteractiveMaps.MAP_UPDATED);
    public Response<VoidData> MapDeleted() => Ok(MessageKeys.InteractiveMaps.MAP_DELETED);
    public Response<T> NodeNotFound<T>() => NotFound<T>(MessageKeys.InteractiveMaps.NODE_NOT_FOUND);
    public Response<VoidData> NodeCreated() => Ok(MessageKeys.InteractiveMaps.NODE_CREATED);
    public Response<VoidData> NodeUpdated() => Ok(MessageKeys.InteractiveMaps.NODE_UPDATED);
    public Response<VoidData> NodeDeleted() => Ok(MessageKeys.InteractiveMaps.NODE_DELETED);

    // ─── Convenience shortcuts (Evaluation domain) ───

    public Response<VoidData> EvaluationSubmitted() => Ok(MessageKeys.Evaluation.EVALUATION_SUBMITTED);
    public Response<T> EvaluationNotFound<T>()      => NotFound<T>(MessageKeys.Evaluation.EVALUATION_NOT_FOUND);

    // ─── Convenience shortcuts (Notification domain) ───

    public Response<T> NotificationTemplateNotFound<T>()            => NotFound<T>(MessageKeys.Notifications.TEMPLATE_NOT_FOUND);
    public Response<T> NotificationLogNotFound<T>()                 => NotFound<T>(MessageKeys.Notifications.NOTIFICATION_NOT_FOUND);
    public Response<VoidData> NotificationSettingsUpdated()         => Ok(MessageKeys.Notifications.NOTIFICATION_SETTINGS_UPDATED);
    public Response<VoidData> NotificationMarkedRead()              => Ok(MessageKeys.Notifications.NOTIFICATION_MARKED_READ);
    public Response<int> NotificationsMarkedRead(int count)         => Ok(count, MessageKeys.Notifications.NOTIFICATIONS_MARKED_READ);
    public Response<T> NotificationRetried<T>(T data)               => Ok(data, MessageKeys.Notifications.NOTIFICATION_RETRIED);
    public Response<T> NotificationTemplateCreated<T>(T data)       => Ok(data, MessageKeys.Notifications.NOTIFICATION_TEMPLATE_CREATED);
    public Response<T> NotificationTemplateUpdated<T>(T data)       => Ok(data, MessageKeys.Notifications.NOTIFICATION_TEMPLATE_UPDATED);

    // ─── Convenience shortcuts (Lookups domain) ───

    public Response<T> CountryCodeNotFound<T>() => NotFound<T>(MessageKeys.Lookups.COUNTRY_CODE_NOT_FOUND);
    public Response<T> LookupCreated<T>(T data) => Ok(data, MessageKeys.Lookups.LOOKUP_CREATED);
    public Response<T> LookupUpdated<T>(T data) => Ok(data, MessageKeys.Lookups.LOOKUP_UPDATED);

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
