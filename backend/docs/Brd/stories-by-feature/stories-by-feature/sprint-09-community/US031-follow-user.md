# US031 - Follow User

## Epic
Knowledge Community

## Feature Code
F031

## Sprint
Sprint 10: Knowledge Community Users

## Priority
Medium

## User Story
**As a** platform user, **I want to** follow another user, **so that** I can continuously view their activities and new posts.

## Roles
| Role | Access |
|------|--------|
| Registered User | Can |

## Preconditions
- User must be logged in

## Acceptance Criteria
1. User navigates to a user profile
2. User clicks "Follow"
3. System saves follow data and updates status with confirmation
4. If cannot follow → ERR018
5. If follow error occurs → ERR018

## Post-conditions
- User can unfollow at any time by clicking "Unfollow"

### Alternative Flows
- ALT001: If follow fails, system displays ERR018

### Business Rules
- BC001: Follow status must be saved so user can easily follow the other user's posts

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR018 | Error | عذراً، لا يمكن متابعة المستخدم حالياً. | User follow failure |