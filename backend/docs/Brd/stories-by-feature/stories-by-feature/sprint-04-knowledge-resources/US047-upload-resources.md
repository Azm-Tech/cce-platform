# US047 - Upload Resources

## Epic
Admin News, Events & Resources

## Feature Code
F047

## Sprint
Sprint 13: Admin News, Events & Resources

## Priority
Medium

## User Story
**As an** admin, **I want to** upload resources, **so that** I can add new content to the platform.

## Roles
| Role | Access |
|------|--------|
| Super Admin | Can |
| Admin | Can |
| Content Manager | Can |

## Preconditions
- User must be registered as admin

## Acceptance Criteria
1. Admin enters platform > "Resources" > clicks "Add Resource"
2. System displays upload form with fields: Title (255 chars), Topic (dropdown CCE), Description (500 chars), Publication Type (dropdown: paper/article/study/presentation/scientific paper/report/book/re research/CCE guide/media), Covered Countries (multi-select), File (PDF/Word or link)
3. Admin fills form and clicks "Submit"
4. System validates input data before uploading (BC001)
5. On success, confirmation message CON021 is displayed
6. On missing required fields, error message ERR013 is displayed
7. On upload error, error message ERR029 is displayed

## Post-conditions
- Admin can delete the resource if needed

### Alternative Flows
- ALT001: If required fields not filled, system displays ERR013

### Business Rules
- BC001: Validate all input data before uploading resource

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR013 | Error | عذراً، الحقول الإجبارية غير مكتملة. | Required fields empty |
| ERR029 | Error | عذراً، حدثت مشكلة أثناء رفع المصدر. | Resource upload failure |

### Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON021 | تم رفع المصدر بنجاح! |

### Form Fields & Validation Rules
| Field | Type | Required | Max Length | Validation |
|-------|------|----------|------------|------------|
| Title | Free Text | Yes | 255 | Must be clear and accurate |
| Topic | Dropdown | Yes | - | Must select from CCE topics list |
| Description | Free Text | Yes | 500 | - |
| Publication Type | Dropdown | Yes | - | Options: Paper, Article, Study, Presentation, Scientific Paper, Report, Book, Research, CCE Guide, Media |
| Covered Countries | Multi-select Dropdown | Yes | - | Must select from countries list |
| File | File/Link | Yes | - | Must be PDF or Word, or a valid link |