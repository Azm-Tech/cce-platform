# US038 - Update About Platform

## Epic
Admin Content Management

## Feature Code
F038

## Sprint
Sprint 11: Admin Content Management

## Priority
High

## User Story
**As a** Super Admin/Admin/Content Manager, **I want to** update the "About Platform" page, **so that** I can improve and update the explanatory information displayed to new users about the platform.

## Roles
| Role | Access |
|------|--------|
| Super Admin | Can |
| Admin | Can |
| Content Manager | Can |

## Preconditions
- User must be a logged-in admin

## Acceptance Criteria
1. Admin enters platform > selects "Update About Platform Content"
2. System shows update options
3. Admin selects "Update About Platform"
4. System displays update form with fields: General Description (1000 chars), How to Use (video file), Knowledge Partners (1000 chars), Terminology Dictionary
5. Admin modifies content and clicks "Save & Update"
6. System validates input data before executing update (BC001)
7. On success, confirmation message CON016 is displayed
8. On update error, error message ERR025 is displayed
9. On load error, error message ERR001 is displayed

## Post-conditions
- New content appears on About Platform page immediately

### Alternative Flows
- ALT001: If content update fails, system displays ERR025

### Business Rules
- BC001: Validate input data before executing the update

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR001 | Error | حدث خطأ أثناء تحميل الصفحة. | Page load error |
| ERR025 | Error | عذراً، حدثت مشكلة أثناء تحديث المحتوى. | Content update failure |

### Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON016 | تمت عملية التحديث بنجاح. |

### Form Fields & Validation Rules
| Field | Type | Required | Max Length | Validation |
|-------|------|----------|------------|------------|
| General Description | Free Text | Yes | 1000 | - |
| How to Use | Video File | Yes | - | - |
| Knowledge Partners | Free Text | Yes | 1000 | Comma-separated or multi-line input, up to 100 partners |
| Term (for Terminology Dictionary) | Free Text | Yes | 100 | - |
| Definition (for Terminology Dictionary) | Free Text | Yes | 1000 | - |