# US036 - تسجيل الخروج

## Epic
Auth & User Services

## Feature Code
F036

## Sprint
Sprint 01: Auth & User Services

## Priority
High

## User Story
**As a** مستخدم مسجل، **I want to** تسجيل الخروج من المنصة، **so that** أتمكن من إنهاء جلستي بشكل آمن.

## Roles
| Role | Access |
|------|--------|
| User (Registered) | Can |

## Preconditions
- User must be logged in

## Acceptance Criteria
1. User clicks the profile icon
2. User clicks "Logout"
3. System properly terminates the session (BC001)
4. System displays confirmation CON015
5. If a logout error occurs, system displays error ERR024
6. System redirects user to the homepage/login page

## Post-conditions
- User redirected to login page or homepage

## Alternative Flows
- ALT001: If logout error occurs, system displays ERR024 and allows retry

## Business Rules
- BC001: System must properly terminate session on logout

## Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR001 | Error | حدث خطأ أثناء تحميل الصفحة. | Page load error |
| ERR024 | Error | حدث خطأ أثناء محاولة تسجيل الخروج. | Logout failure |

## Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON015 | تم تسجيل الخروج بنجاح. |
