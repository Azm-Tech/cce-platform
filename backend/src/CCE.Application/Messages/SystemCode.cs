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

    // ─── Identity Errors (appendix-aligned) ───
    // ERR001-ERR018 reserved for appendix frontend codes
    public const string ERR001 = "ERR001"; // User not found (also used as ERR001 in appendix — keep)
    public const string ERR002 = "ERR002"; // Resource download failure (appendix)
    public const string ERR003 = "ERR003"; // Resource share failure (appendix)
    public const string ERR005 = "ERR005"; // News follow failure (US012)
    public const string ERR004 = "ERR004"; // No verified contact (email/phone)
    public const string ERR013 = "ERR013"; // Required fields empty (appendix)

    public const string ERR019 = "ERR019"; // Email already exists / Account creation failure (appendix)
    public const string ERR020 = "ERR020"; // Invalid credentials (appendix)
    public const string ERR021 = "ERR021"; // Login system error (appendix)
    public const string ERR022 = "ERR022"; // Email not found in password recovery (appendix)
    public const string ERR023 = "ERR023"; // Password recovery system error
    public const string ERR024 = "ERR024"; // Logout failure
    public const string ERR025 = "ERR025"; // Content update failure (appendix)
    public const string ERR026 = "ERR026"; // User deletion failure (appendix)
    public const string ERR027 = "ERR027"; // News/event upload failure (appendix)
    public const string ERR028 = "ERR028"; // News/event deletion failure (appendix)
    public const string ERR029 = "ERR029"; // Resource upload failure (appendix)
    public const string ERR030 = "ERR030"; // Resource deletion failure (appendix)

    // ─── Backend-only Identity Errors (moved to free appendix numbers) ───
    public const string ERR400 = "ERR400"; // Expert request not found
    public const string ERR401 = "ERR401"; // State rep assignment not found
    public const string ERR402 = "ERR402"; // Invalid / expired token
    public const string ERR403 = "ERR403"; // Invalid refresh token
    public const string ERR404 = "ERR404"; // Account deactivated
    public const string ERR405 = "ERR405"; // Username already exists
    public const string ERR406 = "ERR406"; // Registration failed
    public const string ERR407 = "ERR407"; // Not authenticated
    public const string ERR408 = "ERR408"; // Expert request already exists
    public const string ERR409 = "ERR409"; // State rep assignment already exists

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
    public const string ERR059 = "ERR059"; // Asset not clean (virus scan not passed)

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
    public const string ERR069 = "ERR069"; // Post already published (draft edit rejected)
    public const string ERR140 = "ERR140"; // Community not found
    public const string ERR141 = "ERR141"; // Community join request not found
    public const string ERR142 = "ERR142"; // Poll not found
    public const string ERR143 = "ERR143"; // Poll is closed
    public const string ERR144 = "ERR144"; // Cannot follow self

    // ─── Country / State-Rep Errors ───
    public const string ERR070 = "ERR070"; // Country not found
    public const string ERR071 = "ERR071"; // Country profile not found
    public const string ERR072 = "ERR072"; // Country request processing failure (appendix ERR031)
    public const string ERR073 = "ERR073"; // Country scope forbidden (state rep editing another country)
    public const string ERR074 = "ERR074"; // No country assigned to the current state rep
    public const string ERR075 = "ERR075"; // KAPSARC data unavailable (appendix US014 ER001)

    // ─── Notification Errors ───
    public const string ERR080 = "ERR080"; // Template not found
    public const string ERR081 = "ERR081"; // Template duplicate
    public const string ERR082 = "ERR082"; // Notification not found

    // ─── KnowledgeMap Errors ───
    public const string ERR090 = "ERR090"; // Map not found
    public const string ERR091 = "ERR091"; // Node not found
    public const string ERR092 = "ERR092"; // Edge not found

    // ─── Content Errors (extended) ───
    public const string ERR093 = "ERR093"; // News follow not found

    // ─── Media Errors ───
    public const string ERR110 = "ERR110"; // Media file not found
    public const string ERR111 = "ERR111"; // Invalid file type
    public const string ERR112 = "ERR112"; // File too large
    public const string ERR113 = "ERR113"; // Empty file

    // ─── InteractiveCity Errors ───
    public const string ERR100 = "ERR100"; // Scenario not found
    public const string ERR101 = "ERR101"; // Technology not found

    // ─── InterestTopic Errors ───
    public const string ERR114 = "ERR114"; // Interest topic not found

    // ─── Platform Settings Errors ───
    public const string ERR053 = "ERR053"; // Homepage settings not found
    public const string ERR054 = "ERR054"; // About settings not found
    public const string ERR055 = "ERR055"; // Policies settings not found
    public const string ERR056 = "ERR056"; // Glossary entry not found
    public const string ERR057 = "ERR057"; // Knowledge partner not found
    public const string ERR058 = "ERR058"; // Policy section not found

    // ─── Lookups Errors ───
    public const string ERR130 = "ERR130"; // Country code not found

    // ─── Verification Errors ───
    public const string ERR120 = "ERR120"; // OTP not found
    public const string ERR121 = "ERR121"; // OTP expired
    public const string ERR122 = "ERR122"; // OTP invalid code
    public const string ERR123 = "ERR123"; // OTP max attempts exceeded
    public const string ERR124 = "ERR124"; // OTP cooldown active
    public const string ERR125 = "ERR125"; // OTP invalidated
    public const string ERR126 = "ERR126"; // Contact already taken

    // ─── Evaluation Errors ───
    public const string ERR009 = "ERR009"; // Evaluation not found

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

    // ─── Identity Success (appendix-aligned) ───
    public const string CON001 = "CON001"; // Resource download success (appendix)
    public const string CON002 = "CON002"; // Resource share success (appendix)
    public const string CON003 = "CON003"; // Generic share success (appendix)
    public const string CON004 = "CON004"; // Event added to calendar (appendix)
    public const string CON005 = "CON005"; // Profile update success (appendix)
    public const string CON006 = "CON006"; // Expert registration request submitted (appendix)
    public const string CON007 = "CON007"; // Admin notified of expert request (appendix)
    public const string CON008 = "CON008"; // Service evaluation submitted (appendix)
    public const string CON009 = "CON009"; // Personalized suggestions submitted (appendix)
    public const string CON019 = "CON019"; // Interest upserted
    public const string CON010 = "CON010"; // Topic follow success (appendix)
    public const string CON011 = "CON011"; // Post created (appendix)
    public const string CON012 = "CON012"; // Post follow success (appendix)
    public const string CON013 = "CON013"; // Reply submitted (appendix)
    public const string CON014 = "CON014"; // Password recovery success (appendix)
    public const string CON015 = "CON015"; // Logout success (appendix)
    public const string CON016 = "CON016"; // Content update success (appendix)
    public const string CON017 = "CON017"; // User creation success (appendix)
    public const string CON018 = "CON018"; // User deleted successfully (appendix)

    // ─── Backend-only Identity Success (appendix numbers already taken) ───
    public const string CON050 = "CON050"; // Expert request approved
    public const string CON051 = "CON051"; // Expert request rejected
    public const string CON052 = "CON052"; // State rep assignment created
    public const string CON053 = "CON053"; // State rep assignment revoked
    public const string CON054 = "CON054"; // Roles assigned
    public const string CON055 = "CON055"; // User status changed
    public const string CON056 = "CON056"; // Login success

    // ─── Country / State-Rep Success (appendix numbers CON023/024/026 already taken) ───
    public const string CON057 = "CON057"; // State profile updated (appendix CON026)
    public const string CON058 = "CON058"; // Country content request submitted (appendix CON024)
    public const string CON059 = "CON059"; // Country request processed (appendix CON023)
    public const string CON064 = "CON064"; // KAPSARC snapshot refreshed
    public const string CON065 = "CON065"; // Community post/reply vote recorded
    public const string CON066 = "CON066"; // Community post created/published
    public const string CON067 = "CON067"; // Community post draft saved
    public const string CON068 = "CON068"; // Community post published
    public const string CON069 = "CON069"; // Community post draft deleted

    // ─── InterestTopic Success ───
    public const string CON048 = "CON048"; // Interest topic created
    public const string CON049 = "CON049"; // Interest topic updated
    public const string CON072 = "CON072"; // Interest topic deleted

    // ─── Content Success ───
    public const string CON020 = "CON020"; // Content created
    public const string CON021 = "CON021"; // Resource created (BRD appendix)
    public const string CON022 = "CON022"; // Resource deleted (BRD appendix)
    public const string CON023 = "CON023"; // Content published
    public const string CON024 = "CON024"; // Content archived
    public const string CON025 = "CON025"; // Content updated
    public const string CON026 = "CON026"; // Resource updated
    public const string CON027 = "CON027"; // Content deleted
    public const string CON028 = "CON028"; // Resource published

    // ─── Media Success ───
    public const string CON029 = "CON029"; // Media uploaded
    public const string CON036 = "CON036"; // Media updated
    public const string CON037 = "CON037"; // Media deleted

    // ─── Asset Success ───
    public const string CON038 = "CON038"; // Asset uploaded

    // ─── Community Success ───
    public const string CON030 = "CON030"; // Topic created
    public const string CON031 = "CON031"; // Post created
    public const string CON032 = "CON032"; // Reply created
    public const string CON033 = "CON033"; // Followed successfully
    public const string CON034 = "CON034"; // Unfollowed successfully
    public const string CON035 = "CON035"; // Marked as answered

    // ─── Verification Success ───
    public const string CON060 = "CON060"; // OTP sent
    public const string CON061 = "CON061"; // OTP verified
    public const string CON062 = "CON062"; // Email updated
    public const string CON063 = "CON063"; // Phone updated

    // ─── Notification Success ───
    public const string CON040 = "CON040"; // Notification created
    public const string CON041 = "CON041"; // Notification marked read
    public const string CON042 = "CON042"; // Notification deleted
    public const string CON043 = "CON043"; // Notification settings updated
    public const string CON044 = "CON044"; // Notification retried
    public const string CON045 = "CON045"; // Notifications marked read
    public const string CON046 = "CON046"; // Notification template created
    public const string CON047 = "CON047"; // Notification template updated

    // ─── Lookups Success ───
    public const string CON070 = "CON070"; // Lookup created
    public const string CON071 = "CON071"; // Lookup updated

    // ─── General Success ───
    public const string CON100 = "CON100"; // Items listed successfully
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
