namespace CCE.Application.Messages;

/// <summary>
/// Canonical system message codes. Each constant is the code sent in the API response
/// AND the lookup key in Resources.yaml. Codes are unique — no two messages share a code.
///
/// Prefixes:
///   ERR = Error (failure responses)
///   CON = Confirmation (success responses)
///   VAL = Validation (field-level errors in errors[] array)
/// </summary>
public static class SystemCode
{
    // ════════════════════════════════════════════════════════════════
    //  ERR — Error codes (failures)
    // ════════════════════════════════════════════════════════════════

    // ─── Identity Errors ───
    public const string ERR001 = "ERR001"; // User not found
    public const string ERR002 = "ERR002"; // Expert request not found
    public const string ERR003 = "ERR003"; // State rep assignment not found

    public const string ERR019 = "ERR019"; // Email already exists
    public const string ERR020 = "ERR020"; // Invalid credentials
    public const string ERR021 = "ERR021"; // Invalid / expired token
    public const string ERR022 = "ERR022"; // Invalid refresh token
    public const string ERR023 = "ERR023"; // Password recovery failed
    public const string ERR024 = "ERR024"; // Logout failed
    public const string ERR025 = "ERR025"; // Account deactivated
    public const string ERR026 = "ERR026"; // Username already exists
    public const string ERR027 = "ERR027"; // Registration failed
    public const string ERR028 = "ERR028"; // Not authenticated
    public const string ERR029 = "ERR029"; // Expert request already exists
    public const string ERR030 = "ERR030"; // State rep assignment already exists

    // ─── Content Errors ───
    public const string ERR040 = "ERR040"; // News not found
    public const string ERR041 = "ERR041"; // Event not found
    public const string ERR042 = "ERR042"; // Resource not found
    public const string ERR043 = "ERR043"; // Page not found
    public const string ERR044 = "ERR044"; // Category not found
    public const string ERR045 = "ERR045"; // Asset not found
    public const string ERR046 = "ERR046"; // Homepage section not found
    public const string ERR047 = "ERR047"; // Country resource request not found
    public const string ERR048 = "ERR048"; // Resource duplicate (slug/title)
    public const string ERR049 = "ERR049"; // Category duplicate
    public const string ERR050 = "ERR050"; // Page duplicate
    public const string ERR051 = "ERR051"; // News duplicate
    public const string ERR052 = "ERR052"; // Event duplicate

    // ─── Community Errors ───
    public const string ERR060 = "ERR060"; // Topic not found
    public const string ERR061 = "ERR061"; // Post not found
    public const string ERR062 = "ERR062"; // Reply not found
    public const string ERR063 = "ERR063"; // Rating not found
    public const string ERR064 = "ERR064"; // Topic duplicate
    public const string ERR065 = "ERR065"; // Already following
    public const string ERR066 = "ERR066"; // Not following
    public const string ERR067 = "ERR067"; // Cannot mark answered
    public const string ERR068 = "ERR068"; // Edit window expired

    // ─── Country Errors ───
    public const string ERR070 = "ERR070"; // Country not found
    public const string ERR071 = "ERR071"; // Country profile not found

    // ─── Notification Errors ───
    public const string ERR080 = "ERR080"; // Template not found
    public const string ERR081 = "ERR081"; // Template duplicate
    public const string ERR082 = "ERR082"; // Notification not found

    // ─── KnowledgeMap Errors ───
    public const string ERR090 = "ERR090"; // Map not found
    public const string ERR091 = "ERR091"; // Node not found
    public const string ERR092 = "ERR092"; // Edge not found

    // ─── InteractiveCity Errors ───
    public const string ERR100 = "ERR100"; // Scenario not found
    public const string ERR101 = "ERR101"; // Technology not found

    // ─── General Errors ───
    public const string ERR900 = "ERR900"; // Internal server error
    public const string ERR901 = "ERR901"; // Unauthorized access
    public const string ERR902 = "ERR902"; // Forbidden access
    public const string ERR903 = "ERR903"; // Resource not found (generic)
    public const string ERR904 = "ERR904"; // Bad request (generic)
    public const string ERR905 = "ERR905"; // External API error
    public const string ERR906 = "ERR906"; // External API not configured
    public const string ERR907 = "ERR907"; // Concurrency conflict
    public const string ERR908 = "ERR908"; // Duplicate value (generic)

    // ════════════════════════════════════════════════════════════════
    //  CON — Confirmation / Success codes
    // ════════════════════════════════════════════════════════════════

    // ─── Identity Success ───
    public const string CON001 = "CON001"; // Login success
    public const string CON002 = "CON002"; // Register success
    public const string CON003 = "CON003"; // Logout success
    public const string CON004 = "CON004"; // Token refreshed
    public const string CON005 = "CON005"; // User updated
    public const string CON006 = "CON006"; // User created
    public const string CON007 = "CON007"; // User deleted
    public const string CON008 = "CON008"; // User activated
    public const string CON009 = "CON009"; // User deactivated
    public const string CON010 = "CON010"; // Roles assigned
    public const string CON011 = "CON011"; // Password reset success
    public const string CON012 = "CON012"; // Expert request submitted
    public const string CON013 = "CON013"; // Expert request approved
    public const string CON014 = "CON014"; // Expert request rejected
    public const string CON015 = "CON015"; // State rep assignment created
    public const string CON016 = "CON016"; // State rep assignment revoked
    public const string CON017 = "CON017"; // Profile updated

    // ─── Content Success ───
    public const string CON020 = "CON020"; // Content created
    public const string CON021 = "CON021"; // Content updated
    public const string CON022 = "CON022"; // Content deleted
    public const string CON023 = "CON023"; // Content published
    public const string CON024 = "CON024"; // Content archived
    public const string CON025 = "CON025"; // Resource created
    public const string CON026 = "CON026"; // Resource updated
    public const string CON027 = "CON027"; // Resource deleted
    public const string CON028 = "CON028"; // Resource published

    // ─── Community Success ───
    public const string CON030 = "CON030"; // Topic created
    public const string CON031 = "CON031"; // Post created
    public const string CON032 = "CON032"; // Reply created
    public const string CON033 = "CON033"; // Followed successfully
    public const string CON034 = "CON034"; // Unfollowed successfully
    public const string CON035 = "CON035"; // Marked as answered

    // ─── Notification Success ───
    public const string CON040 = "CON040"; // Notification created
    public const string CON041 = "CON041"; // Notification marked read
    public const string CON042 = "CON042"; // Notification deleted

    // ─── General Success ───
    public const string CON900 = "CON900"; // Operation completed successfully
    public const string CON901 = "CON901"; // Created successfully (generic)
    public const string CON902 = "CON902"; // Updated successfully (generic)
    public const string CON903 = "CON903"; // Deleted successfully (generic)

    // ════════════════════════════════════════════════════════════════
    //  VAL — Validation codes (used in errors[] array items)
    // ════════════════════════════════════════════════════════════════

    public const string VAL001 = "VAL001"; // Validation error (header-level)
    public const string VAL002 = "VAL002"; // Required field
    public const string VAL003 = "VAL003"; // Invalid email
    public const string VAL004 = "VAL004"; // Invalid phone
    public const string VAL005 = "VAL005"; // Min length violated
    public const string VAL006 = "VAL006"; // Max length violated
    public const string VAL007 = "VAL007"; // Invalid format
    public const string VAL008 = "VAL008"; // Invalid enum value
    public const string VAL009 = "VAL009"; // Password uppercase required
    public const string VAL010 = "VAL010"; // Password lowercase required
    public const string VAL011 = "VAL011"; // Password number required
}
