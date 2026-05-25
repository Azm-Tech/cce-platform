using CCE.Application.Errors;
using CCE.Application.Localization;
using CCE.Domain.Common;

namespace CCE.Application.Common;

/// <summary>
/// Factory for creating localized <see cref="Error"/> instances.
/// Each method looks up the bilingual message from Resources.yaml.
/// </summary>
public sealed class Errors
{
    private readonly ILocalizationService _l;

    public Errors(ILocalizationService l) => _l = l;

    // ─── General ───
    public Error NotFound(string code)
        => Build(code, ErrorType.NotFound);
    public Error Conflict(string code)
        => Build(code, ErrorType.Conflict);
    public Error BusinessRule(string code)
        => Build(code, ErrorType.BusinessRule);
    public Error Validation(string code, IDictionary<string, string[]>? details = null)
        => Build(code, ErrorType.Validation, details);
    public Error Forbidden(string code)
        => Build(code, ErrorType.Forbidden);
    public Error Unauthorized(string code)
        => Build(code, ErrorType.Unauthorized);

    // ─── Convenience: Content domain ───
    public Error NewsNotFound()      => NotFound($"CONTENT_{ApplicationErrors.Content.NEWS_NOT_FOUND}");
    public Error EventNotFound()     => NotFound($"CONTENT_{ApplicationErrors.Content.EVENT_NOT_FOUND}");
    public Error ResourceNotFound()  => NotFound($"CONTENT_{ApplicationErrors.Content.RESOURCE_NOT_FOUND}");
    public Error PageNotFound()      => NotFound($"CONTENT_{ApplicationErrors.Content.PAGE_NOT_FOUND}");
    public Error CategoryNotFound()  => NotFound($"CONTENT_{ApplicationErrors.Content.CATEGORY_NOT_FOUND}");
    public Error AssetNotFound()     => NotFound($"CONTENT_{ApplicationErrors.Content.ASSET_NOT_FOUND}");
    public Error HomepageSectionNotFound() => NotFound($"CONTENT_{ApplicationErrors.Content.HOMEPAGE_SECTION_NOT_FOUND}");

    // ─── Convenience: Identity domain ───
    public Error UserNotFound()      => NotFound($"IDENTITY_{ApplicationErrors.Identity.USER_NOT_FOUND}");
    public Error ExpertRequestNotFound() => NotFound($"IDENTITY_{ApplicationErrors.Identity.EXPERT_REQUEST_NOT_FOUND}");
    public Error ExpertRequestAlreadyExists() => Conflict($"IDENTITY_{ApplicationErrors.Identity.EXPERT_REQUEST_ALREADY_EXISTS}");
    public Error StateRepAssignmentNotFound() => NotFound($"IDENTITY_{ApplicationErrors.Identity.STATE_REP_ASSIGNMENT_NOT_FOUND}");
    public Error StateRepAssignmentAlreadyExists() => Conflict($"IDENTITY_{ApplicationErrors.Identity.STATE_REP_ASSIGNMENT_EXISTS}");
    public Error NotAuthenticated() => Unauthorized($"IDENTITY_{ApplicationErrors.Identity.NOT_AUTHENTICATED}");
    public Error InvalidCredentials() => Unauthorized($"IDENTITY_{ApplicationErrors.Identity.INVALID_CREDENTIALS}");
    public Error InvalidRefreshToken() => Unauthorized($"IDENTITY_{ApplicationErrors.Identity.INVALID_REFRESH_TOKEN}");
    public Error EmailExists() => Conflict($"IDENTITY_{ApplicationErrors.Identity.EMAIL_EXISTS}");
    public Error RegistrationFailed(IDictionary<string, string[]>? details = null)
        => Validation($"IDENTITY_{ApplicationErrors.Identity.REGISTRATION_FAILED}", details);

    // ─── Convenience: Community domain ───
    public Error TopicNotFound()     => NotFound($"COMMUNITY_{ApplicationErrors.Community.TOPIC_NOT_FOUND}");
    public Error PostNotFound()      => NotFound($"COMMUNITY_{ApplicationErrors.Community.POST_NOT_FOUND}");
    public Error ReplyNotFound()     => NotFound($"COMMUNITY_{ApplicationErrors.Community.REPLY_NOT_FOUND}");

    // ─── Convenience: Country domain ───
    public Error CountryNotFound()   => NotFound($"COUNTRY_{ApplicationErrors.Country.COUNTRY_NOT_FOUND}");

    private Error Build(string code, ErrorType type, IDictionary<string, string[]>? details = null)
    {
        var msg = _l.GetLocalizedMessage(code);
        return new Error(code, msg.Ar, msg.En, type, details);
    }
}
