# US016 - تعديل الملف الشخصي

## Epic
Profiles & Policies

## Feature Code
F016

## Sprint
Sprint 05: Profiles & Policies

## Priority
Medium

## User Story
**As a** مستخدم للمنصة، **I want to** استعراض الملف الشخصي الخاص بي وتحديثه، **so that** أتمكن من الاطلاع على تفاصيل بياناتي وتحديثها إذا لزم الأمر.

## Roles
| Role | Access |
|------|--------|
| Registered User | Can |

## Preconditions
- User must have a profile

## Acceptance Criteria
1. User navigates to their profile
2. User clicks "Edit"
3. System displays an editable form with the same fields as registration (except password): Country, First Name, Last Name, Email, Job Title, Organization
4. User modifies the desired data
5. User clicks "Save"
6. System retrieves data correctly from the database (BC001)
7. System updates the data successfully after "Save" (BC002)
8. System displays confirmation CON005
9. If invalid data is entered, system displays error ERR007
10. If a load error occurs, system displays error ERR001

## Post-conditions
- Updated profile displayed to user

## Alternative Flows
- ALT001: If profile update fails (e.g., invalid email or phone format), system displays ERR007 and requests correction

## Business Rules
- BC001: Personal data must be correctly retrieved from database
- BC002: Personal data must be successfully updated in database after clicking "Save"

## Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR001 | Error | حدث خطأ أثناء تحميل الصفحة. | Page load error |
| ERR007 | Error | حدث خطأ أثناء محاولة تحديث بيانات الملف الشخصي. يرجى التأكد من أن البيانات المدخلة صحيحة، مثل تنسيق البريد الإلكتروني أو رقم الهاتف. | Profile update validation error |

## Confirmation Messages
| Code | Message (AR) |
|------|-------------|
| CON005 | تم تحديث بيانات الملف الشخصي بنجاح. يمكنك الآن الاطلاع على المعلومات المحدثة في ملفك الشخصي. |
