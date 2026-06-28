# Sprint 01 Auth & User Services - Implementation Plan

## Scope

Implement the Sprint 01 auth stories in `docs/Brd/stories/sprint-01-auth-user-services`:

| Story | Capability | API outcome |
|---|---|---|
| US033 | Create account | Register a local user account with profile fields and password |
| US034 | Login | Validate credentials and issue access + refresh tokens |
| US035 | Password recovery | Request password reset, deliver reset link/token, reset password |
| US036 | Logout | Revoke the active refresh token/session |

This plan adds a first-party email/password auth surface for both APIs while keeping the existing Entra ID JWT validation and dev auth shim intact. `CCE.Api.External` and `CCE.Api.Internal` must use different local JWT signing keys, issuers, and audiences so tokens cannot be replayed across API boundaries.

---

## Current State

- `CCE.Api.External` already has `/api/users/register`, but it creates Entra users through `EntraIdRegistrationService` in production and directly creates a dev user in `Auth:DevMode`.
- JWT bearer auth is configured in `CCE.Api.Common/Auth/CceJwtAuthRegistration.cs` using Microsoft.Identity.Web for Entra tokens.
- `CceDbContext` already extends `IdentityDbContext<User, Role, Guid>`, so Identity tables exist.
- There is no registered `UserManager<User>`, `RoleManager<Role>`, or `SignInManager<User>` setup yet.
- There is no local access-token issuer, refresh-token store, refresh endpoint, or password reset endpoint.
- Existing API response direction is `Result<T>` + `ToHttpResult()`, so new application handlers should return `Result<T>` instead of raw `Results.BadRequest(...)` where practical.

---

## Target API Contract

Base group: `/api/auth`, tagged `Auth`.

### Register

`POST /api/auth/register`

Request:

```json
{
  "firstName": "Sara",
  "lastName": "Ahmed",
  "emailAddress": "sara@example.com",
  "jobTitle": "Planner",
  "organizationName": "CCE",
  "phoneNumber": "+966500000000",
  "password": "StrongPass123",
  "confirmPassword": "StrongPass123"
}
```

Response:

- `201 Created`
- `Result<AuthUserDto>`
- Does not auto-login. This follows US033: account creation succeeds, then the user logs in separately.
- Creates user in role `cce-user`.

### Login

`POST /api/auth/login`

Request:

```json
{
  "emailAddress": "sara@example.com",
  "password": "StrongPass123"
}
```

Response:

```json
{
  "isSuccess": true,
  "data": {
    "accessToken": "<jwt>",
    "accessTokenExpiresAtUtc": "2026-05-14T19:10:00Z",
    "refreshToken": "<opaque-token>",
    "refreshTokenExpiresAtUtc": "2026-06-13T19:00:00Z",
    "tokenType": "Bearer",
    "user": {
      "id": "00000000-0000-0000-0000-000000000000",
      "emailAddress": "sara@example.com",
      "firstName": "Sara",
      "lastName": "Ahmed",
      "roles": ["cce-user"]
    }
  },
  "error": null
}
```

### Refresh Token

`POST /api/auth/refresh`

Request:

```json
{
  "refreshToken": "<opaque-token>"
}
```

Response:

- Issues a new access token and a new refresh token.
- Revokes the old refresh token.
- Reuse of a revoked token revokes the full token family for that user/device.

### Forgot Password

`POST /api/auth/forgot-password`

Request:

```json
{
  "emailAddress": "sara@example.com"
}
```

Response:

- `200 OK`
- Always returns success, including when the email is unknown, to avoid account enumeration.
- Internally log the unknown-email case at low severity without exposing it to the caller.

### Reset Password

`POST /api/auth/reset-password`

Request:

```json
{
  "emailAddress": "sara@example.com",
  "token": "<url-safe-reset-token>",
  "newPassword": "NewStrongPass123",
  "confirmPassword": "NewStrongPass123"
}
```

Response:

- `200 OK`
- Existing refresh tokens for the user are revoked after password reset.

### Logout

`POST /api/auth/logout`

Request:

```json
{
  "refreshToken": "<opaque-token>"
}
```

Response:

- `200 OK` with `CON015` equivalent, or `204 NoContent` if the API standard prefers no body.
- Revoke the submitted refresh token.
- Optional later endpoint: `POST /api/auth/logout-all` for revoking every active user session.

---

## Data Model Changes

### Extend `User`

File: `src/CCE.Domain/Identity/User.cs`

Add Sprint 01 profile fields:

- `FirstName`
- `LastName`
- `JobTitle`
- `OrganizationName`

Use private setters and mutation methods, following the existing entity style.

Keep `Email`, `UserName`, `PhoneNumber`, `PasswordHash`, `EmailConfirmed`, lockout fields, security stamp, and concurrency stamp from `IdentityUser<Guid>`.

### Add `RefreshToken`

New file: `src/CCE.Domain/Identity/RefreshToken.cs`

Fields:

- `Id: Guid`
- `UserId: Guid`
- `TokenHash: string`
- `TokenFamilyId: Guid`
- `CreatedAtUtc: DateTimeOffset`
- `ExpiresAtUtc: DateTimeOffset`
- `RevokedAtUtc: DateTimeOffset?`
- `ReplacedByTokenHash: string?`
- `CreatedByIp: string?`
- `RevokedByIp: string?`
- `UserAgent: string?`

Rules:

- Store only SHA-256 hashes of refresh tokens.
- Refresh tokens are opaque random values, not JWTs.
- Active token means `RevokedAtUtc is null && ExpiresAtUtc > now`.
- Refresh is rotation-only: every refresh consumes the old token and creates a new one.
- Reuse detection: if a revoked token is used again, revoke all tokens in the same `TokenFamilyId`.

### EF Mapping

Add `DbSet<RefreshToken>` in `CceDbContext`.

Add configuration:

`src/CCE.Infrastructure/Persistence/Configurations/Identity/RefreshTokenConfiguration.cs`

Indexes:

- Unique index on `TokenHash`
- Index on `UserId`
- Index on `TokenFamilyId`
- Optional filtered index for active tokens if SQL Server filter is worth it

Migration:

```bash
dotnet ef migrations add AddLocalAuthRefreshTokens --project src/CCE.Infrastructure --startup-project src/CCE.Infrastructure
```

---

## Configuration

Add options class:

`src/CCE.Api.Common/Auth/LocalJwtOptions.cs` or `src/CCE.Infrastructure/Identity/LocalAuthOptions.cs`

Config section:

```json
{
  "LocalAuth": {
    "External": {
      "Issuer": "cce-api-external",
      "Audience": "cce-public",
      "SigningKey": "dev-only-external-long-random-secret-replace-in-user-secrets"
    },
    "Internal": {
      "Issuer": "cce-api-internal",
      "Audience": "cce-admin",
      "SigningKey": "dev-only-internal-long-random-secret-replace-in-user-secrets"
    },
    "AccessTokenMinutes": 10,
    "RefreshTokenDays": 30,
    "PasswordResetTokenHours": 2,
    "RequireConfirmedEmail": false
  }
}
```

Rules:

- Do not commit production signing secrets.
- In development use user-secrets or `appsettings.Development.json`.
- Validate both signing key lengths on startup.
- External and Internal keys must be different.
- External and Internal issuers/audiences must be different.
- Keep short access tokens and longer refresh tokens.
- Refresh tokens are returned in the response body for Sprint 01.

---

## Service Design

### Identity Registration

In `Infrastructure.DependencyInjection`, register Identity Core:

```csharp
services
    .AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<Role>()
    .AddEntityFrameworkStores<CceDbContext>()
    .AddDefaultTokenProviders();
```

Password validation must follow US033/US034 exactly: 12-20 characters, uppercase, lowercase, and numbers. Symbols are allowed by Identity unless another validator rejects them, but they are not required.

### Token Issuer

New application abstraction:

`src/CCE.Application/Identity/Auth/ITokenService.cs`

Responsibilities:

- Build JWT access token with `sub`, `email`, `preferred_username`, `roles`, `jti`.
- Include permission claims only if current authorization expects them in token. Otherwise keep `RoleToPermissionClaimsTransformer` responsible for permission expansion.
- Generate cryptographically random refresh token.
- Hash refresh token before persistence.

Infrastructure implementation:

`src/CCE.Infrastructure/Identity/LocalTokenService.cs`

### Refresh Token Repository

New application abstraction:

`src/CCE.Application/Identity/Auth/IRefreshTokenRepository.cs`

Methods:

- `AddAsync(RefreshToken token, CancellationToken ct)`
- `FindByHashAsync(string tokenHash, CancellationToken ct)`
- `RevokeAsync(...)`
- `RevokeFamilyAsync(Guid tokenFamilyId, ...)`
- `RevokeAllForUserAsync(Guid userId, ...)`

Infrastructure implementation:

`src/CCE.Infrastructure/Identity/RefreshTokenRepository.cs`

---

## Application Layer

Create folder:

`src/CCE.Application/Identity/Auth`

Commands and DTOs:

- `RegisterUserCommand`
- `LoginCommand`
- `RefreshTokenCommand`
- `ForgotPasswordCommand`
- `ResetPasswordCommand`
- `LogoutCommand`
- `AuthTokenDto`
- `AuthUserDto`
- `AuthMessageDto`

Validators:

- Register: all fields required, names max 50 letters-only, email max 100 valid email, phone max 15, password 12-20 with uppercase/lowercase/number, confirm matches.
- Login: email/password required.
- Refresh: token required.
- Forgot password: email required and valid.
- Reset password: email/token/new password/confirm required, password 12-20 with uppercase/lowercase/number, confirm matches.
- Logout: refresh token required.

Handlers:

- Use `UserManager<User>` for create, password check, reset token generation/validation, and security stamp updates.
- Use `RoleManager<Role>` or direct role assignment through `UserManager.AddToRoleAsync`.
- Return `Result<T>` with localized `Error` objects.
- Never return different login errors for "email not found" versus "password wrong"; both map to `INVALID_CREDENTIALS`.
- Revoke refresh tokens after reset password and after security-sensitive account changes.

---

## API Layer

New endpoint files:

`src/CCE.Api.External/Endpoints/AuthEndpoints.cs`

`src/CCE.Api.Internal/Endpoints/AuthEndpoints.cs`

Register in `src/CCE.Api.External/Program.cs` and `src/CCE.Api.Internal/Program.cs`:

```csharp
app.MapAuthEndpoints();
```

Endpoint group:

```csharp
var auth = app.MapGroup("/api/auth").WithTags("Auth");
```

Endpoints:

- `POST /register` anonymous
- `POST /login` anonymous
- `POST /refresh` anonymous
- `POST /forgot-password` anonymous
- `POST /reset-password` anonymous
- `POST /logout` anonymous or authorized plus body refresh token

External and Internal share the same endpoint contract, but issue tokens with their own issuer, audience, and signing key. A token minted by External must fail validation on Internal, and the reverse must also fail.

Keep the existing `/dev/*` endpoints for `Auth:DevMode`.

Decision: deprecate or keep `/api/users/register`.

- Recommended: keep it temporarily and forward it to the new `RegisterUserCommand` so existing frontend calls do not break.
- Add a comment marking it as compatibility surface.

---

## JWT Validation Strategy

Current `AddCceJwtAuth` validates Entra JWTs through Microsoft.Identity.Web.

Use local JWT validation for both APIs, with different key material and token metadata per API.

External:

- Issuer: `LocalAuth:External:Issuer`
- Audience: `LocalAuth:External:Audience`
- Signing key: `LocalAuth:External:SigningKey`

Internal:

- Issuer: `LocalAuth:Internal:Issuer`
- Audience: `LocalAuth:Internal:Audience`
- Signing key: `LocalAuth:Internal:SigningKey`

Implementation approach:

- Refactor `AddCceJwtAuth` to accept an API audience/profile, e.g. `AddCceJwtAuth(configuration, LocalAuthApi.External)` and `AddCceJwtAuth(configuration, LocalAuthApi.Internal)`.
- Validate issuer, audience, lifetime, and signing key.
- Keep `MapInboundClaims = false`, `NameClaimType = "preferred_username"`, and `RoleClaimType = "roles"`.
- Keep the dev auth shim when `Auth:DevMode=true`.
- If Entra tokens still need to coexist later, add a policy scheme after Sprint 01. Sprint 01 local auth uses the local JWT scheme as the primary bearer scheme.

Validation tests must prove External tokens are rejected by Internal and Internal tokens are rejected by External.

---

## Password Recovery Email

Reuse `IEmailSender`.

New service:

`src/CCE.Application/Identity/Auth/IPasswordResetEmailService.cs`

or infrastructure service if email composition is infrastructure-owned:

`src/CCE.Infrastructure/Identity/PasswordResetEmailService.cs`

Flow:

1. Handler receives `ForgotPasswordCommand`.
2. Finds user by email.
3. Generates token via `UserManager.GeneratePasswordResetTokenAsync(user)`.
4. Base64Url encodes the token.
5. Builds reset URL from config, e.g. `Frontend:PasswordResetUrl`.
6. Sends email.

Security:

- Do not log reset tokens.
- Token lifetime from `LocalAuth:PasswordResetTokenHours`.
- After successful reset, call `UpdateSecurityStampAsync(user)` and revoke refresh tokens.

---

## Error Codes

Map BRD codes to application errors:

| BRD code | Application code | HTTP |
|---|---|---|
| ERR013 | `GENERAL_VALIDATION_ERROR` / field details | 400 |
| ERR019 | `IDENTITY_REGISTRATION_FAILED` | 500 or 422 |
| ERR020 | `IDENTITY_INVALID_CREDENTIALS` | 401 |
| ERR021 | `IDENTITY_LOGIN_FAILED` | 500 |
| ERR022 | `IDENTITY_USER_NOT_FOUND` | 404 or generic 200 for anti-enumeration |
| ERR023 | `IDENTITY_PASSWORD_RECOVERY_FAILED` | 500 |
| ERR024 | `IDENTITY_LOGOUT_FAILED` | 500 |
| CON017 | `IDENTITY_USER_CREATED` | 201 |
| CON014 | `IDENTITY_PASSWORD_RESET` | 200 |
| CON015 | `IDENTITY_LOGOUT_SUCCESS` | 200 |

Add missing constants to:

`src/CCE.Application/Errors/ApplicationErrors.cs`

Add localization entries when the localization plan is implemented.

---

## Testing Plan

Application tests:

- Register succeeds and creates `cce-user`.
- Register rejects duplicate email.
- Register validates required fields and password confirmation.
- Login returns invalid credentials for unknown email and wrong password.
- Login returns access token + refresh token for valid credentials.
- Refresh rotates token and revokes old token.
- Reuse of old refresh token revokes token family.
- Forgot password sends email for existing user.
- Reset password updates password and revokes existing refresh tokens.
- Logout revokes refresh token.

Infrastructure tests:

- `RefreshTokenConfiguration` creates expected indexes.
- `LocalTokenService` creates valid JWT claims and expiry.
- `RefreshTokenRepository` stores hashes only.

API integration tests:

- `POST /api/auth/register` -> `201`.
- `POST /api/auth/login` -> `200` with usable bearer token.
- Call protected `/api/me` with local access token -> `200`.
- External access token is rejected by an Internal protected endpoint.
- Internal access token is rejected by an External protected endpoint.
- `POST /api/auth/refresh` -> old refresh token cannot be reused.
- `POST /api/auth/logout` -> refresh token cannot be used.
- Password reset flow using fake email sender.

Run:

```bash
dotnet test tests/CCE.Application.Tests
dotnet test tests/CCE.Infrastructure.Tests
dotnet test tests/CCE.Api.IntegrationTests
dotnet build CCE.sln
```

---

## Implementation Phases

### Phase 1 - Foundation

- Add `LocalAuthOptions`.
- Register Identity Core with `UserManager`, roles, EF stores, token providers.
- Extend `User` with Sprint 01 profile fields.
- Add `RefreshToken` entity, EF configuration, repository, migration.
- Add error constants.

### Phase 2 - Token Services

- Implement `ITokenService`.
- Implement local JWT issuing.
- Implement refresh-token generation, hashing, persistence, rotation, family revocation.
- Update auth registration for local JWT validation on External and Internal APIs, using separate config profiles and keys.

### Phase 3 - Commands

- Implement register/login/refresh/logout command DTOs, validators, handlers.
- Keep handlers returning `Result<T>`.
- Assign default `cce-user` role at registration.

### Phase 4 - Password Recovery

- Implement forgot-password and reset-password commands.
- Wire `IEmailSender`.
- Add reset URL configuration.
- Revoke refresh tokens after reset.

### Phase 5 - Endpoints

- Add `AuthEndpoints`.
- Register in External and Internal `Program.cs`.
- Move or forward `/api/users/register` compatibility path.
- Ensure Swagger shows request/response contracts.

### Phase 6 - Tests & Hardening

- Add unit, infrastructure, and integration tests.
- Verify lockout behavior.
- Verify no refresh token plaintext is stored.
- Verify token reuse detection.
- Run full build and tests with warnings as errors.

---

## Accepted Decisions

1. Registration does not auto-login. The user logs in separately after account creation.
2. Forgot-password returns success even when the email is unknown.
3. Local JWT auth applies to both External and Internal APIs, with different signing keys, issuers, and audiences.
4. Refresh tokens are returned in the response body for now.
5. Password validation follows the stories: 12-20 characters with uppercase, lowercase, and numbers. Symbols are not required.

---

## Acceptance Checklist

- [ ] User can create an account with all US033 fields.
- [ ] Duplicate email is rejected.
- [ ] User can login with email/password.
- [ ] Login returns short-lived JWT access token and long-lived refresh token.
- [ ] Protected endpoints accept the local access token.
- [ ] External and Internal tokens are not interchangeable.
- [ ] Refresh rotates refresh tokens.
- [ ] Reused revoked refresh token is detected and invalidates the token family.
- [ ] Logout revokes the submitted refresh token.
- [ ] Forgot password sends reset email/link.
- [ ] Reset password allows login with the new password.
- [ ] Reset password revokes existing refresh tokens.
- [ ] `dotnet build CCE.sln` passes with warnings as errors.
- [ ] Relevant tests pass.
