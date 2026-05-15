# US056 - View Post (Admin)

## Epic
Admin Country Requests & Community

## Feature Code
F055

## Sprint
Sprint 14: Admin Country Requests & Community

## Priority
Medium

## User Story
**As an** admin, **I want to** view a post, **so that** I can see the full details of the submitted post.

## Roles
| Role | Access |
|------|--------|
| Super Admin | Can |
| Admin | Can |
| Content Manager | Can |

## Preconditions
- Posts must be available

## Acceptance Criteria
1. Admin navigates to Knowledge Community and selects a post
2. System displays post with all details
3. System displays full post based on available data (BC001)
4. If no posts exist, alternative flow ALT001 or notification NTF001 is triggered
5. On load error, error message ERR001 is displayed

## Post-conditions
- Admin can delete posts

### Alternative Flows
- ALT001: If no posts available, system displays NTF001

### Business Rules
- BC001: Display full post based on available data

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR001 | Error | حدث خطأ أثناء تحميل الصفحة. | Page load error |

### Informational Messages
| Code | Type | Message (AR) |
|------|------|-------------|
| NTF001 | Notification | عذراً، لا توجد منشورات حالياً. |