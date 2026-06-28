# US061 - Admin Login

## Epic
Admin Content Management

## Feature Code
F061

## Sprint
Sprint 11: Admin Content Management

## Priority
High

## User Story
**As an** admin, **I want to** log in to the platform using my credentials, **so that** I can access all available services.

## Roles
| Role | Access |
|------|--------|
| Super Admin | Can |
| Admin | Can |
| Content Manager | Can |
| State Representative | Can |

## Preconditions
- User must be registered as admin

## Acceptance Criteria
1. Admin enters platform and clicks "Login"
2. System displays login form
3. Admin enters credentials and clicks "Login"
4. System validates email and password before allowing login (BC001)
5. On success, admin is redirected to homepage
6. On invalid credentials, error message ERR020 is displayed
7. On system error, error message ERR021 is displayed

## Post-conditions
- Admin can access administrative services

### Alternative Flows
- ALT001: If admin enters incorrect data, system displays ERR020 and requests retry

### Business Rules
- BC001: Validate email and password before allowing login

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR020 | Error | عذراً، البيانات المدخلة غير صحيحة. | Invalid credentials |
| ERR021 | Error | عذراً، حدثت مشكلة أثناء تسجيل الدخول. | Login system error |