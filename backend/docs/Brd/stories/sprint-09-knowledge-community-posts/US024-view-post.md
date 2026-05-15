# US024 - View Post

## Epic
Knowledge Community

## Feature Code
F024

## Sprint
Sprint 09: Knowledge Community Posts

## Priority
High

## User Story
**As a** platform user, **I want to** view a post, **so that** I can see the full details of the submitted post.

## Roles
| Role | Access |
|------|--------|
| Visitor | Can |
| Registered User | Can |

## Preconditions
- Posts must be available

## Acceptance Criteria
1. User navigates to Knowledge Community
2. User selects a post
3. System displays post with all its data (title, date, topic, content, attachments)
4. If no posts available → ALT001/NTF001
5. If load error occurs → ERR001

## Post-conditions
- User can interact with the post (like, comment)

### Alternative Flows
- ALT001: If no posts available, system displays NTF001 message

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