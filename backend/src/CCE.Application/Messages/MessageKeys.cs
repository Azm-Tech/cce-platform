namespace CCE.Application.Messages;

public static class MessageKeys
{
    public static class General
    {
        public const string VALIDATION_ERROR = "VALIDATION_ERROR";
        public const string INTERNAL_ERROR = "INTERNAL_ERROR";
        public const string UNAUTHORIZED = "UNAUTHORIZED_ACCESS";
        public const string FORBIDDEN = "FORBIDDEN_ACCESS";
        public const string RESOURCE_NOT_FOUND_GENERIC = "RESOURCE_NOT_FOUND_GENERIC";
        public const string BAD_REQUEST = "BAD_REQUEST";
        public const string SUCCESS_CREATED = "SUCCESS_CREATED";
        public const string SUCCESS_UPDATED = "SUCCESS_UPDATED";
        public const string SUCCESS_DELETED = "SUCCESS_DELETED";
        public const string SUCCESS_OPERATION = "SUCCESS_OPERATION";
        public const string DUPLICATE_VALUE = "DUPLICATE_VALUE";
        public const string CONCURRENCY_CONFLICT = "CONCURRENCY_CONFLICT";
        public const string EXTERNAL_API_ERROR = "EXTERNAL_API_ERROR";
        public const string EXTERNAL_API_NOT_CONFIGURED = "EXTERNAL_API_NOT_CONFIGURED";
        public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";
        public const string BUSINESS_RULE_VIOLATION = "BUSINESS_RULE_VIOLATION";
        public const string ITEMS_LISTED = "ITEMS_LISTED";
    }

    public static class Identity
    {
        public const string USER_NOT_FOUND = "USER_NOT_FOUND";
        public const string EMAIL_EXISTS = "EMAIL_EXISTS";
        public const string USERNAME_EXISTS = "USERNAME_EXISTS";
        public const string USER_CREATED = "USER_CREATED";
        public const string USER_UPDATED = "USER_UPDATED";
        public const string USER_DELETED = "USER_DELETED";
        public const string USER_ACTIVATED = "USER_ACTIVATED";
        public const string USER_DEACTIVATED = "USER_DEACTIVATED";
        public const string ROLES_ASSIGNED = "ROLES_ASSIGNED";
        public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
        public const string INVALID_TOKEN = "INVALID_TOKEN";
        public const string INVALID_REFRESH_TOKEN = "INVALID_REFRESH_TOKEN";
        public const string INVALID_RESET_TOKEN = "INVALID_RESET_TOKEN";
        public const string REGISTRATION_FAILED = "REGISTRATION_FAILED";
        public const string LOGIN_FAILED = "LOGIN_FAILED";
        public const string PASSWORD_RECOVERY_FAILED = "PASSWORD_RECOVERY_FAILED";
        public const string PASSWORD_RESET = "PASSWORD_RESET";
        public const string LOGOUT_FAILED = "LOGOUT_FAILED";
        public const string LOGOUT_SUCCESS = "LOGOUT_SUCCESS";
        public const string ACCOUNT_DEACTIVATED = "ACCOUNT_DEACTIVATED";
        public const string NOT_AUTHENTICATED = "NOT_AUTHENTICATED";
        public const string EXPERT_REQUEST_NOT_FOUND = "EXPERT_REQUEST_NOT_FOUND";
        public const string EXPERT_REQUEST_ALREADY_EXISTS = "EXPERT_REQUEST_ALREADY_EXISTS";
        public const string STATE_REP_ASSIGNMENT_NOT_FOUND = "STATE_REP_ASSIGNMENT_NOT_FOUND";
        public const string STATE_REP_ASSIGNMENT_EXISTS = "STATE_REP_ASSIGNMENT_EXISTS";
        public const string CONTACT_NOT_VERIFIED = "CONTACT_NOT_VERIFIED";
        public const string LOGIN_SUCCESS = "LOGIN_SUCCESS";
        public const string AD_LOGIN_SUCCESS = "AD_LOGIN_SUCCESS";
        public const string REGISTER_SUCCESS = "REGISTER_SUCCESS";
        public const string PROFILE_UPDATED = "PROFILE_UPDATED";
        public const string TOKEN_REFRESHED = "TOKEN_REFRESHED";
        public const string USER_STATUS_CHANGED = "USER_STATUS_CHANGED";
        public const string EXPERT_REQUEST_SUBMITTED = "EXPERT_REQUEST_SUBMITTED";
        public const string EXPERT_REQUEST_APPROVED = "EXPERT_REQUEST_APPROVED";
        public const string EXPERT_REQUEST_REJECTED = "EXPERT_REQUEST_REJECTED";
        public const string STATE_REP_ASSIGNMENT_CREATED = "STATE_REP_ASSIGNMENT_CREATED";
        public const string STATE_REP_ASSIGNMENT_REVOKED = "STATE_REP_ASSIGNMENT_REVOKED";
        public const string INTEREST_UPSERTED = "INTEREST_UPSERTED";
        public const string ROLE_NOT_FOUND = "ROLE_NOT_FOUND";
        public const string PERMISSIONS_GRANTED = "PERMISSIONS_GRANTED";
        public const string PERMISSIONS_REVOKED = "PERMISSIONS_REVOKED";
        public const string PERMISSIONS_UPDATED = "PERMISSIONS_UPDATED";
        public const string CLAIMS_GRANTED = "CLAIMS_GRANTED";
        public const string CLAIMS_REVOKED = "CLAIMS_REVOKED";
        public const string USER_CLAIMS_UPDATED = "USER_CLAIMS_UPDATED";
        public const string EMAIL_CHANGE_FAILED = "EMAIL_CHANGE_FAILED";
    }

    public static class Content
    {
        public const string RESOURCE_NOT_FOUND = "RESOURCE_NOT_FOUND";
        public const string RESOURCE_DUPLICATE = "RESOURCE_DUPLICATE";
        public const string TAG_NOT_FOUND = "TAG_NOT_FOUND";
        public const string NEWSLETTER_SUBSCRIBED = "NEWSLETTER_SUBSCRIBED";
        public const string RESOURCE_CREATED = "RESOURCE_CREATED";
        public const string RESOURCE_UPDATED = "RESOURCE_UPDATED";
        public const string RESOURCE_DELETED = "RESOURCE_DELETED";
        public const string RESOURCE_PUBLISHED = "RESOURCE_PUBLISHED";
        public const string CATEGORY_NOT_FOUND = "CATEGORY_NOT_FOUND";
        public const string CATEGORY_DUPLICATE = "CATEGORY_DUPLICATE";
        public const string PAGE_NOT_FOUND = "PAGE_NOT_FOUND";
        public const string PAGE_DUPLICATE = "PAGE_DUPLICATE";
        public const string NEWS_NOT_FOUND = "NEWS_NOT_FOUND";
        public const string NEWS_DUPLICATE = "NEWS_DUPLICATE";
        public const string EVENT_NOT_FOUND = "EVENT_NOT_FOUND";
        public const string EVENT_DUPLICATE = "EVENT_DUPLICATE";
        public const string HOMEPAGE_SECTION_NOT_FOUND = "HOMEPAGE_SECTION_NOT_FOUND";
        public const string ASSET_NOT_FOUND = "ASSET_NOT_FOUND";
        public const string ASSET_NOT_CLEAN = "ASSET_NOT_CLEAN";
        public const string COUNTRY_RESOURCE_REQUEST_NOT_FOUND = "COUNTRY_RESOURCE_REQUEST_NOT_FOUND";
        public const string COUNTRY_CONTENT_REQUEST_SUBMITTED = "COUNTRY_CONTENT_REQUEST_SUBMITTED";
        public const string COUNTRY_REQUEST_PROCESSED = "COUNTRY_REQUEST_PROCESSED";
        public const string COUNTRY_REQUEST_PROCESSING_FAILED = "COUNTRY_REQUEST_PROCESSING_FAILED";
        public const string CONTENT_CREATED = "CONTENT_CREATED";
        public const string CONTENT_UPDATED = "CONTENT_UPDATED";
        public const string CONTENT_DELETED = "CONTENT_DELETED";
        public const string CONTENT_PUBLISHED = "CONTENT_PUBLISHED";
        public const string CONTENT_ARCHIVED = "CONTENT_ARCHIVED";
        public const string ASSET_UPLOADED = "ASSET_UPLOADED";
        public const string RESOURCE_DOWNLOAD_SUCCESS = "RESOURCE_DOWNLOAD_SUCCESS";
        public const string RESOURCE_SHARE_SUCCESS = "RESOURCE_SHARE_SUCCESS";
        public const string RESOURCE_SHARE_FAILED = "RESOURCE_SHARE_FAILED";
        public const string RESOURCE_DOWNLOAD_FAILED = "RESOURCE_DOWNLOAD_FAILED";
        public const string RESOURCE_UPLOAD_FAILED = "RESOURCE_UPLOAD_FAILED";
        public const string RESOURCE_DELETE_FAILED = "RESOURCE_DELETE_FAILED";
    }

    public static class Community
    {
        public const string TOPICS_LISTED = "TOPICS_LISTED";
        public const string TOPIC_NOT_FOUND = "TOPIC_NOT_FOUND";
        public const string TOPIC_DUPLICATE = "TOPIC_DUPLICATE";
        public const string POST_NOT_FOUND = "POST_NOT_FOUND";
        public const string REPLY_NOT_FOUND = "REPLY_NOT_FOUND";
        public const string RATING_NOT_FOUND = "RATING_NOT_FOUND";
        public const string ALREADY_FOLLOWING = "ALREADY_FOLLOWING";
        public const string NOT_FOLLOWING = "NOT_FOLLOWING";
        public const string CANNOT_FOLLOW_SELF = "CANNOT_FOLLOW_SELF";
        public const string CANNOT_MARK_ANSWERED = "CANNOT_MARK_ANSWERED";
        public const string EDIT_WINDOW_EXPIRED = "EDIT_WINDOW_EXPIRED";
        public const string POST_VOTED = "POST_VOTED";
        public const string POST_CREATED = "POST_CREATED";
        public const string POST_DRAFT_SAVED = "POST_DRAFT_SAVED";
        public const string POST_PUBLISHED = "POST_PUBLISHED";
        public const string DRAFT_DELETED = "DRAFT_DELETED";
        public const string POST_ALREADY_PUBLISHED = "POST_ALREADY_PUBLISHED";
        public const string COMMUNITY_NOT_FOUND = "COMMUNITY_NOT_FOUND";
        public const string JOIN_REQUEST_NOT_FOUND = "JOIN_REQUEST_NOT_FOUND";
        public const string POLL_NOT_FOUND = "POLL_NOT_FOUND";
        public const string POLL_CLOSED = "POLL_CLOSED";
    }

    public static class Country
    {
        public const string COUNTRY_NOT_FOUND = "COUNTRY_NOT_FOUND";
        public const string COUNTRY_PROFILE_NOT_FOUND = "COUNTRY_PROFILE_NOT_FOUND";
        public const string COUNTRY_PROFILE_UPDATED = "COUNTRY_PROFILE_UPDATED";
        public const string COUNTRY_SCOPE_FORBIDDEN = "COUNTRY_SCOPE_FORBIDDEN";
        public const string NO_COUNTRY_ASSIGNED = "NO_COUNTRY_ASSIGNED";
        public const string KAPSARC_DATA_UNAVAILABLE = "KAPSARC_DATA_UNAVAILABLE";
        public const string KAPSARC_SNAPSHOT_REFRESHED = "KAPSARC_SNAPSHOT_REFRESHED";
    }

    public static class Notifications
    {
        public const string TEMPLATE_NOT_FOUND = "TEMPLATE_NOT_FOUND";
        public const string TEMPLATE_DUPLICATE = "TEMPLATE_DUPLICATE";
        public const string NOTIFICATION_NOT_FOUND = "NOTIFICATION_NOT_FOUND";
        public const string NOTIFICATION_CREATED = "NOTIFICATION_CREATED";
        public const string NOTIFICATION_MARKED_READ = "NOTIFICATION_MARKED_READ";
        public const string NOTIFICATION_DELETED = "NOTIFICATION_DELETED";
        public const string NOTIFICATION_SETTINGS_UPDATED = "NOTIFICATION_SETTINGS_UPDATED";
        public const string NOTIFICATION_RETRIED = "NOTIFICATION_RETRIED";
        public const string NOTIFICATIONS_MARKED_READ = "NOTIFICATIONS_MARKED_READ";
        public const string NOTIFICATION_TEMPLATE_CREATED = "NOTIFICATION_TEMPLATE_CREATED";
        public const string NOTIFICATION_TEMPLATE_UPDATED = "NOTIFICATION_TEMPLATE_UPDATED";
        public const string DEVICE_TOKEN_NOT_FOUND = "DEVICE_TOKEN_NOT_FOUND";
        public const string DEVICE_TOKEN_REGISTERED = "DEVICE_TOKEN_REGISTERED";
        public const string DEVICE_TOKEN_DELETED = "DEVICE_TOKEN_DELETED";
    }

    public static class KnowledgeMap
    {
        public const string MAP_NOT_FOUND = "MAP_NOT_FOUND";
        public const string NODE_NOT_FOUND = "NODE_NOT_FOUND";
        public const string EDGE_NOT_FOUND = "EDGE_NOT_FOUND";
    }

    public static class CommunityLaws
    {
        public const string SECTION_NOT_FOUND = "SECTION_NOT_FOUND";
        public const string SECTION_CREATED = "SECTION_CREATED";
        public const string SECTION_UPDATED = "SECTION_UPDATED";
        public const string SECTION_DELETED = "SECTION_DELETED";
        public const string CONTENT_REORDERED = "CONTENT_REORDERED";
    }

    public static class PlatformSettings
    {
        public const string HOMEPAGE_SETTINGS_NOT_FOUND = "HOMEPAGE_SETTINGS_NOT_FOUND";
        public const string HOMEPAGE_SECTION_NOT_FOUND = "HOMEPAGE_SECTION_NOT_FOUND";
        public const string SECTION_REORDERED = "SECTION_REORDERED";
        public const string ABOUT_SETTINGS_NOT_FOUND = "ABOUT_SETTINGS_NOT_FOUND";
        public const string POLICIES_SETTINGS_NOT_FOUND = "POLICIES_SETTINGS_NOT_FOUND";
        public const string GLOSSARY_ENTRY_NOT_FOUND = "GLOSSARY_ENTRY_NOT_FOUND";
        public const string KNOWLEDGE_PARTNER_NOT_FOUND = "KNOWLEDGE_PARTNER_NOT_FOUND";
        public const string POLICY_SECTION_NOT_FOUND = "POLICY_SECTION_NOT_FOUND";
        public const string CONTENT_UPDATE_FAILED = "CONTENT_UPDATE_FAILED";
        public const string SETTINGS_UPDATED = "SETTINGS_UPDATED";
    }

    public static class Media
    {
        public const string MEDIA_FILE_NOT_FOUND = "MEDIA_FILE_NOT_FOUND";
        public const string INVALID_FILE_TYPE = "INVALID_FILE_TYPE";
        public const string FILE_TOO_LARGE = "FILE_TOO_LARGE";
        public const string EMPTY_FILE = "EMPTY_FILE";
        public const string MEDIA_UPLOADED = "MEDIA_UPLOADED";
        public const string MEDIA_UPDATED = "MEDIA_UPDATED";
        public const string MEDIA_DELETED = "MEDIA_DELETED";
    }

    public static class Verification
    {
        public const string OTP_NOT_FOUND = "OTP_NOT_FOUND";
        public const string OTP_UNAUTHORIZED = "OTP_UNAUTHORIZED";
        public const string OTP_EXPIRED = "OTP_EXPIRED";
        public const string OTP_INVALID_CODE = "OTP_INVALID_CODE";
        public const string OTP_MAX_ATTEMPTS = "OTP_MAX_ATTEMPTS";
        public const string OTP_COOLDOWN_ACTIVE = "OTP_COOLDOWN_ACTIVE";
        public const string OTP_INVALIDATED = "OTP_INVALIDATED";
        public const string CONTACT_ALREADY_TAKEN = "CONTACT_ALREADY_TAKEN";
        public const string EMAIL_UPDATED = "EMAIL_UPDATED";
        public const string PHONE_UPDATED = "PHONE_UPDATED";
        public const string OTP_SENT = "OTP_SENT";
        public const string OTP_VERIFIED = "OTP_VERIFIED";
    }

    public static class Lookups
    {
        public const string COUNTRY_CODE_NOT_FOUND = "COUNTRY_CODE_NOT_FOUND";
        public const string LOOKUP_CREATED = "LOOKUP_CREATED";
        public const string LOOKUP_UPDATED = "LOOKUP_UPDATED";
    }

    public static class InteractiveMaps
    {
        public const string MAP_NOT_FOUND = "INTERACTIVE_MAP_NOT_FOUND";
        public const string MAP_CREATED = "INTERACTIVE_MAP_CREATED";
        public const string MAP_UPDATED = "INTERACTIVE_MAP_UPDATED";
        public const string MAP_DELETED = "INTERACTIVE_MAP_DELETED";
        public const string NODE_NOT_FOUND = "INTERACTIVE_MAP_NODE_NOT_FOUND";
        public const string NODE_CREATED = "INTERACTIVE_MAP_NODE_CREATED";
        public const string NODE_UPDATED = "INTERACTIVE_MAP_NODE_UPDATED";
        public const string NODE_DELETED = "INTERACTIVE_MAP_NODE_DELETED";
    }

    public static class InteractiveCity
    {
        public const string SCENARIO_NOT_FOUND = "SCENARIO_NOT_FOUND";
        public const string TECHNOLOGY_NOT_FOUND = "TECHNOLOGY_NOT_FOUND";
    }

    public static class Evaluation
    {
        public const string EVALUATION_NOT_FOUND = "EVALUATION_NOT_FOUND";
        public const string EVALUATION_SUBMITTED = "EVALUATION_SUBMITTED";
    }

    public static class InterestTopic
    {
        public const string INTEREST_TOPIC_NOT_FOUND = "INTEREST_TOPIC_NOT_FOUND";
        public const string INTEREST_TOPIC_CREATED = "INTEREST_TOPIC_CREATED";
        public const string INTEREST_TOPIC_UPDATED = "INTEREST_TOPIC_UPDATED";
        public const string INTEREST_TOPIC_DELETED = "INTEREST_TOPIC_DELETED";
    }

    public static class Validation
    {
        public const string REQUIRED_FIELD = "REQUIRED_FIELD";
        public const string INVALID_EMAIL = "INVALID_EMAIL";
        public const string INVALID_PHONE = "INVALID_PHONE";
        public const string MIN_LENGTH = "MIN_LENGTH";
        public const string MAX_LENGTH = "MAX_LENGTH";
        public const string INVALID_FORMAT = "INVALID_FORMAT";
        public const string INVALID_ENUM = "INVALID_ENUM";
        public const string PASSWORD_UPPERCASE = "PASSWORD_UPPERCASE";
        public const string PASSWORD_LOWERCASE = "PASSWORD_LOWERCASE";
        public const string PASSWORD_NUMBER = "PASSWORD_NUMBER";
        public const string PASSWORD_POLICY = "PASSWORD_POLICY";
        public const string PASSWORDS_MUST_MATCH = "PASSWORDS_MUST_MATCH";
    }
}
