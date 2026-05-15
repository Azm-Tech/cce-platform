# US028 - Follow Post

## Epic
Knowledge Community

## Feature Code
F028

## Sprint
Sprint 09: Knowledge Community Posts

## Priority
Medium

## User Story
**As a** platform user, **I want to** follow a specific post, **so that** I can continuously get updates about it.

## Roles
| Role | Access |
|------|--------|
| Registered User | Can |

## Preconditions
- User must be logged in

## Acceptance Criteria
1. User navigates to a post
2. User clicks "Follow Post"
3. System saves data and sends notifications about updates → CON012
4. If cannot follow → ERR015
5. If follow error occurs → ERR015

## Post-conditions
- User can unfollow at any time

### Alternative Flows
- ALT001: If follow fails, system displays ERR015

### Business Rules
- BC001: Must send notifications for post updates

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR015 | Error | عذراً، لا يمكن متابعة المنشور حالياً. | Post follow failure |

### Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON012 | تم حفظ بياناتك بنجاح. س تتلقى إشعارات أو تحديثات حول المنشور. |