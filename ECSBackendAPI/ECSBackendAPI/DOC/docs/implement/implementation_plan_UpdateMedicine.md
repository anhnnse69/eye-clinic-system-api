# Implementation Plan – UpdateMedicineCatalogService Tests

## Target

- **Service**: `ECS.Application.Services.ClinicAdminManagementServices.ClinicEditMedicineServices.UpdateMedicineCatalogService`
- **Interface**: `IUpdateMedicineCatalogService.Process(UpdateMedicineCatalogRequest request)`
- **Test project**: `ECS.Test`
- **Test file (new)**: `ECS.Test/Services/ClinicAdminManagementServices/ClinicEditMedicineServices/UpdateMedicineCatalogServiceTests.cs`
- **MockData (new)**: `ECS.Test/MockData/UpdateMedicineCatalogMockData.cs`

## Public Method Behavior

`Process` orchestrates a 8-step workflow using helper methods:

1. `RetrieveUserId(ref isUserValid)` — parses `ClaimTypes.NameIdentifier` from `HttpContext`. On failure → `isUserValid=false`.
2. `RetrieveClinicId(userId, isUserValid)` — looks up active `StaffClinic`. Returns `(Guid? clinicId, bool isClinicExist)`.
3. `RetrieveMedicineItem(id, clinicId, isClinicExist)` — fetches `MedicineCatalog` by id and clinicId. Returns `(MedicineCatalog? item, bool isMedicineExist)`.
4. `ValidateNameUniqueness(name, id, clinicId, isMedicineExist)` — checks for duplicate name (case-insensitive, trimmed) excluding current id. Returns `bool isNameUnique`.
5. `SaveMedicineState(item, request, isNameUnique)` — updates properties and persists. Returns `MedicineCatalog?`.
6. `MapToResponse(entity)` — projects to `UpdateMedicineCatalogResponse?`.
7. `CreateResponse(result, ...)` — maps state flags to `ApiResponse<T>`.
8. `CreateErrorResponse(...)` — returns error codes per failure flag.

## Error Code Mapping (from `CreateErrorResponse`)

| Flag | Code | Meaning |
|---|---|---|
| `!isUserValid` | `APP_MESSAGE_4001` | Authentication failure / invalid user id |
| `!isClinicExist` | `APP_MESSAGE_4014` | Active staff-clinic mapping missing |
| `!isMedicineExist` | `APP_MESSAGE_4020` | Medicine not found in catalog |
| `!isNameUnique` | `APP_MESSAGE_4019` | Name duplication |
| All true | `APP_MESSAGE_2000` | Success |

## Testability Audit

- The service is **testable as-is** (returns `ApiResponse<T>` synchronously, no untracked side effects).
- No refactor required.

## Test Cases (one per branch)

| # | Test Method | Scenario | Covers |
|---|---|---|---|
| 1 | `Process_NullHttpContext_Returns4001AuthenticationError` | HttpContext null → claim parse skip → 4001 | `RetrieveUserId` HttpContext-null branch, `RetrieveClinicId` skip, `RetrieveMedicineItem` skip, `ValidateNameUniqueness` skip, `SaveMedicineState` skip, `MapToResponse` skip, `CreateErrorResponse` !isUserValid |
| 2 | `Process_NullHttpContextUser_Returns4001AuthenticationError` | HttpContext with empty User identity → 4001 | `RetrieveUserId` User-null branch |
| 3 | `Process_NoNameIdentifierClaim_Returns4001AuthenticationError` | User has claims but no NameIdentifier → 4001 | `RetrieveUserId` FindFirst-null branch |
| 4 | `Process_InvalidGuidClaim_Returns4001AuthenticationError` | Claim value is non-Guid string → 4001 | `RetrieveUserId` Guid.TryParse-false branch |
| 5 | `Process_ValidUserNoStaffClinic_Returns4014ClinicNotFound` | No active StaffClinic → 4014 | `RetrieveClinicId` staffClinic-null branch, `CreateErrorResponse` !isClinicExist |
| 6 | `Process_ValidClinicMedicineNotFound_Returns4020MedicineNotFound` | Clinic ok but no Medicine row → 4020 | `RetrieveMedicineItem` isClinicExist=false branch OR item-null branch, `CreateErrorResponse` !isMedicineExist |
| 7 | `Process_DuplicateMedicineName_Returns4019DuplicateName` | Another Medicine with same name (case-insensitive trimmed) → 4019 | `ValidateNameUniqueness` duplicate branch, `SaveMedicineState` isNameUnique=false branch, `CreateErrorResponse` !isNameUnique |
| 8 | `Process_UniqueMedicineName_Returns2000WithUpdatedFields` | Happy path → 2000 with all fields mapped | `SaveMedicineState` success branch, `MapToResponse` success branch, `CreateResponse` success branch |

> Note: Branches `!isUserValid` (4001) and `!isClinicExist` (4014) are mutually exclusive pre-conditions. Medicine test #6 covers the `isClinicExist=true` but `item==null` branch. The `!isClinicExist` false branch with `clinicId.HasValue=false` is exercised by #5 (no staffClinic). To additionally cover the explicit `isClinicExist=false` short-circuit in `RetrieveMedicineItem` (defense-in-depth), we add a separate test.

| # | Test Method | Scenario | Covers |
|---|---|---|---|
| 9 | `Process_ValidUserIdNoActiveClinic_MedicineLookupSkipped` | User OK but clinicId null → 4014 | `RetrieveMedicineItem` `!isClinicExist` short-circuit branch (defense-in-depth) |

## Branch Coverage Target

Each helper has these branches:

- `RetrieveUserId`: 4 branches (HttpContext null, FindFirst null, Guid.TryParse false, success) → tests #1, #2, #3, #4, #5+ all cover.
- `RetrieveClinicId`: 2 branches (isUserValid=false short-circuit, staffClinic null) → #1, #5, #6.
- `RetrieveMedicineItem`: 3 branches (isClinicExist/clinicId null short-circuit, item null, success) → #5/#9, #6, #7/#8.
- `ValidateNameUniqueness`: 3 branches (isMedicineExist/clinicId null short-circuit, duplicate found, unique) → #6, #7, #8.
- `SaveMedicineState`: 2 branches (item null or !isNameUnique skip, success) → #6/#7, #8.
- `MapToResponse`: 2 branches (entity null, success) → #6/#7, #8.
- `CreateErrorResponse`: 4 branches (!isUserValid, !isClinicExist, !isMedicineExist, !isNameUnique) → #1-#4, #5, #6, #7. Plus `null` success → #8.
- `CreateResponse`: 2 branches (errorResponse null short-circuit, success path) → #1-#7, #8.

## Verification Steps

1. Run `run-tests.bat`.
2. Read `TestResults/coverage-report` HTML — confirm class `UpdateMedicineCatalogService` shows **100% line coverage**.
3. If any branch is uncovered, add additional test cases and rerun.
