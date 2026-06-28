# US030 - View User Profile in Community

## Epic
Knowledge Community

## Feature Code
F030

## Sprint
Sprint 10: Knowledge Community Users

## Priority
Medium

## User Story
**As a** platform user, **I want to** view another user's profile, **so that** I can see their information and follow their activities on the platform.

## Roles
| Role | Access |
|------|--------|
| Registered User | Can |

## Preconditions
- User must be logged in

## Acceptance Criteria
1. User navigates to Knowledge Community
2. User selects a user profile
3. System displays: First Name, Last Name, Job Title, Organization, Join Date, Post Count, Reply Count
4. If user is an expert, system displays CV description and expert badge
5. If no internet → ERR001
6. If load error occurs → ERR001

## Post-conditions
- User can follow the profile

### Alternative Flows
- ALT001: If no internet, system displays ERR001 and redirects after retry

### Business Rules
- BC001: User profile must appear in a clear view template with all available information

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR001 | Error | حدث خطأ أثناء تحميل الصفحة. | Page load error |