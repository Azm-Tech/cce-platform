# US009 - التفاعل مع المدينة التفاعلية

## Epic
Knowledge Maps & Interactive City

## Feature Code
F009

## Sprint
Sprint 03: Knowledge Maps & Interactive City

## Priority
High

## User Story
**As a** مستخدم للمنصة، **I want to** التفاعل مع المدينة التفاعلية، **so that** أتمكن من إدخال البيانات واكتساب معلومات تفاعلية مباشرة من المدينة.

## Roles
| Role | Access |
|------|--------|
| Visitor | Can |
| Registered User | Can |

## Preconditions
- None

## Acceptance Criteria
1. User enters the interactive city
2. User fills in environmental factor values:
   - Public Transport Usage (0-100%)
   - Transport Distance (0-100km)
   - Bike Lanes (integer > 0)
   - Temperature (-50 to 50°C)
   - Precipitation (0-5000mm)
   - Population (integer > 0)
   - Area (decimal > 0)
   - Energy Consumption (0-1000 kWh)
   - Mixed-Use Ratio (0-100%)
   - CO2 Emissions (decimal > 0)
   - Industrial Facilities (integer > 0)
   - Waste Conversion (0-100%)
   - Waste per Person (decimal > 0)
   - Renewable Energy (0-100%)
   - Carbon Intensity (0-1000 g/W)
3. System validates all input data (BC001)
4. Data must update dynamically based on new inputs (BC001)
5. System calculates and displays the city performance index
6. System displays improvement techniques: Reduce, Reuse, Recycle, Reduce emissions
7. If no data is available, system displays ALT001
8. If a load error occurs, system displays error ERR001

## Post-conditions
- Performance index displayed with improvement suggestions

## Alternative Flows
- ALT001: If no interactive city data available, system displays message and redirects to homepage

## Business Rules
- BC001: Data must update dynamically based on new inputs

## Error Codes & Messages
| Code | Type | Message (AR) | Trigger |
|------|------|-------------|---------|
| ERR001 | Error | حدث خطأ أثناء تحميل الصفحة. | Page load error |

## Form Fields & Validation Rules
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| Public Transport Usage | Number/Percentage | Yes | Must be between 0% and 100% |
| Average Transportation Distance | Number/Decimal | Yes | Must be between 0 and 100 km |
| Bike Lanes per km² | Number/Integer | Yes | Must be an integer greater than 0 |
| Average Annual Temperature | Number/Decimal | Yes | Must be between -50 and 50°C |
| Annual Precipitation | Number/Decimal | Yes | Must be between 0 and 5000 mm |
| Population | Number/Integer | Yes | Must be an integer greater than 0 |
| Area of Province | Number/Decimal | Yes | Must be greater than 0 |
| Energy Consumption per km² | Number/Decimal | Yes | Must be between 0 and 1000 kWh |
| Mixed-Use Development Ratio | Number/Percentage | Yes | Must be between 0% and 100% |
| Total CO2 Emissions | Number/Decimal | Yes | Must be greater than 0 |
| Number of Industrial Facilities | Number/Integer | Yes | Must be an integer greater than 0 |
| Waste Conversion Rate | Number/Percentage | Yes | Must be between 0% and 100% |
| Waste per Person per Year | Number/Decimal | Yes | Must be greater than 0 |
| Renewable Energy Production Ratio | Number/Percentage | Yes | Must be between 0% and 100% |
| Carbon Intensity from Electricity | Number/Decimal | Yes | Must be between 0 and 1000 g/W |
