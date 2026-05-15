# US035 - استعادة كلمة المرور

## Epic
Auth & User Services

## Feature Code
F035

## Sprint
Sprint 01: Auth & User Services

## Priority
High

## User Story
**As a** مستخدم مسجل، **I want to** استعادة كلمة المرور الخاصة بي، **so that** أتمكن من الدخول إلى حسابي إذا نسيت كلمة المرور.

## Roles
| Role | Access |
|------|--------|
| User (Registered) | Can |

## Preconditions
- User must be registered with valid account

## Acceptance Criteria
1. User navigates to the platform homepage
2. User clicks "Login"
3. User clicks "Forgot Password?"
4. User enters their email address
5. System validates that the email is registered (BC001)
6. If email is not found, system displays error ERR022
7. If a system error occurs, system displays error ERR023
8. System sends a password reset link via email
9. User clicks the reset link
10. User enters new password and confirms the password
11. System updates the password and displays confirmation CON014

## Post-conditions
- User can login with new password

## Alternative Flows
- ALT001: If email not found in system, system displays ERR022

## Business Rules
- BC001: Email must be registered in the system for password recovery

## Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR001 | Error | حدث خطأ أثناء تحميل الصفحة. | Page load error |
| ERR022 | Error | عذراً، لم يتم العثور على الحساب المرتبط بالبريد الإلكتروني. | Email not found |
| ERR023 | Error | عذراً، حدثت مشكلة أثناء استعادة كلمة المرور. | Password recovery system error |

## Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON014 | تمت استعادة كلمة المرور بنجاح! |

## Form Fields & Validation Rules
| Field | Type | Required | Max Length | Validation |
|-------|------|----------|------------|------------|
| Email Address (EmailAddress) | Free Text | Yes | 100 | Must be a valid email |
