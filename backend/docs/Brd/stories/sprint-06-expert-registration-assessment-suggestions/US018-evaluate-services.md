# US018 - Evaluate Services

## Epic
Assessment

## Feature Code
F018

## Sprint
Sprint 06: Expert Registration, Assessment & Suggestions

## Priority
Medium

## User Story
**As a** platform user, **I want to** evaluate the platform services, **so that** I can share my experience and improve the service provided.

## Roles
| Role | Access |
|------|--------|
| Visitor | Can |
| Registered User | Can |

## Preconditions
- User must be logged in or on second visit to the platform

## Acceptance Criteria
1. User enters platform and navigates to homepage
2. System displays assessment form
3. User fills form with 4 radio button questions: overall satisfaction, ease of use, content suitability, personalized suggestions suitability
4. User optionally enters feedback (500 chars max)
5. User clicks "Submit"
6. System confirms submission → CON008
7. If submission error occurs → ERR009

## Post-conditions
- None

### Alternative Flows
- ALT001: If evaluation submission fails, system displays ERR009

### Business Rules
- BC001: Evaluation must be saved correctly in the database for reporting purposes

### Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR009 | Error | حدث خطأ أثناء محاولة إرسال تقييمك. يرجى المحاولة مرة أخرى. | Evaluation submission error |

### Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON008 | تم إرسال تقييمك بنجاح. نشكرك على مشاركتك في تحسين خدماتنا. |

### Form Fields & Validation Rules
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| How would you rate your overall satisfaction with the platform? | Radio Button | Yes | Select from 5 options: Excellent, Satisfied, Neutral, Dissatisfied, Poor |
| How would you rate the ease of use of the platform? | Radio Button | Yes | Select from 5 options: Excellent, Satisfied, Neutral, Dissatisfied, Poor |
| How suitable is the platform's content for your knowledge level? | Radio Button | Yes | Select from 5 options: Excellent, Satisfied, Neutral, Dissatisfied, Poor |
| How suitable are the personalized suggestions to your interests? | Radio Button | Yes | Select from 5 options: Excellent, Satisfied, Neutral, Dissatisfied, Poor |
| Do you have any other feedback or complaints? Please mention them below. | Free Text | No | 500 chars |