namespace CCE.Application.Errors;

public static class ApplicationErrors
{
    public static class General
    {
        public const string VALIDATION_ERROR = "VALIDATION_ERROR";
        public const string INTERNAL_ERROR = "INTERNAL_ERROR";
        public const string UNAUTHORIZED = "UNAUTHORIZED_ACCESS";
        public const string FORBIDDEN = "FORBIDDEN_ACCESS";
        public const string NOT_FOUND = "RESOURCE_NOT_FOUND";
        public const string BAD_REQUEST = "BAD_REQUEST";
        public const string SUCCESS_CREATED = "SUCCESS_CREATED";
        public const string SUCCESS_UPDATED = "SUCCESS_UPDATED";
        public const string SUCCESS_DELETED = "SUCCESS_DELETED";
        public const string SUCCESS_OPERATION = "SUCCESS_OPERATION";
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
    }

    public static class Content
    {
        public const string RESOURCE_NOT_FOUND = "RESOURCE_NOT_FOUND";
        public const string RESOURCE_DUPLICATE = "RESOURCE_DUPLICATE";
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
        public const string COUNTRY_RESOURCE_REQUEST_NOT_FOUND = "COUNTRY_RESOURCE_REQUEST_NOT_FOUND";
    }

    public static class Community
    {
        public const string TOPIC_NOT_FOUND = "TOPIC_NOT_FOUND";
        public const string TOPIC_DUPLICATE = "TOPIC_DUPLICATE";
        public const string POST_NOT_FOUND = "POST_NOT_FOUND";
        public const string REPLY_NOT_FOUND = "REPLY_NOT_FOUND";
        public const string RATING_NOT_FOUND = "RATING_NOT_FOUND";
        public const string ALREADY_FOLLOWING = "ALREADY_FOLLOWING";
        public const string NOT_FOLLOWING = "NOT_FOLLOWING";
        public const string CANNOT_MARK_ANSWERED = "CANNOT_MARK_ANSWERED";
        public const string EDIT_WINDOW_EXPIRED = "EDIT_WINDOW_EXPIRED";
    }

    public static class Country
    {
        public const string COUNTRY_NOT_FOUND = "COUNTRY_NOT_FOUND";
        public const string COUNTRY_PROFILE_NOT_FOUND = "COUNTRY_PROFILE_NOT_FOUND";
    }

    public static class Notifications
    {
        public const string TEMPLATE_NOT_FOUND = "TEMPLATE_NOT_FOUND";
        public const string TEMPLATE_DUPLICATE = "TEMPLATE_DUPLICATE";
        public const string NOTIFICATION_NOT_FOUND = "NOTIFICATION_NOT_FOUND";
    }

    public static class KnowledgeMap
    {
        public const string MAP_NOT_FOUND = "MAP_NOT_FOUND";
        public const string NODE_NOT_FOUND = "NODE_NOT_FOUND";
        public const string EDGE_NOT_FOUND = "EDGE_NOT_FOUND";
    }

    public static class InteractiveCity
    {
        public const string SCENARIO_NOT_FOUND = "SCENARIO_NOT_FOUND";
        public const string TECHNOLOGY_NOT_FOUND = "TECHNOLOGY_NOT_FOUND";
    }

    public static class Validation
    {
        public const string REQUIRED_FIELD = "REQUIRED_FIELD";
        public const string INVALID_EMAIL = "INVALID_EMAIL";
        public const string MIN_LENGTH = "MIN_LENGTH";
        public const string MAX_LENGTH = "MAX_LENGTH";
        public const string INVALID_FORMAT = "INVALID_FORMAT";
        public const string INVALID_ENUM = "INVALID_ENUM";
    }
}
