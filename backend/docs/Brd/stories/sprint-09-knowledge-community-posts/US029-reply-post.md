# US029 - Reply to Post

## Epic
Knowledge Community

## Feature Code
F029

## Sprint
Sprint 09: Knowledge Community Posts

## Priority
High

## User Story
**As a** platform user, **I want to** reply to a post, **so that** I can add my comment or answer to the post.

## Roles
| Role | Access |
|------|--------|
| Registered User | Can |

## Preconditions
- User must be logged in

## Acceptance Criteria
1. User navigates to a post
2. User clicks "Reply" or comment field
3. User types reply
4. User clicks "Send"
5. System saves reply and displays it under the post → CON013
6. If empty reply → ERR016
7. If reply error occurs → ERR017

## Post-conditions
- User can review their replies at any time

### Alternative Flows
- ALT001: If user submits empty reply, system displays ERR016

### Business Rules
- BC001: Replies must be displayed immediately after submission

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR016 | Error | عذراً، لا يمكن إرسال رد فارغ. | Empty reply |
| ERR017 | Error | عذراً، حدثت مشكلة أثناء إرسال الرد. | Reply submission failure |

### Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON013 | تم إرسال الرد بنجاح! |