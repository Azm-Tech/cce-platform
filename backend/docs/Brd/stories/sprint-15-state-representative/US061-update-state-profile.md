# US061 - Update State Profile

## Epic
State Representative

## Feature Code
F060

## Sprint
Sprint 15: State Representative

## Priority
Medium

## User Story
**As a** State Representative, **I want to** update my country's profile, **so that** I can update country-related information according to the latest available data.

## Roles
| Role | Access |
|------|--------|
| State Representative | Can |
| Admin | Can |
| Super Admin | Can |

## Preconditions
- User must be registered as State Rep
- Profile must be available

## Acceptance Criteria
1. State Rep navigates to state profile and reviews data
2. State Rep clicks "Edit"
3. State Rep modifies editable fields: Population (integer > 0), Area (decimal > 0), GDP per capita (decimal > 0), Nationally Determined Contribution (PNG attachment)
4. CCE Classification, CCE Performance, and CCE Total Index are read-only (retrieved from KAPSARC)
5. State Rep clicks "Save Updates"
6. State Rep can only edit manually entered data; KAPSARC-linked data cannot be modified (BC001)
7. On success, confirmation message CON026 is displayed
8. On missing required fields, error message ERR013 is displayed
9. On update error, error message ERR033 is displayed

## Post-conditions
- State Rep can review updated data or make future modifications

### Alternative Flows
- ALT001: If required fields left empty, system displays ERR013 requesting all mandatory fields be filled

### Business Rules
- BC001: State Rep can only edit manually entered data; KAPSARC-linked data (CCE Classification, Performance, Total Index) cannot be modified

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR013 | Error | عذراً، الحقول الإجبارية غير مكتملة. | Required fields empty |
| ERR033 | Error | عذراً، حدثت مشكلة أثناء تحديث البيانات. | State profile update failure |

### Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON026 | تم تحديث الملف التعريفي للدولة بنجاح! |

### Form Fields & Validation Rules
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| Population | Number/Integer | Yes | Must be an integer greater than 0 |
| Area | Number/Decimal | Yes | Must be greater than 0 |
| GDP per capita | Number/Decimal | Yes | Must be greater than 0 |
| Nationally Determined Contribution (PDF) | Attachment | Yes | Must be PNG format |
| CCE Classification | Text (Display Only) | Yes | Retrieved from KAPSARC, cannot be edited |
| CCE Performance | Text (Display Only) | Yes | Retrieved from KAPSARC, cannot be edited |
| CCE Total Index | Number/Decimal (Display Only) | Yes | Retrieved from KAPSARC, cannot be edited |