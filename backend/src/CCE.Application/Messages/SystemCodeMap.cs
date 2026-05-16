namespace CCE.Application.Messages;

/// <summary>
/// Maps domain keys (used internally and in Resources.yaml) to system codes (sent to clients).
/// Every domain key maps to a UNIQUE system code.
/// </summary>
public static class SystemCodeMap
{
    private static readonly Dictionary<string, string> DomainToCode = new(StringComparer.OrdinalIgnoreCase)
    {
        // ─── Identity Errors ───
        ["USER_NOT_FOUND"] = SystemCode.ERR001,
        ["EXPERT_REQUEST_NOT_FOUND"] = SystemCode.ERR002,
        ["STATE_REP_ASSIGNMENT_NOT_FOUND"] = SystemCode.ERR003,
        ["EMAIL_EXISTS"] = SystemCode.ERR019,
        ["INVALID_CREDENTIALS"] = SystemCode.ERR020,
        ["INVALID_TOKEN"] = SystemCode.ERR021,
        ["INVALID_REFRESH_TOKEN"] = SystemCode.ERR022,
        ["PASSWORD_RECOVERY_FAILED"] = SystemCode.ERR023,
        ["LOGOUT_FAILED"] = SystemCode.ERR024,
        ["ACCOUNT_DEACTIVATED"] = SystemCode.ERR025,
        ["USERNAME_EXISTS"] = SystemCode.ERR026,
        ["REGISTRATION_FAILED"] = SystemCode.ERR027,
        ["NOT_AUTHENTICATED"] = SystemCode.ERR028,
        ["EXPERT_REQUEST_ALREADY_EXISTS"] = SystemCode.ERR029,
        ["STATE_REP_ASSIGNMENT_EXISTS"] = SystemCode.ERR030,

        // ─── Content Errors ───
        ["NEWS_NOT_FOUND"] = SystemCode.ERR040,
        ["EVENT_NOT_FOUND"] = SystemCode.ERR041,
        ["RESOURCE_NOT_FOUND"] = SystemCode.ERR042,
        ["PAGE_NOT_FOUND"] = SystemCode.ERR043,
        ["CATEGORY_NOT_FOUND"] = SystemCode.ERR044,
        ["ASSET_NOT_FOUND"] = SystemCode.ERR045,
        ["HOMEPAGE_SECTION_NOT_FOUND"] = SystemCode.ERR046,
        ["COUNTRY_RESOURCE_REQUEST_NOT_FOUND"] = SystemCode.ERR047,
        ["RESOURCE_DUPLICATE"] = SystemCode.ERR048,
        ["CATEGORY_DUPLICATE"] = SystemCode.ERR049,
        ["PAGE_DUPLICATE"] = SystemCode.ERR050,
        ["NEWS_DUPLICATE"] = SystemCode.ERR051,
        ["EVENT_DUPLICATE"] = SystemCode.ERR052,

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

        // ─── Country Errors ───
        ["COUNTRY_NOT_FOUND"] = SystemCode.ERR070,
        ["COUNTRY_PROFILE_NOT_FOUND"] = SystemCode.ERR071,

        // ─── Notification Errors ───
        ["TEMPLATE_NOT_FOUND"] = SystemCode.ERR080,
        ["TEMPLATE_DUPLICATE"] = SystemCode.ERR081,
        ["NOTIFICATION_NOT_FOUND"] = SystemCode.ERR082,

        // ─── KnowledgeMap Errors ───
        ["MAP_NOT_FOUND"] = SystemCode.ERR090,
        ["NODE_NOT_FOUND"] = SystemCode.ERR091,
        ["EDGE_NOT_FOUND"] = SystemCode.ERR092,

        // ─── InteractiveCity Errors ───
        ["SCENARIO_NOT_FOUND"] = SystemCode.ERR100,
        ["TECHNOLOGY_NOT_FOUND"] = SystemCode.ERR101,

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

        // ─── Identity Success ───
        ["LOGIN_SUCCESS"] = SystemCode.CON001,
        ["REGISTER_SUCCESS"] = SystemCode.CON002,
        ["LOGOUT_SUCCESS"] = SystemCode.CON003,
        ["TOKEN_REFRESHED"] = SystemCode.CON004,
        ["USER_UPDATED"] = SystemCode.CON005,
        ["USER_CREATED"] = SystemCode.CON006,
        ["USER_DELETED"] = SystemCode.CON007,
        ["USER_ACTIVATED"] = SystemCode.CON008,
        ["USER_DEACTIVATED"] = SystemCode.CON009,
        ["ROLES_ASSIGNED"] = SystemCode.CON010,
        ["PASSWORD_RESET"] = SystemCode.CON011,
        ["EXPERT_REQUEST_SUBMITTED"] = SystemCode.CON012,
        ["EXPERT_REQUEST_APPROVED"] = SystemCode.CON013,
        ["EXPERT_REQUEST_REJECTED"] = SystemCode.CON014,
        ["STATE_REP_ASSIGNMENT_CREATED"] = SystemCode.CON015,
        ["STATE_REP_ASSIGNMENT_REVOKED"] = SystemCode.CON016,
        ["PROFILE_UPDATED"] = SystemCode.CON017,

        // ─── Content Success ───
        ["CONTENT_CREATED"] = SystemCode.CON020,
        ["CONTENT_UPDATED"] = SystemCode.CON021,
        ["CONTENT_DELETED"] = SystemCode.CON022,
        ["CONTENT_PUBLISHED"] = SystemCode.CON023,
        ["CONTENT_ARCHIVED"] = SystemCode.CON024,
        ["RESOURCE_CREATED"] = SystemCode.CON025,
        ["RESOURCE_UPDATED"] = SystemCode.CON026,
        ["RESOURCE_DELETED"] = SystemCode.CON027,
        ["RESOURCE_PUBLISHED"] = SystemCode.CON028,

        // ─── Notification Success ───
        ["NOTIFICATION_CREATED"] = SystemCode.CON040,
        ["NOTIFICATION_MARKED_READ"] = SystemCode.CON041,
        ["NOTIFICATION_DELETED"] = SystemCode.CON042,

        // ─── General Success ───
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
        => DomainToCode.TryGetValue(domainKey, out var code) ? code : SystemCode.ERR900;

    /// <summary>Get the domain key from a system code. Returns null if unmapped.</summary>
    public static string? ToDomainKey(string systemCode)
        => CodeToDomain.TryGetValue(systemCode, out var key) ? key : null;

    /// <summary>True when the domain key has an explicit mapping.</summary>
    public static bool HasMapping(string domainKey) => DomainToCode.ContainsKey(domainKey);
}
