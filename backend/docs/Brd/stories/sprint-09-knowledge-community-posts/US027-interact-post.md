# US027 - Interact with Post

## Epic
Knowledge Community

## Feature Code
F027

## Sprint
Sprint 09: Knowledge Community Posts

## Priority
Medium

## User Story
**As a** platform user, **I want to** interact with a post through upvoting or downvoting, **so that** I can directly evaluate the post.

## Roles
| Role | Access |
|------|--------|
| Registered User | Can |

## Preconditions
- User must be logged in
- Post must be available

## Acceptance Criteria
1. User navigates to a post
2. User clicks "Rate Up" or "Rate Down"
3. System updates post to show new interaction
4. Only upvotes are displayed publicly
5. If interaction failure occurs, system shows error message asking to retry

## Post-conditions
- User can review their interaction at any time

### Alternative Flows
- ALT001: If interaction fails, system displays error message and requests retry

### Business Rules
- BC001: Display new interaction (up/down) immediately after click. Upvotes shown publicly with total count. Downvotes affect ranking only, not displayed publicly.

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR001 | Error | حدث خطأ أثناء تحميل الصفحة. | Post interaction failure |