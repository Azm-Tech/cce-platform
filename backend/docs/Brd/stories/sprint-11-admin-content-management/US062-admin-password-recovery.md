# US062 - Admin Password Recovery

## Epic
Admin Content Management

## Feature Code
F062

## Sprint
Sprint 11: Admin Content Management

## Priority
High

## User Story
**As an** admin, **I want to** recover my password, **so that** I can access my account if I forget my password.

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
1. Admin enters platform > "Login" > clicks "Forgot Password?"
2. Admin enters email address
3. System sends password reset link (BC001: email must be registered for password recovery)
4. Admin clicks reset link and enters new password
5. System updates password and displays confirmation CON014
6. Admin is redirected to login page
7. On email not found, error message ERR022 is displayed
8. On system error, error message ERR023 is displayed

## Post-conditions
- Admin can login with new password

### Alternative Flows
- ALT001: If email not found, system displays ERR022

### Business Rules
- BC001: Email must be registered in the system for password recovery

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR022 | Error | عذراً، لم يتم العثور على الحساب المرتبط بالبريد الإلكتروني. | Email not found |
| ERR023 | Error | عذراً، حدثت مشكلة أثناء استعادة كلمة المرور. | Password recovery system error |

### Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON014 | تمت استعادة كلمة المرور بنجاح! |