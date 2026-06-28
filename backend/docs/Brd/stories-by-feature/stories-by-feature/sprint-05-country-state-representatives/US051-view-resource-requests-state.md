# US051 - View Resource Requests (State)

## Epic
State Representative

## Feature Code
F051

## Sprint
Sprint 15: State Representative

## Priority
Medium

## User Story
**As a** State Representative, **I want to** view resource/news/events requests submitted by my country, **so that** I can track their status and take appropriate actions.

## Roles
| Role | Access |
|------|--------|
| State Representative | Can |

## Preconditions
- User must be registered as State Rep
- Requests must have been submitted by their state

## Acceptance Criteria
1. State Rep enters platform > "Requests"
2. System displays list of state's resource requests
3. State Rep selects a request
4. System displays request details (resource form or news/event form, view-only)
5. System displays correct request details (BC001)
6. If no requests exist, alternative flow ALT001 or info message INF005 is triggered
7. On load error, error message ERR001 is displayed

## Post-conditions
- State Rep can track request status

### Alternative Flows
- ALT001: If no requests available, system displays INF005

### Business Rules
- BC001: Display correct request details

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR001 | Error | حدث خطأ أثناء تحميل الصفحة. | Page load error |

### Informational Messages
| Code | Type | Message (AR) |
|------|------|-------------|
| INF005 | Informational | عذراً، لا توجد طلبات متاحة حالياً. |