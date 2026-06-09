namespace CCE.Application.Messages;

/// <summary>
/// Maps domain keys (used internally and in Resources.yaml) to system codes (sent to clients).
/// Every domain key maps to a UNIQUE system code.
/// </summary>
public static class SystemCodeMap
{
    private static readonly Dictionary<string, string> DomainToCode = new(StringComparer.OrdinalIgnoreCase)
    {
        // ─── Identity Errors (appendix-aligned) ───
        ["USER_NOT_FOUND"] = SystemCode.ERR001,
        ["EMAIL_EXISTS"] = SystemCode.ERR019,
        ["INVALID_CREDENTIALS"] = SystemCode.ERR020,
        ["PASSWORD_RECOVERY_FAILED"] = SystemCode.ERR023,
        ["LOGOUT_FAILED"] = SystemCode.ERR024,

        // ─── Backend-only Identity Errors (moved to free appendix numbers) ───
        ["EXPERT_REQUEST_NOT_FOUND"] = SystemCode.ERR400,
        ["STATE_REP_ASSIGNMENT_NOT_FOUND"] = SystemCode.ERR401,
        ["INVALID_TOKEN"] = SystemCode.ERR402,
        ["INVALID_REFRESH_TOKEN"] = SystemCode.ERR403,
        ["ACCOUNT_DEACTIVATED"] = SystemCode.ERR404,
        ["USERNAME_EXISTS"] = SystemCode.ERR405,
        ["REGISTRATION_FAILED"] = SystemCode.ERR406,
        ["NOT_AUTHENTICATED"] = SystemCode.ERR407,
        ["EXPERT_REQUEST_ALREADY_EXISTS"] = SystemCode.ERR408,
        ["STATE_REP_ASSIGNMENT_EXISTS"] = SystemCode.ERR409,

        // ─── Content Errors ───
        ["NEWS_NOT_FOUND"] = SystemCode.ERR040,
        ["EVENT_NOT_FOUND"] = SystemCode.ERR041,
        ["RESOURCE_NOT_FOUND"] = SystemCode.ERR042,
        ["PAGE_NOT_FOUND"] = SystemCode.ERR043,
        ["CATEGORY_NOT_FOUND"] = SystemCode.ERR044,
        ["ASSET_NOT_FOUND"] = SystemCode.ERR045,
        ["HOMEPAGE_SECTION_NOT_FOUND"] = SystemCode.ERR046,
        ["ASSET_NOT_CLEAN"] = SystemCode.ERR059,
        ["COUNTRY_RESOURCE_REQUEST_NOT_FOUND"] = SystemCode.ERR047,
        ["RESOURCE_DUPLICATE"] = SystemCode.ERR048,
        ["CATEGORY_DUPLICATE"] = SystemCode.ERR049,
        ["PAGE_DUPLICATE"] = SystemCode.ERR050,
        ["NEWS_DUPLICATE"] = SystemCode.ERR051,
        ["EVENT_DUPLICATE"] = SystemCode.ERR052,
        ["RESOURCE_DOWNLOAD_FAILED"] = SystemCode.ERR002,
        ["RESOURCE_UPLOAD_FAILED"] = SystemCode.ERR029,
        ["RESOURCE_DELETE_FAILED"] = SystemCode.ERR030,

        // ─── Community Errors ───
        ["TOPIC_NOT_FOUND"] = SystemCode.ERR060,
        ["POST_NOT_FOUND"] = SystemCode.ERR061,
        ["REPLY_NOT_FOUND"] = SystemCode.ERR062,
        ["RATING_NOT_FOUND"] = SystemCode.ERR063,
        ["TOPIC_DUPLICATE"] = SystemCode.ERR064,
        ["ALREADY_FOLLOWING"] = SystemCode.ERR065,
        ["NOT_FOLLOWING"] = SystemCode.ERR066,
        ["CANNOT_MARK_ANSWERED"] = SystemCode.ERR067,
        ["EDIT_WINDOW_EXPIRED"] = SystemCode.ERR068,

        ["POST_ALREADY_PUBLISHED"] = SystemCode.ERR069,
        ["COMMUNITY_NOT_FOUND"] = SystemCode.ERR140,
        ["JOIN_REQUEST_NOT_FOUND"] = SystemCode.ERR141,
        ["POLL_NOT_FOUND"] = SystemCode.ERR142,
        ["POLL_CLOSED"] = SystemCode.ERR143,

        // ─── Community Success ───
        ["POST_VOTED"] = SystemCode.CON065,
        ["POST_CREATED"] = SystemCode.CON066,
        ["POST_DRAFT_SAVED"] = SystemCode.CON067,
        ["POST_PUBLISHED"] = SystemCode.CON068,
        ["DRAFT_DELETED"] = SystemCode.CON069,

        // ─── Country / State-Rep Errors ───
        ["COUNTRY_NOT_FOUND"] = SystemCode.ERR070,
        ["COUNTRY_PROFILE_NOT_FOUND"] = SystemCode.ERR071,
        ["COUNTRY_REQUEST_PROCESSING_FAILED"] = SystemCode.ERR072,
        ["COUNTRY_SCOPE_FORBIDDEN"] = SystemCode.ERR073,
        ["NO_COUNTRY_ASSIGNED"] = SystemCode.ERR074,
        ["KAPSARC_DATA_UNAVAILABLE"] = SystemCode.ERR075,

        // ─── Country / State-Rep Success ───
        ["COUNTRY_PROFILE_UPDATED"] = SystemCode.CON057,
        ["COUNTRY_CONTENT_REQUEST_SUBMITTED"] = SystemCode.CON058,
        ["COUNTRY_REQUEST_PROCESSED"] = SystemCode.CON059,
        ["KAPSARC_SNAPSHOT_REFRESHED"] = SystemCode.CON064,

        // ─── Notification Errors ───
        ["TEMPLATE_NOT_FOUND"] = SystemCode.ERR080,
        ["TEMPLATE_DUPLICATE"] = SystemCode.ERR081,
        ["NOTIFICATION_NOT_FOUND"] = SystemCode.ERR082,

        // ─── KnowledgeMap Errors ───
        ["MAP_NOT_FOUND"] = SystemCode.ERR090,
        ["NODE_NOT_FOUND"] = SystemCode.ERR091,
        ["EDGE_NOT_FOUND"] = SystemCode.ERR092,

        // ─── Media Errors ───
        ["MEDIA_FILE_NOT_FOUND"] = SystemCode.ERR110,
        ["INVALID_FILE_TYPE"] = SystemCode.ERR111,
        ["FILE_TOO_LARGE"] = SystemCode.ERR112,
        ["EMPTY_FILE"] = SystemCode.ERR113,

        // ─── InteractiveCity Errors ───
        ["SCENARIO_NOT_FOUND"] = SystemCode.ERR100,
        ["TECHNOLOGY_NOT_FOUND"] = SystemCode.ERR101,

        // ─── Platform Settings Errors ───
        ["HOMEPAGE_SETTINGS_NOT_FOUND"] = SystemCode.ERR053,
        ["ABOUT_SETTINGS_NOT_FOUND"] = SystemCode.ERR054,
        ["POLICIES_SETTINGS_NOT_FOUND"] = SystemCode.ERR055,
        ["GLOSSARY_ENTRY_NOT_FOUND"] = SystemCode.ERR056,
        ["KNOWLEDGE_PARTNER_NOT_FOUND"] = SystemCode.ERR057,
        ["POLICY_SECTION_NOT_FOUND"] = SystemCode.ERR058,

        // ─── Lookups Errors ───
        ["COUNTRY_CODE_NOT_FOUND"] = SystemCode.ERR130,

        // ─── Verification Errors ───
        ["OTP_NOT_FOUND"] = SystemCode.ERR120,
        ["OTP_EXPIRED"] = SystemCode.ERR121,
        ["OTP_INVALID_CODE"] = SystemCode.ERR122,
        ["OTP_MAX_ATTEMPTS"] = SystemCode.ERR123,
        ["OTP_COOLDOWN_ACTIVE"] = SystemCode.ERR124,
        ["OTP_INVALIDATED"] = SystemCode.ERR125,
        ["CONTACT_ALREADY_TAKEN"] = SystemCode.ERR126,

        // ─── Evaluation Errors ───
        ["EVALUATION_NOT_FOUND"] = SystemCode.ERR009,

        // ─── General Errors ───
        ["INTERNAL_ERROR"] = SystemCode.ERR900,
        ["UNAUTHORIZED_ACCESS"] = SystemCode.ERR901,
        ["FORBIDDEN_ACCESS"] = SystemCode.ERR902,
        ["RESOURCE_NOT_FOUND_GENERIC"] = SystemCode.ERR903,
        ["BAD_REQUEST"] = SystemCode.ERR904,
        ["EXTERNAL_API_ERROR"] = SystemCode.ERR905,
        ["EXTERNAL_API_NOT_CONFIGURED"] = SystemCode.ERR906,
        ["CONCURRENCY_CONFLICT"] = SystemCode.ERR907,
        ["DUPLICATE_VALUE"] = SystemCode.ERR908,

        // ─── Identity Success (appendix-aligned) ───
        ["LOGIN_SUCCESS"] = SystemCode.CON056,
        ["TOKEN_REFRESHED"] = SystemCode.CON004,
        ["PROFILE_UPDATED"] = SystemCode.CON005,
        ["EXPERT_REQUEST_SUBMITTED"] = SystemCode.CON006,
        ["PASSWORD_RESET"] = SystemCode.CON014,
        ["LOGOUT_SUCCESS"] = SystemCode.CON015,
        ["REGISTER_SUCCESS"] = SystemCode.CON017,
        ["USER_DELETED"] = SystemCode.CON018,

        // ─── Backend-only Identity Success (appendix numbers already taken) ───
        ["EXPERT_REQUEST_APPROVED"] = SystemCode.CON050,
        ["EXPERT_REQUEST_REJECTED"] = SystemCode.CON051,
        ["STATE_REP_ASSIGNMENT_CREATED"] = SystemCode.CON052,
        ["STATE_REP_ASSIGNMENT_REVOKED"] = SystemCode.CON053,
        ["ROLES_ASSIGNED"] = SystemCode.CON054,
        ["USER_STATUS_CHANGED"] = SystemCode.CON055,

        // ─── Platform Settings Success ───
        ["SETTINGS_UPDATED"] = SystemCode.CON016,
        ["CONTENT_UPDATE_FAILED"] = SystemCode.ERR025,

        // ─── Content Success ───
        ["CONTENT_CREATED"] = SystemCode.CON020,
        ["CONTENT_UPDATED"] = SystemCode.CON025,
        ["CONTENT_DELETED"] = SystemCode.CON027,

        // ─── Asset Success ───
        ["ASSET_UPLOADED"] = SystemCode.CON038,

        // ─── Media Success ───
        ["MEDIA_UPLOADED"] = SystemCode.CON029,
        ["MEDIA_UPDATED"] = SystemCode.CON036,
        ["MEDIA_DELETED"] = SystemCode.CON037,
        ["CONTENT_PUBLISHED"] = SystemCode.CON023,
        ["CONTENT_ARCHIVED"] = SystemCode.CON024,
        ["RESOURCE_CREATED"] = SystemCode.CON021,
        ["RESOURCE_UPDATED"] = SystemCode.CON026,
        ["RESOURCE_DELETED"] = SystemCode.CON022,
        ["RESOURCE_PUBLISHED"] = SystemCode.CON028,
        ["RESOURCE_DOWNLOAD_SUCCESS"] = SystemCode.CON001,
        ["RESOURCE_SHARE_SUCCESS"] = SystemCode.CON002,
        ["RESOURCE_SHARE_FAILED"] = SystemCode.ERR003,

        // ─── Lookups Success ───
        ["LOOKUP_CREATED"] = SystemCode.CON070,
        ["LOOKUP_UPDATED"] = SystemCode.CON071,

        // ─── Notification Success ───
        ["NOTIFICATION_CREATED"] = SystemCode.CON040,
        ["NOTIFICATION_MARKED_READ"] = SystemCode.CON041,
        ["NOTIFICATION_DELETED"] = SystemCode.CON042,
        ["NOTIFICATION_SETTINGS_UPDATED"] = SystemCode.CON043,
        ["NOTIFICATION_RETRIED"] = SystemCode.CON044,
        ["NOTIFICATIONS_MARKED_READ"] = SystemCode.CON045,
        ["NOTIFICATION_TEMPLATE_CREATED"] = SystemCode.CON046,
        ["NOTIFICATION_TEMPLATE_UPDATED"] = SystemCode.CON047,

        // ─── Verification Success ───
        ["OTP_SENT"] = SystemCode.CON060,
        ["OTP_VERIFIED"] = SystemCode.CON061,
        ["EMAIL_UPDATED"] = SystemCode.CON062,
        ["PHONE_UPDATED"] = SystemCode.CON063,

        // ─── Evaluation Success ───
        ["EVALUATION_SUBMITTED"] = SystemCode.CON008,

        // ─── General Success ───
        ["ITEMS_LISTED"] = SystemCode.CON100,
        ["SUCCESS_OPERATION"] = SystemCode.CON900,
        ["SUCCESS_CREATED"] = SystemCode.CON901,
        ["SUCCESS_UPDATED"] = SystemCode.CON902,
        ["SUCCESS_DELETED"] = SystemCode.CON903,

        // ─── Validation ───
        ["VALIDATION_ERROR"] = SystemCode.VAL001,
        ["REQUIRED_FIELD"] = SystemCode.VAL002,
        ["INVALID_EMAIL"] = SystemCode.VAL003,
        ["INVALID_PHONE"] = SystemCode.VAL004,
        ["MIN_LENGTH"] = SystemCode.VAL005,
        ["MAX_LENGTH"] = SystemCode.VAL006,
        ["INVALID_FORMAT"] = SystemCode.VAL007,
        ["INVALID_ENUM"] = SystemCode.VAL008,
        ["PASSWORD_UPPERCASE"] = SystemCode.VAL009,
        ["PASSWORD_LOWERCASE"] = SystemCode.VAL010,
        ["PASSWORD_NUMBER"] = SystemCode.VAL011,
    };

    private static readonly Dictionary<string, string> CodeToDomain =
        DomainToCode.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>Get the ERR/CON/VAL code for a domain key. Returns ERR900 if unmapped.</summary>
    public static string ToSystemCode(string domainKey)
        => domainKey is not null && DomainToCode.TryGetValue(domainKey, out var code) ? code : SystemCode.ERR900;

    /// <summary>Get the domain key from a system code. Returns null if unmapped.</summary>
    public static string? ToDomainKey(string systemCode)
        => CodeToDomain.TryGetValue(systemCode, out var key) ? key : null;

    /// <summary>True when the domain key has an explicit mapping.</summary>
    public static bool HasMapping(string domainKey) => DomainToCode.ContainsKey(domainKey);
}
