# US042 - Delete User

## Epic
Admin User Management

## Feature Code
F042

## Sprint
Sprint 12: Admin User Management

## Priority
High

## User Story
**As a** Super Admin, **I want to** delete a user from the platform, **so that** I can better manage users and organize access to services.

## Roles
| Role | Access |
|------|--------|
| Super Admin | Can only |

## Preconditions
- User must be Super Admin

## Acceptance Criteria
1. Super Admin navigates to user details
2. Admin clicks "Delete User"
3. System displays confirmation dialog ("Are you sure?")
4. System must display confirmation before deletion to prevent accidental deletion (BC001)
5. If admin clicks "Yes", system deletes user and displays confirmation CON018
6. If admin clicks "Cancel", alternative flow ALT001 is triggered (no deletion)
7. On deletion error, error message ERR026 is displayed

## Post-conditions
- Deleted user data cannot be restored unless backup exists

### Alternative Flows
- ALT001: If admin clicks "Cancel", system closes confirmation and returns to user list without deletion

### Business Rules
- BC001: Must display confirmation before deletion to prevent accidental deletion

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR001 | Error | حدث خطأ أثناء تحميل الصفحة. | Page load error |
| ERR026 | Error | عذراً، حدثت مشكلة أثناء حذف المستخدم. | User deletion failure |

### Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON018 | تم حذف المستخدم بنجاح! |