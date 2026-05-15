# US015 - استعراض الملف الشخصي

## Epic
Profiles & Policies

## Feature Code
F015

## Sprint
Sprint 05: Profiles & Policies

## Priority
High

## User Story
**As a** مستخدم للمنصة، **I want to** استعراض الملف الشخصي الخاص بي، **so that** أتمكن من الاطلاع على تفاصيل بياناتي.

## Roles
| Role | Access |
|------|--------|
| Registered User | Can |

## Preconditions
- User must have a profile

## Acceptance Criteria
1. User enters the platform and navigates to the homepage
2. User clicks "Profile"
3. System displays profile information: Country, First Name, Last Name, Email, Job Title, Organization
4. System displays following/followers lists
5. Personal data must be correctly retrieved from the database (BC001)
6. If there is no internet connection, system displays error ERR001
7. If a load error occurs, system displays error ERR001

## Post-conditions
- User can choose to edit profile

## Alternative Flows
- ALT001: If no internet, system displays ERR001 and redirects after retry

## Business Rules
- BC001: Personal data must be correctly retrieved from the database

## Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR001 | Error | حدث خطأ أثناء تحميل الصفحة. | Page load error |
