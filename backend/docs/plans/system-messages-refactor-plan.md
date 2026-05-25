# System Messages Refactor Plan — From Error Codes to Unified Response Envelope

## Problem Statement

The current system was designed around an **"error codes"** mindset, but in reality the codebase already uses codes for **success messages** too (`CON005`, `CON011`, `CON017`). This creates several fundamental problems:

### 1. Naming Lie — "Error" used for success
```csharp
// Current: The Error record is used for BOTH success and failure
public sealed record Error(string Code, string MessageAr, string MessageEn, ErrorType Type, ...);

// In ErrorCodeMapper — success codes live in an "error" mapper:
["IDENTITY_USER_CREATED"] = "CON017",      // ← This isn't an error!
["IDENTITY_LOGOUT_SUCCESS"] = "CON015",    // ← This isn't an error!
["GENERAL_SUCCESS_CREATED"] = "CON011",    // ← This isn't an error!
```

### 2. No Success Message in the Response Envelope
```json
// Current success response — NO message for the frontend to display
{
  "isSuccess": true,
  "data": { "id": "...", "email": "..." },
  "error": null           // ← Where does "تم الإنشاء بنجاح" go?
}
```

The frontend gets **no code and no bilingual message** on success. It must hardcode its own toast messages.

### 3. Duplicate/Ambiguous Numeric Codes
Many different errors share the same code — 15+ different "not found" errors all map to `ERR001`. Frontend can't distinguish between "User not found" and "News not found". Same code, different meaning.

### 4. No `errors[]` Array for Validation
```json
// Current validation error — details buried inside the Error record
{
  "isSuccess": false,
  "error": {
    "code": "ERR013",
    "details": { "Email": ["REQUIRED_FIELD"] }  // ← keys are field names, values are code strings
  }
}
```

The frontend wants a flat `errors[]` array with per-field codes it can map to inline messages.

### 5. `Result<T>` Only Carries One Error
Current `Result<T>` has a single `Error?` property. There's no way to return multiple errors (e.g., "email is invalid AND phone is missing").

---

## Target Response Shape

Every API endpoint returns this shape — success AND failure. The `code` field uses the **`ERR0xx` / `CON0xx` / `VAL0xx`** numbering convention, but every message now gets its own **unique** code (no more 15 things sharing `ERR001`).

```json
// ─── Success ───
{
  "success": true,
  "code": "CON017",
  "message": {
    "ar": "تم إنشاء المستخدم بنجاح!",
    "en": "User created successfully!"
  },
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com"
  },
  "errors": [],
  "traceId": "00-abc123def456...",
  "timestamp": "2026-05-15T16:00:00Z"
}

// ─── Single Error ───
{
  "success": false,
  "code": "ERR019",
  "message": {
    "ar": "عذرًا، حدثت مشكلة أثناء إنشاء الحساب",
    "en": "Sorry, a problem occurred while creating the account"
  },
  "data": null,
  "errors": [],
  "traceId": "00-abc123def456...",
  "timestamp": "2026-05-15T16:00:00Z"
}

// ─── Validation Error (multiple field errors) ───
{
  "success": false,
  "code": "VAL001",
  "message": {
    "ar": "عذرًا، البيانات المدخلة غير صحيحة",
    "en": "Sorry, the entered data is invalid"
  },
  "data": null,
  "errors": [
    {
      "field": "email",
      "code": "VAL003",
      "message": {
        "ar": "البريد الإلكتروني غير صالح",
        "en": "Invalid email format"
      }
    },
    {
      "field": "phoneNumber",
      "code": "VAL002",
      "message": {
        "ar": "هذا الحقل مطلوب",
        "en": "This field is required"
      }
    }
  ],
  "traceId": "00-abc123def456...",
  "timestamp": "2026-05-15T16:00:00Z"
}
```

### Code Numbering Convention

| Prefix | Range | Usage |
|---|---|---|
| `ERR` | `ERR001`–`ERR999` | Errors (not found, conflict, unauthorized, forbidden, business rule, internal) |
| `CON` | `CON001`–`CON999` | Confirmations / Success messages (created, updated, deleted, etc.) |
| `VAL` | `VAL001`–`VAL999` | Validation errors (required, format, length, etc.) |

**Rule: Every distinct message gets its own unique number.** No more sharing `ERR001` across 15 different "not found" errors.

### Key Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Code format | `ERR0xx` / `CON0xx` / `VAL0xx` | Compact, sortable, familiar to frontend team, distinguishes error/success/validation at a glance |
| Each message = unique code | Yes — no duplicates | Frontend can `switch` on code, support tickets reference exact code |
| `message` is always an object | `{ "ar": "...", "en": "..." }` | Frontend picks the locale it needs, no server-side content negotiation |
| `errors[]` always present | Empty array on success or non-validation failure | Frontend doesn't need `null` checks |
| `traceId` + `timestamp` | Always present | Debugging, logging, support tickets |
| `data` is `null` on failure | Always | Clean separation |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│  Handler                                                     │
│                                                              │
│  return Response<UserDto>.Success(dto, MessageCode.UserCreated);      │
│  return Response<UserDto>.Fail(MessageCode.UserNotFound, ...);        │
│  (never throw for expected failures)                         │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  ValidationBehavior<TRequest, TResponse> (MediatR Pipeline)  │
│  Catches FluentValidation failures → Response with errors[]  │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Endpoint                                                    │
│                                                              │
│  var response = await mediator.Send(cmd, ct);                │
│  return response.ToHttpResult();  // one-liner               │
│                                                              │
│  Maps MessageType → HTTP status automatically:               │
│    Success      → 200/201/204                                │
│    NotFound     → 404                                        │
│    Validation   → 400                                        │
│    Conflict     → 409                                        │
│    Forbidden    → 403                                        │
│    Unauthorized → 401                                        │
│    BusinessRule → 422                                        │
│    Internal     → 500                                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Phase 0 — New Core Types (Domain + Application Layer)

### Step 0.1 — Rename `ErrorType` → `MessageType`, add `Success`

**File:** `src/CCE.Domain/Common/MessageType.cs` (new — replaces `Error.cs`)

```csharp
using System.Text.Json.Serialization;

namespace CCE.Domain.Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageType
{
    Success,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    BusinessRule,
    Internal
}
```

### Step 0.2 — Create `LocalizedMessage` Value Object

**File:** `src/CCE.Domain/Common/LocalizedMessage.cs` (new)

```csharp
namespace CCE.Domain.Common;

/// <summary>
/// Bilingual message that serializes as { "ar": "...", "en": "..." }.
/// </summary>
public sealed record LocalizedMessage(string Ar, string En);
```

### Step 0.3 — Create `FieldError` Record

**File:** `src/CCE.Domain/Common/FieldError.cs` (new)

```csharp
namespace CCE.Domain.Common;

/// <summary>
/// Per-field validation error for the errors[] array.
/// </summary>
public sealed record FieldError(
    string Field,
    string Code,
    LocalizedMessage Message);
```

### Step 0.4 — Create the New `Response<T>` Envelope

**File:** `src/CCE.Application/Common/Response.cs` (new)

```csharp
using CCE.Domain.Common;
using System.Text.Json.Serialization;

namespace CCE.Application.Common;

/// <summary>
/// Unified API response envelope. Every endpoint returns this shape.
/// Replaces <see cref="Result{T}"/> with proper success messages and error arrays.
/// Code field uses ERR0xx/CON0xx/VAL0xx numbering.
/// </summary>
public sealed record Response<T>
{
    [JsonInclude] public bool Success { get; private init; }
    [JsonInclude] public string Code { get; private init; } = string.Empty;
    [JsonInclude] public LocalizedMessage Message { get; private init; } = new("", "");
    [JsonInclude] public T? Data { get; private init; }
    [JsonInclude] public IReadOnlyList<FieldError> Errors { get; private init; } = [];
    [JsonInclude] public string TraceId { get; init; } = string.Empty;
    [JsonInclude] public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Not serialized — used internally to select HTTP status.</summary>
    [JsonIgnore] public MessageType Type { get; private init; } = MessageType.Success;

    public Response() { }

    // ─── Success Factories ───

    public static Response<T> Ok(T data, string code, LocalizedMessage message) => new()
    {
        Success = true,
        Code = code,
        Message = message,
        Data = data,
        Type = MessageType.Success,
    };

    /// <summary>Shorthand for void commands that return no data.</summary>
    public static Response<VoidData> Ok(string code, LocalizedMessage message) => new()
    {
        Success = true,
        Code = code,
        Message = message,
        Data = VoidData.Instance,
        Type = MessageType.Success,
    };

    // ─── Failure Factories ───

    public static Response<T> Fail(string code, LocalizedMessage message, MessageType type) => new()
    {
        Success = false,
        Code = code,
        Message = message,
        Type = type,
    };

    public static Response<T> Fail(
        string code, LocalizedMessage message, MessageType type, IReadOnlyList<FieldError> errors) => new()
    {
        Success = false,
        Code = code,
        Message = message,
        Type = type,
        Errors = errors,
    };

    // ─── Implicit conversions for clean handler returns ───
    // NOTE: Implicit conversion removed — every success must provide an explicit code.
}

/// <summary>Placeholder type for commands that return no data.</summary>
public sealed record VoidData
{
    public static readonly VoidData Instance = new();
    private VoidData() { }
}

/// <summary>Non-generic companion for void commands.</summary>
public static class Response
{
    public static Response<VoidData> Ok(string code, LocalizedMessage message)
        => Response<VoidData>.Ok(code, message);

    public static Response<VoidData> Fail(string code, LocalizedMessage message, MessageType type)
        => Response<VoidData>.Fail(code, message, type);
}
```

---

## Phase 1 — Unified Message Code System

### Step 1.1 — Create `SystemCode` Constants (replaces `ApplicationErrors` + `ErrorCodeMapper`)

The old system had two disconnected layers: domain keys (`IDENTITY_USER_NOT_FOUND`) mapped to numeric codes (`ERR001`) in `ErrorCodeMapper`. The problem: many domain keys shared the same numeric code, making debugging impossible.

**New rule: every distinct message gets its own unique `ERR0xx` / `CON0xx` / `VAL0xx` code.**

**File:** `src/CCE.Application/Messages/SystemCode.cs` (new)

Each constant IS the numeric code. The same string is used as the key in `Resources.yaml`.

```csharp
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
```

### Step 1.2 — Create Mapping from Domain Keys → System Codes

**File:** `src/CCE.Application/Messages/SystemCodeMap.cs` (new — replaces `ErrorCodeMapper.cs`)

This maps the internal domain keys (used in `Resources.yaml` and handlers) to the `ERR`/`CON`/`VAL` codes sent to clients. Unlike the old mapper, **every entry is unique — no shared codes.**

```csharp
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
```

### Step 1.3 — Create `MessageFactory` (replaces `Errors` class)

**File:** `src/CCE.Application/Messages/MessageFactory.cs` (new — replaces `Common/Errors.cs`)

The factory takes **domain keys** (human-readable, used in YAML), resolves the localized message, and maps to `ERR`/`CON`/`VAL` codes for the response.

```csharp
using CCE.Application.Common;
using CCE.Application.Localization;
using CCE.Domain.Common;

namespace CCE.Application.Messages;

/// <summary>
/// Factory for building <see cref="Response{T}"/> instances with localized messages.
/// Takes domain keys (e.g. "USER_NOT_FOUND"), resolves bilingual message from Resources.yaml,
/// and maps to system codes (e.g. "ERR001") via <see cref="SystemCodeMap"/>.
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

    // ─── Private ───

    private Response<T> Fail<T>(string domainKey, MessageType type)
    {
        var code = SystemCodeMap.ToSystemCode(domainKey);
        var msg = Localize(domainKey);
        return Response<T>.Fail(code, msg, type);
    }

    private LocalizedMessage Localize(string domainKey)
    {
        var raw = _l.GetLocalizedMessage(domainKey);
        return new LocalizedMessage(raw.Ar, raw.En);
    }
}
```

---

## Phase 2 — Update `ResponseExtensions` (API Layer)

### Step 2.1 — Create `ResponseExtensions`

**File:** `src/CCE.Api.Common/Extensions/ResponseExtensions.cs` (new — replaces `ResultExtensions.cs`)

```csharp
using CCE.Application.Common;
using CCE.Domain.Common;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace CCE.Api.Common.Extensions;

public static class ResponseExtensions
{
    /// <summary>
    /// Maps a <see cref="Response{T}"/> to an <see cref="IResult"/> with correct HTTP status,
    /// injecting traceId and timestamp.
    /// </summary>
    public static IResult ToHttpResult<T>(this Response<T> response, int successStatusCode = StatusCodes.Status200OK)
    {
        // Stamp traceId + timestamp
        var stamped = response with
        {
            TraceId = Activity.Current?.Id ?? string.Empty,
            Timestamp = DateTimeOffset.UtcNow,
        };

        if (stamped.Success)
        {
            return successStatusCode switch
            {
                StatusCodes.Status204NoContent => Results.NoContent(),
                _ => Results.Json(stamped, statusCode: successStatusCode),
            };
        }

        var statusCode = stamped.Type switch
        {
            MessageType.NotFound => StatusCodes.Status404NotFound,
            MessageType.Validation => StatusCodes.Status400BadRequest,
            MessageType.Conflict => StatusCodes.Status409Conflict,
            MessageType.Unauthorized => StatusCodes.Status401Unauthorized,
            MessageType.Forbidden => StatusCodes.Status403Forbidden,
            MessageType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Json(stamped, statusCode: statusCode);
    }

    public static IResult ToCreatedHttpResult<T>(this Response<T> response)
        => response.ToHttpResult(StatusCodes.Status201Created);

    public static IResult ToNoContentHttpResult(this Response<VoidData> response)
        => response.ToHttpResult(StatusCodes.Status204NoContent);
}
```

### Step 2.2 — Update `ExceptionHandlingMiddleware`

The middleware becomes a safety net that wraps unexpected exceptions into `Response<object>`:

```csharp
// Key changes:
// 1. Return Response<object> shape instead of anonymous { isSuccess, data, error }
// 2. Use SystemCodeMap.ToSystemCode() to resolve ERR/CON/VAL codes
// 3. Validation errors produce errors[] array with FieldError items
// 4. Every response includes traceId + timestamp
```

---

## Phase 3 — Migrate Handlers (Feature-by-Feature)

Each handler migration follows this pattern:

### Before (current):
```csharp
public class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommand, Result<AuthUserDto>>
{
    private readonly Errors _errors;

    public async Task<Result<AuthUserDto>> Handle(...)
    {
        // On failure:
        return _errors.EmailExists();        // returns Error record with code "ERR019"
        // On success:
        return dto;                          // implicit conversion, NO message, no code
    }
}
```

### After (new):
```csharp
public class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommand, Response<AuthUserDto>>
{
    private readonly MessageFactory _msg;

    public async Task<Response<AuthUserDto>> Handle(...)
    {
        // On failure → response.code = "ERR019", response.message = { ar: "...", en: "..." }
        return _msg.EmailExists<AuthUserDto>();
        // or explicit: return _msg.Conflict<AuthUserDto>("EMAIL_EXISTS");

        // On success → response.code = "CON002", response.message = { ar: "تم إنشاء الحساب بنجاح", en: "Account created successfully" }
        return _msg.Ok(dto, "REGISTER_SUCCESS");
    }
}
```

**What the frontend receives:**
```json
// Success case:
{ "success": true, "code": "CON002", "message": { "ar": "...", "en": "..." }, "data": {...}, "errors": [] }

// Failure case:
{ "success": false, "code": "ERR019", "message": { "ar": "...", "en": "..." }, "data": null, "errors": [] }
```

### Migration Order (by domain):

| # | Domain | Handlers | Priority |
|---|--------|----------|----------|
| 1 | Identity/Auth | Login, Register, Logout, RefreshToken, ForgotPassword, ResetPassword | 🔴 High |
| 2 | Identity/Commands | AssignRoles, ApproveExpert, RejectExpert, CreateStateRep, RevokeStateRep | 🔴 High |
| 3 | Identity/Queries | GetUserById, GetMyProfile, GetMyExpertStatus | 🟡 Medium |
| 4 | Identity/Public | SubmitExpertRequest, UpdateMyProfile | 🟡 Medium |
| 5 | Content/* | All news, events, resources, pages, categories, assets, homepage handlers | 🟡 Medium |
| 6 | Community/* | Topics, posts, replies, ratings, follows | 🟢 Low |
| 7 | Country/* | Countries, profiles | 🟢 Low |
| 8 | Notifications/* | Templates, user notifications | 🟢 Low |
| 9 | KnowledgeMap/* | Maps, nodes, edges | 🟢 Low |
| 10 | InteractiveCity/* | Scenarios, technologies | 🟢 Low |

---

## Phase 4 — Update `ValidationBehavior`

### Step 4.1 — New `ResponseValidationBehavior`

**File:** `src/CCE.Application/Common/Behaviors/ResponseValidationBehavior.cs` (new)

```csharp
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using FluentValidation;
using MediatR;

namespace CCE.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that catches FluentValidation failures
/// and converts them to Response{T} with errors[] array.
/// </summary>
public sealed class ResponseValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILocalizationService _l;

    public ResponseValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILocalizationService l)
    {
        _validators = validators;
        _l = l;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any())
            return await next().ConfigureAwait(false);

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, ct))).ConfigureAwait(false);

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count == 0)
            return await next().ConfigureAwait(false);

        // Check if TResponse is Response<T>
        var responseType = typeof(TResponse);
        if (responseType.IsGenericType &&
            responseType.GetGenericTypeDefinition() == typeof(Response<>))
        {
            var fieldErrors = failures.Select(f =>
            {
                var domainKey = f.ErrorMessage;  // We use domain key as ErrorMessage in validators
                var valCode = SystemCodeMap.ToSystemCode(domainKey); // e.g. "REQUIRED_FIELD" → "VAL002"
                var msg = _l.GetLocalizedMessage(domainKey);
                return new FieldError(
                    ToCamelCase(f.PropertyName),
                    valCode,
                    new LocalizedMessage(msg.Ar, msg.En));
            }).ToList();

            var headerDomainKey = "VALIDATION_ERROR";
            var headerCode = SystemCodeMap.ToSystemCode(headerDomainKey); // → "VAL001"
            var headerMsg = _l.GetLocalizedMessage(headerDomainKey);

            // Build Response<T>.Fail via reflection or known factory
            var failMethod = responseType.GetMethod("Fail",
                new[] { typeof(string), typeof(LocalizedMessage), typeof(MessageType), typeof(IReadOnlyList<FieldError>) });

            return (TResponse)failMethod!.Invoke(null, new object[]
            {
                headerCode,  // "VAL001"
                new LocalizedMessage(headerMsg.Ar, headerMsg.En),
                MessageType.Validation,
                fieldErrors  // Each item has its own VAL0xx code
            })!;
        }

        // Fallback for non-Response handlers — throw as before
        throw new ValidationException(failures);
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
```

---

## Phase 5 — Update Resources.yaml

`Resources.yaml` still uses **domain keys** (human-readable) as the lookup key. The `SystemCodeMap` resolves domain key → `ERR`/`CON`/`VAL` code. No changes to how YAML is structured.

Ensure every domain key referenced by `SystemCodeMap` has a corresponding YAML entry. New keys to add:

```yaml
# ─── New keys for domain keys that didn't exist in YAML before ───
REGISTRATION_FAILED:
  ar: "عذرًا، حدثت مشكلة أثناء إنشاء الحساب"
  en: "Sorry, a problem occurred while creating the account"

EXPERT_REQUEST_NOT_FOUND:
  ar: "طلب الخبير غير موجود"
  en: "Expert request not found"

EXPERT_REQUEST_ALREADY_EXISTS:
  ar: "لديك طلب خبير موجود بالفعل"
  en: "You already have an existing expert request"

STATE_REP_ASSIGNMENT_NOT_FOUND:
  ar: "تعيين ممثل الولاية غير موجود"
  en: "State representative assignment not found"

STATE_REP_ASSIGNMENT_EXISTS:
  ar: "تعيين ممثل الولاية موجود بالفعل"
  en: "State representative assignment already exists"

NEWS_NOT_FOUND:
  ar: "الخبر غير موجود"
  en: "News not found"

EVENT_NOT_FOUND:
  ar: "الفعالية غير موجودة"
  en: "Event not found"

PAGE_NOT_FOUND:
  ar: "الصفحة غير موجودة"
  en: "Page not found"

CATEGORY_NOT_FOUND:
  ar: "التصنيف غير موجود"
  en: "Category not found"

ASSET_NOT_FOUND:
  ar: "الملف غير موجود"
  en: "Asset not found"

HOMEPAGE_SECTION_NOT_FOUND:
  ar: "قسم الصفحة الرئيسية غير موجود"
  en: "Homepage section not found"

RESOURCE_DUPLICATE:
  ar: "المورد بهذا العنوان موجود بالفعل"
  en: "Resource with this title already exists"

CATEGORY_DUPLICATE:
  ar: "التصنيف بهذا الاسم موجود بالفعل"
  en: "Category with this name already exists"

PAGE_DUPLICATE:
  ar: "الصفحة بهذا العنوان موجودة بالفعل"
  en: "Page with this slug already exists"

NEWS_DUPLICATE:
  ar: "الخبر بهذا العنوان موجود بالفعل"
  en: "News with this title already exists"

EVENT_DUPLICATE:
  ar: "الفعالية بهذا العنوان موجودة بالفعل"
  en: "Event with this title already exists"

TOPIC_NOT_FOUND:
  ar: "الموضوع غير موجود"
  en: "Topic not found"

POST_NOT_FOUND:
  ar: "المنشور غير موجود"
  en: "Post not found"

REPLY_NOT_FOUND:
  ar: "الرد غير موجود"
  en: "Reply not found"

TOPIC_DUPLICATE:
  ar: "الموضوع بهذا العنوان موجود بالفعل"
  en: "Topic with this title already exists"

ALREADY_FOLLOWING:
  ar: "أنت تتابع هذا الموضوع بالفعل"
  en: "You are already following this topic"

NOT_FOLLOWING:
  ar: "أنت لا تتابع هذا الموضوع"
  en: "You are not following this topic"

CANNOT_MARK_ANSWERED:
  ar: "لا يمكنك تحديد هذا الرد كإجابة"
  en: "You cannot mark this reply as answered"

EDIT_WINDOW_EXPIRED:
  ar: "انتهت فترة التعديل المسموح بها"
  en: "Edit window has expired"

COUNTRY_NOT_FOUND:
  ar: "الدولة غير موجودة"
  en: "Country not found"

COUNTRY_PROFILE_NOT_FOUND:
  ar: "ملف الدولة غير موجود"
  en: "Country profile not found"

# ... (ensure all domain keys in SystemCodeMap have a YAML entry)
```

### YAML ↔ Code Flow

```
Handler calls: _msg.NotFound<T>("NEWS_NOT_FOUND")
    ↓
MessageFactory:
    1. SystemCodeMap.ToSystemCode("NEWS_NOT_FOUND") → "ERR040"
    2. _l.GetLocalizedMessage("NEWS_NOT_FOUND")    → { Ar: "الخبر غير موجود", En: "News not found" }
    ↓
Response JSON:
    { "success": false, "code": "ERR040", "message": { "ar": "الخبر غير موجود", "en": "News not found" }, ... }
```

---

## Phase 6 — Delete Deprecated Files

After all handlers are migrated and tests pass:

| File | Action | Replaced By |
|---|---|---|
| `src/CCE.Application/Errors/ErrorCodeMapper.cs` | 🗑️ Delete | `Messages/SystemCodeMap.cs` |
| `src/CCE.Application/Errors/ApplicationErrors.cs` | 🗑️ Delete | `Messages/SystemCode.cs` |
| `src/CCE.Application/Common/Errors.cs` | 🗑️ Delete | `Messages/MessageFactory.cs` |
| `src/CCE.Application/Common/Result.cs` | 🗑️ Delete | `Common/Response.cs` |
| `src/CCE.Domain/Common/Error.cs` | 🗑️ Delete | `Common/MessageType.cs` + `LocalizedMessage.cs` + `FieldError.cs` |
| `src/CCE.Api.Common/Extensions/ResultExtensions.cs` | 🗑️ Delete | `Extensions/ResponseExtensions.cs` |
| `src/CCE.Application/Common/Behaviors/ResultValidationBehavior.cs` | 🗑️ Delete | `Behaviors/ResponseValidationBehavior.cs` |

---

## Phase 7 — Update Tests

### Test changes:
1. **Unit tests** — Assert on `response.Success`, `response.Code`, `response.Errors.Count`
2. **Integration tests** — Deserialize to `Response<T>` instead of `Result<T>`
3. **Architecture tests** — Update any rules that reference old types

### Example test:
```csharp
[Fact]
public async Task Register_DuplicateEmail_Returns_Conflict_With_ERR019()
{
    // Arrange ...
    var response = await _mediator.Send(command, CancellationToken.None);

    response.Success.Should().BeFalse();
    response.Code.Should().Be("ERR019");       // Email already exists
    response.Message.Ar.Should().NotBeNullOrWhiteSpace();
    response.Message.En.Should().NotBeNullOrWhiteSpace();
    response.Errors.Should().BeEmpty();
    response.Type.Should().Be(MessageType.Conflict);
}

[Fact]
public async Task Register_Success_Returns_CON002()
{
    // Arrange ...
    var response = await _mediator.Send(command, CancellationToken.None);

    response.Success.Should().BeTrue();
    response.Code.Should().Be("CON002");       // Register success
    response.Data.Should().NotBeNull();
    response.Errors.Should().BeEmpty();
}

[Fact]
public async Task Register_InvalidData_Returns_VAL001_With_FieldErrors()
{
    // Arrange ...
    var response = await _mediator.Send(command, CancellationToken.None);

    response.Success.Should().BeFalse();
    response.Code.Should().Be("VAL001");       // Validation error header
    response.Errors.Should().Contain(e => e.Field == "email" && e.Code == "VAL003");  // Invalid email
    response.Errors.Should().Contain(e => e.Field == "phoneNumber" && e.Code == "VAL002");  // Required field
}
```

---

## Migration Checklist Per Handler

For each handler file, follow this checklist:

- [ ] Change return type from `Result<T>` → `Response<T>`
- [ ] Change command/query `IRequest<Result<T>>` → `IRequest<Response<T>>`
- [ ] Replace `Errors _errors` injection → `MessageFactory _msg` injection
- [ ] Replace `return _errors.XxxNotFound()` → `return _msg.NotFound<T>("XXX_NOT_FOUND")` (resolves to `ERR0xx`)
- [ ] Replace `return dto` (implicit success) → `return _msg.Ok(dto, "XXX_CREATED")` (resolves to `CON0xx`)
- [ ] Replace `return Result.Success()` → `return _msg.Ok("SUCCESS_OPERATION")` (resolves to `CON900`)
- [ ] Update endpoint: `.ToHttpResult()` stays the same (new extension method has same name)
- [ ] Update unit test assertions
- [ ] Build + run tests

---

## Estimated Effort

| Phase | Files | Effort |
|---|---|---|
| Phase 0 — Core types | 4 new files | 1 day |
| Phase 1 — MessageCodes + Factory | 2 new files | 0.5 day |
| Phase 2 — ResponseExtensions + Middleware | 2 files (new + update) | 0.5 day |
| Phase 3 — Migrate handlers | ~40 handler files | 3–4 days |
| Phase 4 — ValidationBehavior | 1 file | 0.5 day |
| Phase 5 — Resources.yaml | 1 file | 0.5 day |
| Phase 6 — Delete deprecated | 7 files | 0.5 day |
| Phase 7 — Update tests | ~20 test files | 2 days |
| **Total** | | **~8–9 days** |

---

## Breaking Changes for Frontend

| Before | After |
|---|---|
| `isSuccess` | `success` |
| `error.code` = `"ERR019"` (shared across many errors) | `code` = `"ERR019"` (top-level, **unique** per message) |
| `error.messageAr` / `error.messageEn` | `message.ar` / `message.en` (top-level, always present) |
| `error.details` = `{ "Email": ["REQUIRED_FIELD"] }` | `errors[]` = `[{ field, code, message }]` — codes are `VAL002`, `VAL003`, etc. |
| No success message | `code` = `"CON002"` + `message` always present on success too |
| No `traceId` / `timestamp` | Always present |
| Same `ERR001` for 15+ different not-found errors | Each entity gets its own code: `ERR001`=User, `ERR040`=News, `ERR060`=Topic, etc. |

> **⚠️ Frontend must be updated simultaneously.** Coordinate with the frontend team on the new response shape. Consider versioning the API or deploying behind a feature flag.

---

## Optional: Backward Compatibility Strategy

If a hard cutover isn't possible, add a temporary `X-Response-Version: 2` header. The middleware checks this header and returns the new shape. Endpoints without the header return the old shape. Remove after frontend migration is complete.
