# US044 - Upload News & Events

## Epic
Admin News, Events & Resources

## Feature Code
F044

## Sprint
Sprint 13: Admin News, Events & Resources

## Priority
Medium

## User Story
**As an** admin, **I want to** upload news or events, **so that** I can add new content to the platform.

## Roles
| Role | Access |
|------|--------|
| Super Admin | Can |
| Admin | Can |
| Content Manager | Can |

## Preconditions
- User must be registered as admin

## Acceptance Criteria
1. Admin enters platform > "News & Events" > clicks "Add News/Event"
2. System displays upload form. For News: Title (255 chars), Image (PNG), Topic (dropdown CCE), Content (2000 chars). For Event: Title (255 chars), Location (255 chars URL), Event Date (date), Topic (dropdown CCE), Description (2000 chars)
3. Admin fills form and clicks "Submit"
4. System validates input data before uploading (BC001)
5. On success, confirmation message CON021 is displayed
6. On missing required fields, error message ERR013 is displayed
7. On upload error, error message ERR027 is displayed

## Post-conditions
- Admin can delete the news/event if needed

### Alternative Flows
- ALT001: If required fields not filled, system displays ERR013

### Business Rules
- BC001: Validate all input data before uploading news/event

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR013 | Error | عذراً، الحقول الإجبارية غير مكتملة. | Required fields empty |
| ERR027 | Error | عذراً، حدثت مشكلة أثناء رفع الخبر/الفعالية. | News/event upload failure |

### Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON021 | تم رفع المصدر بنجاح! |

### Form Fields & Validation Rules (News)
| Field | Type | Required | Max Length | Validation |
|-------|------|----------|------------|------------|
| Title | Free Text | Yes | 255 | Must be clear and accurate |
| Image | Attachment | Yes | - | Must be PNG format |
| Topic | Dropdown | Yes | - | Must select from CCE topics list |
| News Content | Free Text | Yes | 2000 | Must be clear and accurate |

### Form Fields & Validation Rules (Event)
| Field | Type | Required | Max Length | Validation |
|-------|------|----------|------------|------------|
| Title | Free Text | Yes | 255 | Must be clear and accurate |
| Location | URL | Yes | 255 | Must be a valid URL |
| Event Date | Date | Yes | 500 | Must be valid date format (yyyy-mm-dd) |
| Topic | Dropdown | Yes | - | Must select from CCE topics list |
| Event Description | Free Text | Yes | 2000 | Must be accurate and cover event details |