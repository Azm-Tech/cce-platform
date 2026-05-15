# US019 - Personalized Suggestions

## Epic
Suggestions

## Feature Code
F019

## Sprint
Sprint 06: Expert Registration, Assessment & Suggestions

## Priority
High

## User Story
**As a** platform user, **I want to** receive personalized suggestions based on my personal information, **so that** I can access content and resources that match my interests and needs.

## Roles
| Role | Access |
|------|--------|
| Registered User | Can |

## Preconditions
- User must be logged in

## Acceptance Criteria
1. User enters platform
2. System displays personalized suggestions form
3. User fills Areas of Interest (checkbox, CCE topics, required)
4. User selects Knowledge Level (radio: high/medium/low, required)
5. User selects Work Sector (radio: government/academic/private, required)
6. User selects Country (dropdown, required)
7. User clicks "Submit"
8. System confirms submission → CON009
9. System reorders resources, news, events, and community posts by relevance
10. If submission error occurs → ERR010

## Post-conditions
- User can return to modify preferences

### Alternative Flows
- ALT001: If submission fails, system displays ERR010

### Business Rules
- BC001: Suggestions must be generated based on user's answers in the form

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR010 | Error | حدث خطأ أثناء محاولة إرسال بياناتك. يرجى المحاولة مرة أخرى. | Suggestions submission error |

### Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON009 | تم إرسال بياناتك بنجاح! سيتم تخصيص المقترحات لتتناسب مع اهتماماتك واحتياجاتك. |

### Form Fields & Validation Rules
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| Areas of Interest | Checkbox | Yes | Must select from CCE topics |
| Circular Carbon Economy Knowledge Level | Radio Button | Yes | Select from: High, Medium, Low |
| Sector of Work | Radio Button | Yes | Select from: Government, Academic, Private |
| Country | Dropdown | Yes | Must select from country list |