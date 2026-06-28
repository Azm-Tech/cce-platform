# US026 - Create Post

## Epic
Knowledge Community

## Feature Code
F026

## Sprint
Sprint 09: Knowledge Community Posts

## Priority
High

## User Story
**As a** platform user, **I want to** share a post, **so that** I can publish it with others via the platform.

## Roles
| Role | Access |
|------|--------|
| Registered User | Can |

## Preconditions
- User must be logged in

## Acceptance Criteria
1. User navigates to Knowledge Community
2. User clicks "Create Post"
3. System displays post creation form
4. User fills Title (150 chars, required)
5. User fills Content (5000 chars, required)
6. User selects Post Type (dropdown: info/question/poll, required)
7. User clicks "Publish"
8. System confirms publication → CON011
9. If missing required fields → ERR013
10. If publish error occurs → ERR014

## Post-conditions
- User can review and interact with their post
- User can share the post

### Alternative Flows
- ALT001: If required fields not filled, system displays ERR013

### Business Rules
- BC001: User must enter required data (title and content) before publishing

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR013 | Error | عذراً، الحقول الإجبارية غير مكتملة. | Required fields empty |
| ERR014 | Error | عذراً، حدثت مشكلة أثناء نشر المنشور. | Post publish failure |

### Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON011 | تم إنشاء المنشور بنجاح! |

### Form Fields & Validation Rules
| Field | Type | Required | Max Length | Validation |
|-------|------|----------|------------|------------|
| Post Title | Free Text | Yes | 150 | - |
| Post Content | Free Text | Yes | 5000 | - |
| Post Type | Dropdown | Yes | - | Options: Info, Question, Poll |