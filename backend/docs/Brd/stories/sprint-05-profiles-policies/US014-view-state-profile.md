# US014 - استعراض ملف تعريف الدولة

## Epic
Profiles & Policies

## Feature Code
F014

## Sprint
Sprint 05: Profiles & Policies

## Priority
High

## User Story
**As a** مستخدم للمنصة، **I want to** استعراض ملف التعريف الخاص بالدولة، **so that** أتمكن من الاطلاع على التفاصيل المتعلقة بالدولة.

## Roles
| Role | Access |
|------|--------|
| Visitor | Can |
| Registered User | Can |

## Preconditions
- State profile must be available

## Acceptance Criteria
1. User enters the platform and navigates to the homepage
2. User clicks "State Profile"
3. System shows a list of countries
4. User selects a country
5. System displays the state profile details: population, area, GDP per capita, CCE classification, CCE performance, PDF nationally determined contribution, Total CCE Index
6. System retrieves CCE data from KAPSARC integration (BC001)
7. If no profile exists for the selected country, system displays ALT001
8. If a load error occurs, system displays error ERR001

## Post-conditions
- User can navigate to other country profiles

## Alternative Flows
- ALT001: If state profile not found, system displays message suggesting different search

## Business Rules
- BC001: System must correctly retrieve and display state profile data including KAPSARC-linked data (CCE Classification, CCE Performance, CCE Total Index)

## Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR001 | Error | حدث خطأ أثناء تحميل الصفحة. | Page load error |

## KAPSARC Integration
- Requires KAPSARC API integration for CCE Classification, CCE Performance, and CCE Total Index data
- See appendix for KAPSARC service specification
