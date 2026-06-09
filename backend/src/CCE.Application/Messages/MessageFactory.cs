using CCE.Application.Common;
using CCE.Application.Errors;
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

    public Response<T> UserNotFound<T>()       => NotFound<T>(ApplicationErrors.Identity.USER_NOT_FOUND);
    public Response<T> InterestUpserted<T>(T data) => Ok(data, "INTEREST_UPSERTED");
    public Response<T> EmailExists<T>()        => Conflict<T>(ApplicationErrors.Identity.EMAIL_EXISTS);
    public Response<T> InvalidCredentials<T>() => Unauthorized<T>(ApplicationErrors.Identity.INVALID_CREDENTIALS);
    public Response<T> NotAuthenticated<T>()   => Unauthorized<T>(ApplicationErrors.Identity.NOT_AUTHENTICATED);
    public Response<T> AccountDeactivated<T>() => Forbidden<T>(ApplicationErrors.Identity.ACCOUNT_DEACTIVATED);

    // ─── Convenience shortcuts (Content domain) ───

    public Response<T> NewsNotFound<T>()      => NotFound<T>("NEWS_NOT_FOUND");
    public Response<T> EventNotFound<T>()     => NotFound<T>("EVENT_NOT_FOUND");
    public Response<T> ResourceNotFound<T>()  => NotFound<T>("RESOURCE_NOT_FOUND");
    public Response<T> PageNotFound<T>()      => NotFound<T>("PAGE_NOT_FOUND");
    public Response<T> TopicNotFound<T>()     => NotFound<T>("TOPIC_NOT_FOUND");
    public Response<T> CategoryNotFound<T>()  => NotFound<T>("CATEGORY_NOT_FOUND");
    public Response<T> AssetNotFound<T>()     => NotFound<T>("ASSET_NOT_FOUND");
    public Response<T> AssetNotClean<T>()     => BusinessRule<T>("ASSET_NOT_CLEAN");

    // ─── Convenience shortcuts (Identity / Expert domain) ───

    public Response<T> ExpertRequestNotFound<T>() => NotFound<T>(ApplicationErrors.Identity.EXPERT_REQUEST_NOT_FOUND);

    // ─── Convenience shortcuts (Platform Settings domain) ───

    public Response<T> HomepageSettingsNotFound<T>()  => NotFound<T>(ApplicationErrors.PlatformSettings.HOMEPAGE_SETTINGS_NOT_FOUND);
    public Response<T> AboutSettingsNotFound<T>()     => NotFound<T>(ApplicationErrors.PlatformSettings.ABOUT_SETTINGS_NOT_FOUND);
    public Response<T> PoliciesSettingsNotFound<T>()  => NotFound<T>(ApplicationErrors.PlatformSettings.POLICIES_SETTINGS_NOT_FOUND);
    public Response<T> GlossaryEntryNotFound<T>()     => NotFound<T>(ApplicationErrors.PlatformSettings.GLOSSARY_ENTRY_NOT_FOUND);
    public Response<T> KnowledgePartnerNotFound<T>()  => NotFound<T>(ApplicationErrors.PlatformSettings.KNOWLEDGE_PARTNER_NOT_FOUND);
    public Response<T> PolicySectionNotFound<T>()     => NotFound<T>(ApplicationErrors.PlatformSettings.POLICY_SECTION_NOT_FOUND);
    public Response<T> ContentUpdateFailed<T>()       => BusinessRule<T>(ApplicationErrors.PlatformSettings.CONTENT_UPDATE_FAILED);

    // ─── Convenience shortcuts (Media domain) ───

    public Response<T> MediaFileNotFound<T>() => NotFound<T>(ApplicationErrors.Media.MEDIA_FILE_NOT_FOUND);
    public Response<T> InvalidFileType<T>()   => BusinessRule<T>(ApplicationErrors.Media.INVALID_FILE_TYPE);
    public Response<T> FileTooLarge<T>()      => BusinessRule<T>(ApplicationErrors.Media.FILE_TOO_LARGE);
    public Response<T> EmptyFile<T>()         => BusinessRule<T>(ApplicationErrors.Media.EMPTY_FILE);

    // ─── Convenience shortcuts (Verification domain) ───

    public Response<T> OtpNotFound<T>()          => NotFound<T>(ApplicationErrors.Verification.OTP_NOT_FOUND);
    public Response<T> OtpExpired<T>()           => BusinessRule<T>(ApplicationErrors.Verification.OTP_EXPIRED);
    public Response<T> OtpInvalidCode<T>()       => BusinessRule<T>(ApplicationErrors.Verification.OTP_INVALID_CODE);
    public Response<T> OtpMaxAttempts<T>()       => BusinessRule<T>(ApplicationErrors.Verification.OTP_MAX_ATTEMPTS);
    public Response<T> OtpCooldownActive<T>()    => BusinessRule<T>(ApplicationErrors.Verification.OTP_COOLDOWN_ACTIVE);
    public Response<T> OtpInvalidated<T>()       => BusinessRule<T>(ApplicationErrors.Verification.OTP_INVALIDATED);
    public Response<T> ContactAlreadyTaken<T>()  => Conflict<T>(ApplicationErrors.Verification.CONTACT_ALREADY_TAKEN);
    public Response<VoidData> EmailUpdated()     => Ok(ApplicationErrors.Verification.EMAIL_UPDATED);
    public Response<VoidData> PhoneUpdated()     => Ok(ApplicationErrors.Verification.PHONE_UPDATED);

    // ─── Convenience shortcuts (Country domain) ───

    public Response<T> CountryNotFound<T>()               => NotFound<T>(ApplicationErrors.Country.COUNTRY_NOT_FOUND);
    public Response<T> CountryProfileNotFound<T>()        => NotFound<T>(ApplicationErrors.Country.COUNTRY_PROFILE_NOT_FOUND);
    public Response<T> NoCountryAssigned<T>()             => NotFound<T>(ApplicationErrors.Country.NO_COUNTRY_ASSIGNED);
    public Response<T> CountryScopeForbidden<T>()         => Forbidden<T>(ApplicationErrors.Country.COUNTRY_SCOPE_FORBIDDEN);
    public Response<T> CountryContentRequestNotFound<T>()    => NotFound<T>(ApplicationErrors.Content.COUNTRY_RESOURCE_REQUEST_NOT_FOUND);
    public Response<T> CountryRequestProcessed<T>(T data)    => Ok(data, ApplicationErrors.Content.COUNTRY_REQUEST_PROCESSED);
    public Response<T> CountryRequestProcessingFailed<T>()   => BusinessRule<T>(ApplicationErrors.Content.COUNTRY_REQUEST_PROCESSING_FAILED);
    public Response<T> KapsarcDataUnavailable<T>()           => BusinessRule<T>(ApplicationErrors.Country.KAPSARC_DATA_UNAVAILABLE);
    public Response<T> KapsarcSnapshotRefreshed<T>(T data)   => Ok(data, ApplicationErrors.Country.KAPSARC_SNAPSHOT_REFRESHED);

    // ─── Convenience shortcuts (Evaluation domain) ───

    public Response<VoidData> EvaluationSubmitted() => Ok(ApplicationErrors.Evaluation.EVALUATION_SUBMITTED);
    public Response<T> EvaluationNotFound<T>()      => NotFound<T>(ApplicationErrors.Evaluation.EVALUATION_NOT_FOUND);

    // ─── Convenience shortcuts (Notification domain) ───

    public Response<T> NotificationTemplateNotFound<T>()            => NotFound<T>(ApplicationErrors.Notifications.TEMPLATE_NOT_FOUND);
    public Response<T> NotificationLogNotFound<T>()                 => NotFound<T>(ApplicationErrors.Notifications.NOTIFICATION_NOT_FOUND);
    public Response<VoidData> NotificationSettingsUpdated()         => Ok(ApplicationErrors.Notifications.NOTIFICATION_SETTINGS_UPDATED);
    public Response<VoidData> NotificationMarkedRead()              => Ok(ApplicationErrors.Notifications.NOTIFICATION_MARKED_READ);
    public Response<int> NotificationsMarkedRead(int count)         => Ok(count, ApplicationErrors.Notifications.NOTIFICATIONS_MARKED_READ);
    public Response<T> NotificationRetried<T>(T data)               => Ok(data, ApplicationErrors.Notifications.NOTIFICATION_RETRIED);
    public Response<T> NotificationTemplateCreated<T>(T data)       => Ok(data, ApplicationErrors.Notifications.NOTIFICATION_TEMPLATE_CREATED);
    public Response<T> NotificationTemplateUpdated<T>(T data)       => Ok(data, ApplicationErrors.Notifications.NOTIFICATION_TEMPLATE_UPDATED);

    // ─── Convenience shortcuts (Lookups domain) ───

    public Response<T> CountryCodeNotFound<T>() => NotFound<T>(ApplicationErrors.Lookups.COUNTRY_CODE_NOT_FOUND);
    public Response<T> LookupCreated<T>(T data) => Ok(data, ApplicationErrors.Lookups.LOOKUP_CREATED);
    public Response<T> LookupUpdated<T>(T data) => Ok(data, ApplicationErrors.Lookups.LOOKUP_UPDATED);

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
        if (code == SystemCode.ERR900 && domainKey != ApplicationErrors.General.INTERNAL_ERROR)
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
