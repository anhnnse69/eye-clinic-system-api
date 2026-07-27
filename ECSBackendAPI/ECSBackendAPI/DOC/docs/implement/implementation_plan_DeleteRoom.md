# Implementation Plan: ClinicDeleteRoomServices Unit Tests

## Scope

Generate xUnit unit tests for `DeleteRoomService` (1 public method: `Process`) targeting **100% line coverage** and ≥90% branch coverage.

**Source file:** `ECS.Application/Services/ClinicAdminManagementServices/ClinicDeleteRoomServices/DeleteRoomService.cs`

## Testability Audit (Step 2)

The implementation is testable as-is — no refactor required.

| Concern | Status |
|---|---|
| `private` helpers — `RetrieveUserId`, `RetrieveClinicId`, `FetchRoomData`, `ValidateRoomExistence`, `ApplyStatusMutation`, `MapToResponseDto`, `CreateResponse`, `CreateErrorResponse` | ✅ Testable indirectly through `Process`; no reflection needed |
| `_httpContextAccessor.HttpContext?.User.FindFirst(...)?.Value` null-conditional chain | ✅ Both null branches covered by separate tests |
| `Guid.TryParse(claim, out userId)` true/false branches | ✅ Cover both with valid and invalid Guid strings |
| `EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(...)` for `StaffClinic` and `FacilityRoom` | ✅ Mock via `MockQueryable.Moq` |
| `_facilityRoomRepository.UpdateAsync` / `SaveChangesAsync` | ✅ Mock via `Mock<IRepositoryBaseAsync<...>>` |

## Files to Create

| File | Purpose |
|---|---|
| `ECS.Test/MockData/DeleteRoomMockData.cs` | Reusable mock entities, request factories, deterministic IDs |
| `ECS.Test/Services/ClinicAdminManagementServices/ClinicDeleteRoomServices/DeleteRoomServiceTests.cs` | `[Fact]` test cases following strict 4-step pattern |

## Test Plan — 7 Test Cases

| # | Test Name | Branch Coverage | Mocks |
|---|-----------|-----------------|-------|
| TC-DR-01 | `Process_NullHttpContext_Returns4001AuthenticationError` | `RetrieveUserId` early `null-conditional` (`_httpContextAccessor.HttpContext == null`) | `HttpContext → null` |
| TC-DR-02 | `Process_MissingNameIdentifierClaim_Returns4001AuthenticationError` | `RetrieveUserId` `User.FindFirst → null` branch | `HttpContext → principal without NameIdentifier claim` |
| TC-DR-03 | `Process_InvalidUserIdClaim_Returns4001AuthenticationError` | `RetrieveUserId` `Guid.TryParse` returns false branch | claim value `"not-a-guid"` |
| TC-DR-04 | `Process_ValidUserWithoutActiveClinic_Returns4020RoomNotFoundError` | `RetrieveClinicId` `staffClinic == null` branch → `FetchRoomData` `clinicId null` short-circuit → `ValidateRoomExistence` `originalRoom == null` → `isRoomExist = false` → `CreateErrorResponse` `!isRoomExist → APP_MESSAGE_4020` | staffClinic repo empty; room repo not queried |
| TC-DR-05 | `Process_ActiveRoomNotFoundInClinic_Returns4020RoomNotFoundError` | `RetrieveClinicId` returns valid clinicId → `FetchRoomData` `FirstOrDefaultAsync → null` → `ValidateRoomExistence` false branch → `ApplyStatusMutation` early return (`!isRoomExist`) → `CreateErrorResponse` `!isRoomExist → APP_MESSAGE_4020` | staffClinic OK; room repo empty |
| TC-DR-06 | `Process_ActiveRoomDeactivate_ReturnsSuccessWithInactiveRoom` | Full happy path: `ApplyStatusMutation` success branch → `UpdateAsync` + `SaveChangesAsync` invoked → `MapToResponseDto` populates DTO → `CreateResponse` `success` branch → `APP_MESSAGE_2000`; DTO fields (Id, ClinicId, RoomName, RoomType, IsActive) asserted | staffClinic OK; room with IsActive=true; request IsActive=false |
| TC-DR-07 | `Process_InactiveRoomReactivate_ReturnsSuccessWithActiveRoom` | Happy path toggle branch: `ApplyStatusMutation` re-applies `IsActive = true` → success envelope | staffClinic OK; room with IsActive=false; request IsActive=true |

### Branch coverage notes

- **`RetrieveUserId`** has 4 distinct paths:
  - `_httpContextAccessor.HttpContext == null` → TC-DR-01
  - `HttpContext.User` non-null but `FindFirst → null` → TC-DR-02
  - `FindFirst` returns a claim but `Guid.TryParse` fails → TC-DR-03
  - `Guid.TryParse` succeeds → covered by TC-DR-04..07
- **`RetrieveClinicId`** branches:
  - `isUserValid == false` short-circuit → covered by TC-DR-01/02/03
  - `staffClinic == null` → TC-DR-04
  - `staffClinic != null` → TC-DR-05/06/07
- **`FetchRoomData`** branches:
  - `clinicId.HasValue == false` short-circuit → TC-DR-04
  - `FirstOrDefaultAsync → null` → TC-DR-05
  - `FirstOrDefaultAsync → room` → TC-DR-06/07
- **`ValidateRoomExistence`** branches:
  - `originalRoom == null` → TC-DR-04/05
  - `originalRoom != null` (no change) → TC-DR-06/07
- **`ApplyStatusMutation`** branches:
  - `!isRoomExist || room == null` early return → TC-DR-04/05
  - Full update path → TC-DR-06/07
- **`CreateErrorResponse`** branches:
  - `!isUserValid` → TC-DR-01/02/03
  - `!isRoomExist` → TC-DR-04/05
  - Both valid (returns null) → TC-DR-06/07
- **`CreateResponse`** branches:
  - `errorResponse != null` (error branch) → TC-DR-01..05
  - `errorResponse == null` (success branch with `MapToResponseDto(updatedRoom!)`) → TC-DR-06/07
- **`MapToResponseDto`** mapping all 5 fields → TC-DR-06 (asserts all DTO fields)

### Strict 4-Step Pattern Applied

Every test will include literal `//Arrange 1`, `//Arrange 2`, `//Act`, and `//Assert` comments.

### Mock Setup Helpers

```csharp
private void SetupHttpContextClaim(string? rawClaimValue)
private void SetupHttpContextUserId(Guid userId)
private void SetupStaffClinicRepo(StaffClinic? returnStaffClinic)
private void SetupFacilityRoomRepo(FacilityRoom? existingRoom)
private void SetupRoomUpdateSuccess()
private void SetupRoomUpdateFailure(Exception ex)
```

## Coverage Validation Workflow

1. `cd` to `ECS.Test`, run `run-tests.bat`
2. Open `TestResults/coverage-report/index.html`
3. Filter for class `ECS.Application.Services.ClinicAdminManagementServices.ClinicDeleteRoomServices.DeleteRoomService`
4. Confirm 100% line coverage and ≥90% branch coverage
5. Iterate on tests if uncovered lines remain

## Cleanup

After plan approval, any `implementation_plan*.md` files in `BE\api\ECSBackendAPI\ECSBackendAPI\ECS.Test\` (project root) will be moved to this `DOC\docs\implement\` folder.