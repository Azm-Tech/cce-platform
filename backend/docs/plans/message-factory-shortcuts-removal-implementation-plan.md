# MessageFactory Shortcuts Removal — Implementation Plan

## Decision

Remove all shortcut/convenience methods from `MessageFactory`. Every handler calls the **9 core methods** directly with explicit `MessageKeys` constants. No shortcuts survive.

**Reason:** 83% of handlers already use the generic API. Shortcuts benefit only 17% while forcing every reader to ask "does a shortcut exist for this?" before writing a handler. Three handlers already mix both styles — proof that shortcuts create confusion rather than clarity.

---

## Target API (9 methods — unchanged, no removals here)

```csharp
// Success
Ok<T>(T data, string domainKey)
Ok(string domainKey)                    // → Response<VoidData>

// Failure
NotFound<T>(string domainKey)
Conflict<T>(string domainKey)
Unauthorized<T>(string domainKey)
Forbidden<T>(string domainKey)
BusinessRule<T>(string domainKey)
ValidationError<T>(string domainKey, IReadOnlyList<FieldError> fieldErrors)

// FieldError builder
Field(string fieldName, string domainKey)
```

---

## Phase 1 — Remove shortcuts from MessageFactory.cs

Delete lines 77–181 of `src/CCE.Application/Messages/MessageFactory.cs` (everything after `// ─── Private ───`).

The file ends at:

```csharp
    // ─── Private ───

    private Response<T> Fail<T>(string domainKey, MessageType type) { ... }
    private string ResolveCode(string domainKey) { ... }
    private string Localize(string domainKey) { ... }
}
```

---

## Phase 2 — Update every call site

### 2.1 How to find call sites

```powershell
# List all handler files using shortcuts (any _msg. call that isn't Ok/NotFound/Conflict/Unauthorized/Forbidden/BusinessRule/ValidationError/Field)
Select-String -Path "src\CCE.Application\**\*.cs" -Pattern "_msg\.\w+\(" -SimpleMatch | Where-Object { $_ -notmatch "_msg\.(Ok|NotFound|Conflict|Unauthorized|Forbidden|BusinessRule|ValidationError|Field)\(" }
```

### 2.2 Complete replacement map

Apply every substitution below. All call sites are in `src/CCE.Application/`.

> **Note:** Handlers that currently don't import `MessageKeys` will need `using CCE.Application.Messages;` added — the build will tell you exactly which files.

#### Identity domain

| Remove | Replace with |
|--------|-------------|
| `_msg.UserNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Identity.USER_NOT_FOUND)` |
| `_msg.InterestUpserted<T>(data)` | `_msg.Ok(data, MessageKeys.Identity.INTEREST_UPSERTED)` |
| `_msg.EmailExists<T>()` | `_msg.Conflict<T>(MessageKeys.Identity.EMAIL_EXISTS)` |
| `_msg.InvalidCredentials<T>()` | `_msg.Unauthorized<T>(MessageKeys.Identity.INVALID_CREDENTIALS)` |
| `_msg.NotAuthenticated<T>()` | `_msg.Unauthorized<T>(MessageKeys.Identity.NOT_AUTHENTICATED)` |
| `_msg.AccountDeactivated<T>()` | `_msg.Forbidden<T>(MessageKeys.Identity.ACCOUNT_DEACTIVATED)` |
| `_msg.ContactNotVerified<T>()` | `_msg.Forbidden<T>(MessageKeys.Identity.CONTACT_NOT_VERIFIED)` |
| `_msg.ExpertRequestNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Identity.EXPERT_REQUEST_NOT_FOUND)` |

#### Verification domain

| Remove | Replace with |
|--------|-------------|
| `_msg.OtpNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Verification.OTP_NOT_FOUND)` |
| `_msg.OtpExpired<T>()` | `_msg.BusinessRule<T>(MessageKeys.Verification.OTP_EXPIRED)` |
| `_msg.OtpInvalidCode<T>()` | `_msg.BusinessRule<T>(MessageKeys.Verification.OTP_INVALID_CODE)` |
| `_msg.OtpMaxAttempts<T>()` | `_msg.BusinessRule<T>(MessageKeys.Verification.OTP_MAX_ATTEMPTS)` |
| `_msg.OtpCooldownActive<T>()` | `_msg.BusinessRule<T>(MessageKeys.Verification.OTP_COOLDOWN_ACTIVE)` |
| `_msg.OtpInvalidated<T>()` | `_msg.BusinessRule<T>(MessageKeys.Verification.OTP_INVALIDATED)` |
| `_msg.ContactAlreadyTaken<T>()` | `_msg.Conflict<T>(MessageKeys.Verification.CONTACT_ALREADY_TAKEN)` |
| `_msg.EmailUpdated()` | `_msg.Ok(MessageKeys.Verification.EMAIL_UPDATED)` |
| `_msg.PhoneUpdated()` | `_msg.Ok(MessageKeys.Verification.PHONE_UPDATED)` |

#### Content domain

| Remove | Replace with |
|--------|-------------|
| `_msg.NewsNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Content.NEWS_NOT_FOUND)` |
| `_msg.EventNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Content.EVENT_NOT_FOUND)` |
| `_msg.ResourceNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Content.RESOURCE_NOT_FOUND)` |
| `_msg.PageNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Content.PAGE_NOT_FOUND)` |
| `_msg.CategoryNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Content.CATEGORY_NOT_FOUND)` |
| `_msg.AssetNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Content.ASSET_NOT_FOUND)` |
| `_msg.AssetNotClean<T>()` | `_msg.BusinessRule<T>(MessageKeys.Content.ASSET_NOT_CLEAN)` |

#### Community domain

| Remove | Replace with |
|--------|-------------|
| `_msg.TopicNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Community.TOPIC_NOT_FOUND)` |
| `_msg.CannotFollowSelf<T>()` | See note below |

> **`CannotFollowSelf` expansion** — this shortcut wraps both `ValidationError` and `Field` internally. Expand inline:
> ```csharp
> _msg.ValidationError<T>(
>     MessageKeys.Community.CANNOT_FOLLOW_SELF,
>     new[] { _msg.Field("userId", MessageKeys.Community.CANNOT_FOLLOW_SELF) })
> ```

#### Country domain

| Remove | Replace with |
|--------|-------------|
| `_msg.CountryNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Country.COUNTRY_NOT_FOUND)` |
| `_msg.CountryProfileNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Country.COUNTRY_PROFILE_NOT_FOUND)` |
| `_msg.NoCountryAssigned<T>()` | `_msg.NotFound<T>(MessageKeys.Country.NO_COUNTRY_ASSIGNED)` |
| `_msg.CountryScopeForbidden<T>()` | `_msg.Forbidden<T>(MessageKeys.Country.COUNTRY_SCOPE_FORBIDDEN)` |
| `_msg.CountryContentRequestNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Content.COUNTRY_RESOURCE_REQUEST_NOT_FOUND)` |
| `_msg.CountryRequestProcessed<T>(data)` | `_msg.Ok(data, MessageKeys.Content.COUNTRY_REQUEST_PROCESSED)` |
| `_msg.CountryRequestProcessingFailed<T>()` | `_msg.BusinessRule<T>(MessageKeys.Content.COUNTRY_REQUEST_PROCESSING_FAILED)` |
| `_msg.KapsarcDataUnavailable<T>()` | `_msg.BusinessRule<T>(MessageKeys.Country.KAPSARC_DATA_UNAVAILABLE)` |
| `_msg.KapsarcSnapshotRefreshed<T>(data)` | `_msg.Ok(data, MessageKeys.Country.KAPSARC_SNAPSHOT_REFRESHED)` |

#### Platform Settings domain

| Remove | Replace with |
|--------|-------------|
| `_msg.HomepageSettingsNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.PlatformSettings.HOMEPAGE_SETTINGS_NOT_FOUND)` |
| `_msg.AboutSettingsNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.PlatformSettings.ABOUT_SETTINGS_NOT_FOUND)` |
| `_msg.PoliciesSettingsNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.PlatformSettings.POLICIES_SETTINGS_NOT_FOUND)` |
| `_msg.GlossaryEntryNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.PlatformSettings.GLOSSARY_ENTRY_NOT_FOUND)` |
| `_msg.KnowledgePartnerNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.PlatformSettings.KNOWLEDGE_PARTNER_NOT_FOUND)` |
| `_msg.PolicySectionNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.PlatformSettings.POLICY_SECTION_NOT_FOUND)` |
| `_msg.ContentUpdateFailed<T>()` | `_msg.BusinessRule<T>(MessageKeys.PlatformSettings.CONTENT_UPDATE_FAILED)` |

#### Media domain

| Remove | Replace with |
|--------|-------------|
| `_msg.MediaFileNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Media.MEDIA_FILE_NOT_FOUND)` |
| `_msg.InvalidFileType<T>()` | `_msg.BusinessRule<T>(MessageKeys.Media.INVALID_FILE_TYPE)` |
| `_msg.FileTooLarge<T>()` | `_msg.BusinessRule<T>(MessageKeys.Media.FILE_TOO_LARGE)` |
| `_msg.EmptyFile<T>()` | `_msg.BusinessRule<T>(MessageKeys.Media.EMPTY_FILE)` |

#### InteractiveMaps domain

| Remove | Replace with |
|--------|-------------|
| `_msg.MapNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.InteractiveMaps.MAP_NOT_FOUND)` |
| `_msg.MapCreated()` | `_msg.Ok(MessageKeys.InteractiveMaps.MAP_CREATED)` |
| `_msg.MapUpdated()` | `_msg.Ok(MessageKeys.InteractiveMaps.MAP_UPDATED)` |
| `_msg.MapDeleted()` | `_msg.Ok(MessageKeys.InteractiveMaps.MAP_DELETED)` |
| `_msg.NodeNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.InteractiveMaps.NODE_NOT_FOUND)` |
| `_msg.NodeCreated()` | `_msg.Ok(MessageKeys.InteractiveMaps.NODE_CREATED)` |
| `_msg.NodeUpdated()` | `_msg.Ok(MessageKeys.InteractiveMaps.NODE_UPDATED)` |
| `_msg.NodeDeleted()` | `_msg.Ok(MessageKeys.InteractiveMaps.NODE_DELETED)` |

#### Evaluation domain

| Remove | Replace with |
|--------|-------------|
| `_msg.EvaluationSubmitted()` | `_msg.Ok(MessageKeys.Evaluation.EVALUATION_SUBMITTED)` |
| `_msg.EvaluationNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Evaluation.EVALUATION_NOT_FOUND)` |

#### Notifications domain

| Remove | Replace with |
|--------|-------------|
| `_msg.NotificationTemplateNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Notifications.TEMPLATE_NOT_FOUND)` |
| `_msg.NotificationLogNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Notifications.NOTIFICATION_NOT_FOUND)` |
| `_msg.NotificationSettingsUpdated()` | `_msg.Ok(MessageKeys.Notifications.NOTIFICATION_SETTINGS_UPDATED)` |
| `_msg.NotificationMarkedRead()` | `_msg.Ok(MessageKeys.Notifications.NOTIFICATION_MARKED_READ)` |
| `_msg.NotificationsMarkedRead(count)` | `_msg.Ok(count, MessageKeys.Notifications.NOTIFICATIONS_MARKED_READ)` |
| `_msg.NotificationRetried<T>(data)` | `_msg.Ok(data, MessageKeys.Notifications.NOTIFICATION_RETRIED)` |
| `_msg.NotificationTemplateCreated<T>(data)` | `_msg.Ok(data, MessageKeys.Notifications.NOTIFICATION_TEMPLATE_CREATED)` |
| `_msg.NotificationTemplateUpdated<T>(data)` | `_msg.Ok(data, MessageKeys.Notifications.NOTIFICATION_TEMPLATE_UPDATED)` |
| `_msg.DeviceTokenRegistered()` | `_msg.Ok(MessageKeys.Notifications.DEVICE_TOKEN_REGISTERED)` |
| `_msg.DeviceTokenDeleted()` | `_msg.Ok(MessageKeys.Notifications.DEVICE_TOKEN_DELETED)` |
| `_msg.DeviceTokenNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Notifications.DEVICE_TOKEN_NOT_FOUND)` |

#### Lookups domain

| Remove | Replace with |
|--------|-------------|
| `_msg.CountryCodeNotFound<T>()` | `_msg.NotFound<T>(MessageKeys.Lookups.COUNTRY_CODE_NOT_FOUND)` |
| `_msg.LookupCreated<T>(data)` | `_msg.Ok(data, MessageKeys.Lookups.LOOKUP_CREATED)` |
| `_msg.LookupUpdated<T>(data)` | `_msg.Ok(data, MessageKeys.Lookups.LOOKUP_UPDATED)` |

---

## Phase 3 — Verify

```powershell
# Build should produce 0 errors / 0 warnings from our code
dotnet build src/CCE.Application/CCE.Application.csproj --no-incremental
dotnet build src/CCE.Api.External/CCE.Api.External.csproj --no-incremental
dotnet build src/CCE.Api.Internal/CCE.Api.Internal.csproj --no-incremental
```

If a handler is missing `using CCE.Application.Messages;`, the compiler will report `The name 'MessageKeys' does not exist` — add the using.

---

## Rule going forward

**`MessageFactory` has exactly 9 methods. No new shortcuts ever.** Any handler that returns a domain outcome writes `_msg.<Verb><T>(MessageKeys.<Domain>.<KEY>)` directly. New outcomes get a new `MessageKeys` constant, a new `SystemCodeMap` entry, and a new `Resources.yaml` string — nothing else.
