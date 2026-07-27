# Implementation Plan: ClinicCreateStaffAccountServices Unit Tests

## Scope

Generate xUnit unit tests for `CreateStaffService` (1 public method: `Process`) targeting **100% line coverage** and ≥90% branch coverage.

**Source file:** `ECS.Application/Services/ClinicAdminManagementServices/ClinicCreateStaffAccountServices/CreateStaffService.cs`

## Testability Audit (Step 2)

The implementation is testable as-is — no refactor required.

| Concern | Status |
|---|---|
| `PersistStaffDataGraph` uses `BeginTransactionAsync` returning `IDbContextTransaction` | ✅ Mock via `Mock<IDbContextTransaction>` (loose mock — `CommitAsync`/`RollbackAsync`/`Dispose` no-op) |
| `BCrypt.Net.BCrypt.HashPassword` is non-deterministic side-effect | ✅ Acceptable — production code path tested, hash itself is not asserted |
| `private` helpers — `RetrieveAdminUserId`, `ConstructUserEntityTree`, etc. | ✅ Testable indirectly through `Process`; no reflection needed |
| `Guid.NewGuid()` & `DateTime.UtcNow` inside `ConstructUserEntityTree` | ✅ Captured via `It.Is<User>(u => ...)` assertions rather than literal value match |

## Files to Create

| File | Purpose |
|---|---|
| `ECS.Test/MockData/CreateStaffMockData.cs` | Reusable mock entities, request factories, deterministic IDs |
| `ECS.Test/Services/ClinicAdminManagementServices/ClinicCreateStaffAccountServices/CreateStaffServiceTests.cs` | `[Fact]` test cases following strict 4-step pattern |

## Test Plan — 8 Test Cases

| # | Test Name | Branch Coverage | Mocks |
|---|-----------|-----------------|-------|
| TC-CSA-01 | `Process_HttpContextHasNoNameIdentifierClaim_Returns4014AdminError` | `RetrieveAdminUserId` false branch; `RetrieveContextClinicId` `!isCurrentAdminValid` short-circuit; `PersistStaffDataGraph` early return; `FilterSystemicValidationFailures` `!adminState` first check | `HttpContext → null principal` |
| TC-CSA-02 | `Process_NullHttpContext_Returns4014AdminError` | `RetrieveAdminUserId` early `null-conditional` (`_httpContextAccessor.HttpContext == null`) | `HttpContext → null` |
| TC-CSA-03 | `Process_ValidUserButNoStaffClinic_Returns4014AdminError` | `RetrieveContextClinicId` `bindingNode == null` branch | staffClinic repo empty |
| TC-CSA-04 | `Process_PhoneAlreadyExists_Returns4018PhoneError` | `CheckPhoneUniqueness` `AnyAsync → true` | user repo for phone returns 1 row |
| TC-CSA-05 | `Process_EmailAlreadyExists_Returns4017EmailError` | `CheckEmailUniqueness` `AnyAsync → true` | user repo for email returns 1 row |
| TC-CSA-06 | `Process_PersistenceThrows_Returns5001GeneralError` | `PersistStaffDataGraph` `catch (Exception)` branch → `RollbackAsync` executed | `CreateAsync` throws |
| TC-CSA-07 | `Process_ReceptionistRole_ValidRequest_ReturnsSuccessWithAssignedRole` | `switch` RECEPTIONIST → UserRole.RECEPTIONIST default branch; full success path including `MapToResponse` | full happy path: empty uniqueness lists, returns success |
| TC-CSA-08 | `Process_DoctorRole_ValidRequest_PersistsAsDoctorRole` | `switch` DOCTOR → UserRole.DOCTOR branch | full happy path; assert persisted entity `Role == UserRole.DOCTOR` |

### Branch coverage notes

- **`switch (parsedStaffRole)`** has 3 cases: `DOCTOR`, `CLINIC_ADMIN`, `RECEPTIONIST`. To cover `CLINIC_ADMIN` cleanly without duplicating TC-CSA-07, we exploit that the implementation initializes `mappedUserRole = UserRole.RECEPTIONIST;` then **only overwrites** for DOCTOR / CLINIC_ADMIN, leaving RECEPTIONIST to hit the literal assignment at line 170. The minimum-coverage trio needs:
  - DOCTOR (TC-CSA-08)
  - RECEPTIONIST (TC-CSA-07) — also exercises the default branch where `mappedUserRole` keeps its initial value
  - CLINIC_ADMIN — required to hit branch #2 of the switch
- A 9th test `TC-CSA-09: Process_ClinicAdminRole_ValidRequest_PersistsAsClinicAdminRole` will be added to cover the `CLINIC_ADMIN` switch branch.

Updated final count: **9 test cases**.

### Additional covering note

- `Enum.TryParse<StaffRole>` always succeeds for valid enum values passed from `CreateStaffRequest.StaffRole`. The "false" branch (line 156-158) returns `null`; this branch is unreachable through the public surface because the request property is typed as `StaffRole` (compiler enforced). **This branch is intentionally excluded from branch-coverage goals** since it cannot be reached via valid input. No test is added for it.

## Mock Setup Helpers

```csharp
private void SetupHttpContextClaim(string? rawClaimValue)
private void SetupHttpContextUserId(Guid userId)
private void SetupStaffClinicRepo(StaffClinic? returnStaffClinic)
private void SetupUserPhoneRepo(User? existingPhoneUser)
private void SetupUserEmailRepo(User? existingEmailUser)
private void SetupUserTransactionSuccess(Guid returnNewId)
private void SetupUserTransactionFailure(Exception thrown)
```

For `SetupUserTransactionSuccess/Failure`, a `Mock<IDbContextTransaction>` will be created and used for `BeginTransactionAsync()` — `CommitAsync()` and `Dispose()` are loose no-ops. Moq can supply `Mock.Of<IDbContextTransaction>()` for the success path; failure path needs explicit setup of `RollbackAsync()`.

## Strict 4-Step Pattern Applied

Every test will include literal `//Arrange 1`, `//Arrange 2`, `//Act`, and `//Assert` comments.

## Coverage Validation Workflow

1. `cd` to `ECS.Test`, run `run-tests.bat`
2. Open `TestResults/coverage-report/index.html`
3. Filter for class `ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices.CreateStaffService`
4. Confirm 100% line coverage and ≥90% branch coverage
5. Iterate on tests if uncovered lines remain

## Cleanup

After plan approval, any `implementation_plan*.md` files in `BE\api\ECSBackendAPI\ECSBackendAPI\ECS.Test\` (project root) will be moved to this `DOC\docs\implement\` folder.
