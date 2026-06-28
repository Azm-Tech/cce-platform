# US005 - مشاركة المصادر

## Epic
Core Content Viewing

## Feature Code
F005

## Sprint
Sprint 02: Core Content Viewing

## Priority
Medium

## User Story
**As a** مستخدم للمنصة، **I want to** مشاركة المصدر مع الآخرين عبر المنصة، **so that** يتمكنوا من الاطلاع عليه واستخدامه.

## Roles
| Role | Access |
|------|--------|
| Visitor | Can |
| Registered User | Can |

## Preconditions
- Resource must be available for sharing

## Acceptance Criteria
1. User navigates to resource details
2. User clicks "Share Resource"
3. System displays sharing options (email, link)
4. User selects a sharing method
5. System shares the resource and displays confirmation CON002
6. System displays full resource details (BC001)
7. If no resource is available, system displays error ERR003
8. If sharing fails, system displays error ERR004

## Post-conditions
- Resource shared successfully via link or email

## Alternative Flows
- ALT001: If no resource available for sharing, system displays ERR003 and redirects to resources page

## Business Rules
- BC001: Display full details for each resource

## Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR001 | Error | حدث خطأ أثناء تحميل الصفحة. | Page load error |
| ERR003 | Error | حدث خطأ أثناء محاولة مشاركة المصدر. يرجى المحاولة مرة أخرى لاحقاً. | No resource for sharing |
| ERR004 | Error | حدث خطأ أثناء محاولة المشاركة. يرجى المحاولة مرة أخرى لاحقاً. | Share failure |

## Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON002 | تمت مشاركة المصدر بنجاح! |
