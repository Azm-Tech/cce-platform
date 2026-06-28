# US025 - Share Post

## Epic
Knowledge Community

## Feature Code
F025

## Sprint
Sprint 09: Knowledge Community Posts

## Priority
Medium

## User Story
**As a** platform user, **I want to** share a post, **so that** I can distribute it with others via the platform or via social media.

## Roles
| Role | Access |
|------|--------|
| Visitor | Can |
| Registered User | Can |

## Preconditions
- Post must be available

## Acceptance Criteria
1. User navigates to a post
2. User clicks "Share"
3. System shows sharing options (email, link)
4. User selects sharing method
5. System shares the post → CON003
6. If cannot share → ERR004
7. If share failure occurs → ERR004

## Post-conditions
- User can interact with the post

### Alternative Flows
- ALT001: If no post available for sharing, system displays ERR004 and redirects to community

### Business Rules
- BC001: Display full post details

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR004 | Error | حدث خطأ أثناء محاولة المشاركة. يرجى المحاولة مرة أخرى لاحقاً. | Post share failure |

### Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON003 | تمت المشاركة بنجاح! |