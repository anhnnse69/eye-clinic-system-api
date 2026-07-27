# Implementation Plan: ClinicDashboardServices Unit Tests

## Scope

Generate xUnit unit tests for `ViewClinicDashboardService` (1 public method: `Process`) targeting **100% line coverage**.

**Source file:** `ECS.Application/Services/ClinicAdminManagementServices/ClinicDashboardServices/ViewClinicDashboardService.cs`
**Interface file:** `ECS.Application/Services/ClinicAdminManagementServices/ClinicDashboardServices/IViewClinicDashboardService.cs`

## Service Architecture Overview

The service has the following execution graph:

```
Process()
├── RetrieveUserId(ref isUserValid)            -- returns Guid, sets isUserValid=false on failure
├── RetrieveClinicId(userId, isUserValid)      -- returns Guid?, null if !isUserValid OR no StaffClinic
│   └── _staffClinicRepository.FindByCondition(...).FirstOrDefaultAsync()
├── RetrieveDashboardData(clinicId)            -- returns ViewClinicDashboardResponse? (null if clinicId==null)
│   ├── _appointmentRepository.FindByCondition(x => x.Doctor.ClinicId == ...).Include(...).ToListAsync()
│   ├── Filter todayAppointments (Date == UtcNow.Date)
│   ├── Count appointments today (Total / Completed / Cancelled)
│   ├── Sum DepositAmount where DepositPaid (TotalRevenue)
│   ├── CountAsync on 4 other repositories (StaffClinic / Service / FacilityRoom / MedicineCatalog)
│   └── BuildWeeklyStatistics(appointments) -- 7-day rolling window
├── ValidateRetrievedData(dashboardData, ref isClinicExist)   -- sets isClinicExist=false if null
└── CreateResponse(dashboardData, isUserValid, isClinicExist)
    ├── CreateErrorResponse(isUserValid, isClinicExist)        -- 4001 or 4020 or null
    └── MapToResponse(dashboardData)                          -- identity copy
```

## Testability Audit (Step 2)

The implementation is testable as-is — no refactor required.

| Concern | Status |
|---|---|
| `Process()` returns `Task<ApiResponse<ViewClinicDashboardResponse>>` | ✅ Testable — verifiable outcome |
| Private helpers (`RetrieveUserId`, `RetrieveClinicId`, `RetrieveDashboardData`, `ValidateRetrievedData`, `CreateResponse`, `CreateErrorResponse`, `MapToResponse`, `BuildWeeklyStatistics`) | ✅ All testable indirectly through `Process` |
| `DateTime.UtcNow` references (used in `today`, `startDate`) | ✅ Acceptable — `BuildWeeklyStatistics` and `TotalAppointments` count depend on `UtcNow.Date` so test appointments must use the same fixed-at-runtime date (today UTC) |
| HttpContext access via `IHttpContextAccessor` | ✅ Mockable via Moq |
| `_appointmentRepository.FindByCondition(...).Include(x => x.Doctor).ToListAsync()` | ✅ Mockable via `MockQueryable.Moq.BuildMockDbSet<>()` |
| 4 separate `CountAsync` calls against independent repositories | ✅ Each repo mock is independent |

## Files to Create

| File | Purpose |
|---|---|
| `ECS.Test/MockData/ClinicDashboardMockData.cs` | Reusable mock entities (DoctorProfile, PatientProfile, TimeSlot, Service, StaffClinic, Service, FacilityRoom, MedicineCatalog, Appointments), deterministic IDs, scenario builders |
| `ECS.Test/Services/ClinicAdminManagementServices/ClinicDashboardServices/ViewClinicDashboardServiceTests.cs` | `[Fact]` test cases following strict 4-step pattern |

## Test Plan — 11 Test Cases

| # | Test Name | Branch / Coverage Target | Mocks |
|---|-----------|---------------------------|-------|
| TC-VCD-01 | `Process_HttpContextHasNoNameIdentifierClaim_Returns4001AuthenticationError` | `RetrieveUserId` (User != null, FindFirst returns null branch), `RetrieveClinicId` (`!isUserValid` short-circuit → null), `RetrieveDashboardData` (clinicId null → returns null), `ValidateRetrievedData` (data null → isClinicExist=false), `CreateErrorResponse` (!isUserValid branch → APP_MESSAGE_4001) | HttpContext User with no claims; appointment repo empty list |
| TC-VCD-02 | `Process_NullHttpContext_Returns4001AuthenticationError` | `RetrieveUserId` (HttpContext null branch of `?.User.FindFirst(...)?.Value` chain) | HttpContext accessor returns null |
| TC-VCD-03 | `Process_NullHttpContextUser_Returns4001AuthenticationError` | `RetrieveUserId` (HttpContext != null AND User == null branch) | HttpContext != null, User == null |
| TC-VCD-04 | `Process_NonGuidClaim_Returns4001AuthenticationError` | `RetrieveUserId` (Guid.TryParse fails on non-Guid string branch) | claim value = "not-a-guid" |
| TC-VCD-05 | `Process_ValidUserButNoStaffClinic_Returns4020ClinicNotFound` | `RetrieveClinicId` (FirstOrDefaultAsync returns null → staffClinic null → returns null), `RetrieveDashboardData` (clinicId null), `CreateErrorResponse` (!isClinicExist branch → APP_MESSAGE_4020) | staffClinic repo empty |
| TC-VCD-06 | `Process_HappyPath_EmptyAppointments_ReturnsSuccessWithZeroMetrics` | All counter branches on zero, `BuildWeeklyStatistics` (7-day loop with no matches), `MapToResponse`, `CreateResponse` (success path → APP_MESSAGE_2000) | all repos return empty / 0 |
| TC-VCD-07 | `Process_HappyPath_TodayCompletedAndCancelledAppointments_ComputedMetrics` | `RetrieveDashboardData` (todayAppointments.Count for Total / Completed / Cancelled, TotalRevenue from DepositPaid) | seed appointments for today: 1 COMPLETED paid, 1 CANCELLED paid, 1 PENDING not paid |
| TC-VCD-08 | `Process_HappyPath_AppointmentFromDifferentDay_ExcludedFromTodayMetrics` | today filter (todayAppointments.Count excludes future/past dates) | seed an appointment for tomorrow, verify TotalAppointments=0 |
| TC-VCD-09 | `Process_HappyPath_NonPaidAppointments_ExcludedFromRevenue` | `TotalRevenue` filter (`Where(x => x.DepositPaid)` branch — false branch) | seed a today appointment with DepositPaid=false |
| TC-VCD-10 | `Process_HappyPath_CountsFromAllResourceRepos_ReflectedInTotals` | TotalStaffs, TotalServices, TotalRooms, TotalMedicines counters | Seed each repo with N rows |
| TC-VCD-11 | `Process_HappyPath_WeeklyStatistics_BuildsSevenDayTimeline` | `BuildWeeklyStatistics` (7-iteration for-loop with Revenue and Appointments sums per day) | Seed appointments spanning previous 7 days |

### Branch coverage notes

- **`RetrieveUserId`** has 5 branches to cover:
  - HttpContext accessor returns null (TC-VCD-02)
  - HttpContext != null, User == null (TC-VCD-03)
  - HttpContext/User != null, FindFirst returns null (TC-VCD-01)
  - HttpContext/User/FindFirst != null, value is non-Guid (TC-VCD-04)
  - Valid Guid path (TC-VCD-06 and all happy-path tests)
- **`RetrieveClinicId`** has 2 branches:
  - `!isUserValid` → return null (TC-VCD-01)
  - success path (TC-VCD-06, TC-VCD-07, etc.)
- **`RetrieveDashboardData`** early-return null branch is exercised whenever `clinicId` is null (TC-VCD-01, TC-VCD-05)
- **`BuildWeeklyStatistics`** for-loop (7 iterations) is exercised in TC-VCD-11 with appointments distributed across the rolling 7-day window
- **`CreateErrorResponse`** three branches: `!isUserValid` (TC-VCD-01), `!isClinicExist` (TC-VCD-05), null return (TC-VCD-06+)
- **`CreateResponse`** two branches: error not null (TC-VCD-01, TC-VCD-05), success path (TC-VCD-06+)

## Mock Setup Helpers

```csharp
private void SetupHttpContextClaim(string? rawClaimValue)
private void SetupHttpContextUserId(Guid userId)
private void SetupStaffClinicRepo(StaffClinic? returnStaffClinic)
private void SetupAppointmentRepo(IEnumerable<Appointment> appointments)
private void SetupStaffClinicCountRepo(int count)
private void SetupServiceCountRepo(int count)
private void SetupRoomCountRepo(int count)
private void SetupMedicineCountRepo(int count)
```

The `_staffClinicRepository` is used twice:
1. `RetrieveClinicId` → `.FindByCondition(...)..FirstOrDefaultAsync()` — returns a `StaffClinic?`
2. `RetrieveDashboardData.TotalStaffs` → `.FindByCondition(...)..CountAsync()` — returns an `int`

Both consume the same `BuildMockDbSet` if the dataset is consistent. To exercise the count branch independently from the lookup branch, the helpers above accept either a `StaffClinic?` (for lookup) or an `int` (for count) and wire the same `BuildMockDbSet`-based IQueryable.

## Strict 4-Step Pattern Applied

Every test will include literal `//Arrange 1`, `//Arrange 2`, `//Act`, and `//Assert` comments.

## Coverage Validation Workflow

1. `cd` to `ECS.Test`, run `run-tests.bat`
2. Open `TestResults/coverage-report/index.html`
3. Filter for class `ECS.Application.Services.ClinicAdminManagementServices.ClinicDashboardServices.ViewClinicDashboardService`
4. Confirm 100% line coverage
5. Iterate on tests if uncovered lines remain

## Cleanup

After plan approval, any `implementation_plan*.md` files in the project root will be moved to this `DOC\docs\implement\` folder.
