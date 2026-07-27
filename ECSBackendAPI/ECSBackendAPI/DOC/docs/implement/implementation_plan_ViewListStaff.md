# Implementation Plan — ViewListStaff

## Scope

Target folder:

- `BE/api/ECSBackendAPI/ECSBackendAPI/ECS.Application/Services/ClinicAdminManagementServices/ClinicViewListStaffAccountServices/`

The folder contains one concrete application service and its public contract:

- `ECS.Application.Services.ClinicAdminManagementServices.ViewListStaffAccountsServices.ViewListStaffService`
- `IViewListStaffService.Process(ViewListStaffRequest)`

The request, response DTOs are support types, not additional application services. The service-specific coverage target is therefore `ViewListStaffService`.

> Note: The production code uses `namespace = ECS.Application.Services.ClinicAdminManagementServices.ViewListStaffAccountsServices` (note the trailing `s` in `Accounts`), even though the folder lives under `ClinicViewListStaffAccountServices`. Tests must mirror the production namespace.

## Current test status

Inspection of `BE/api/ECSBackendAPI/ECSBackendAPI/ECS.Test/Services/ClinicAdminManagementServices/` found no `ClinicViewListStaffAccountServices` folder. The service has **no existing unit tests**.

A new test class will be created at:

- `BE/api/ECSBackendAPI/ECSBackendAPI/ECS.Test/Services/ClinicAdminManagementServices/ClinicViewListStaffAccountServices/ViewListStaffServiceTests.cs`

## Read and analysis

`Process` runs the following sequential steps:

1. `RetrieveUserId(out isUserValid)` — extracts `ClaimTypes.NameIdentifier` from `IHttpContextAccessor.HttpContext.User` and parses to `Guid`. Returns `Guid.Empty` and sets `isUserValid = false` if any link in the chain is null or the value is not a valid Guid.
2. `RetrieveClinicId(userId, isUserValid)` — if `isUserValid` is false, returns `null` immediately. Otherwise calls `_staffClinicRepository.FindByCondition(...).FirstOrDefaultAsync()` to find the active `StaffClinic` row for that user. Returns `staffClinic?.ClinicId`.
3. `ValidateClinicExistence(clinicId)` — returns `clinicId.HasValue`.
4. `BuildStaffQuery(clinicId, isClinicExist, request)`:
   - If `!isClinicExist` returns `Enumerable.Empty<StaffClinic>().AsQueryable()`.
   - Otherwise `query = _staffClinicRepository.FindByCondition(x => x.ClinicId == clinicId!.Value, false, x => x.User!)`.
   - If `request.IsActive.HasValue` → `query = query.Where(x => x.IsActive == request.IsActive.Value)`.
   - If `!string.IsNullOrWhiteSpace(request.SearchTerm)` → `query = query.Where(x => x.User!.FullName.ToLower().Contains(cleanSearch) || x.User!.Email.ToLower().Contains(cleanSearch) || x.User!.Phone.Contains(cleanSearch))`.
5. `ComputeTotalCount(query, isClinicExist)` — if `!isClinicExist` returns 0; otherwise `CountAsync(query)`.
6. `FetchPagedStaffData(query, request, isClinicExist)` — if `!isClinicExist` returns `new List<StaffClinic>()`; otherwise `OrderByDescending(x => x.CreatedAt).Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync()`.
7. `CreateResponse(staffList, totalCount, request, isUserValid, isClinicExist)` → `CreateErrorResponse` first.
8. `CreateErrorResponse(isUserValid, isClinicExist)` — returns `Fail(APP_MESSAGE_4001)` if `!isUserValid`, `Fail(APP_MESSAGE_4020)` if `!isClinicExist`, otherwise `null`.
9. `MapToResponse(staffList)` — projects each `StaffClinic` to `StaffAccountResponse`:
   - `UserId = item.UserId`
   - `FullName = item.User?.FullName ?? string.Empty`
   - `Email = item.User?.Email ?? string.Empty`
   - `Phone = item.User?.Phone ?? string.Empty`
   - `Role` switch: `DOCTOR → "Bác sĩ"`, `RECEPTIONIST → "Tiếp tân"`, `CLINIC_ADMIN → "Quản lý phòng khám"`, default → `item.Role.ToString()`
   - `IsActive = item.IsActive`, `CreatedAt = item.CreatedAt`

The guard flag `isUserValid` is propagated through `out` parameter and `isClinicExist` from `ValidateClinicExistence`. The full success path requires both `isUserValid == true` and `isClinicExist == true`.

## Testability audit

- The public method returns `ApiResponse<List<StaffAccountResponse>>`, so its result is observable without reflection.
- `IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>` is mockable.
- `IHttpContextAccessor` is mockable.
- `MockQueryable.Moq` is required for the EF Core async queries (`FirstOrDefaultAsync`, `CountAsync`, `ToListAsync` after `OrderByDescending/Skip/Take`).
- The base `FindByCondition` overload with `includeProperties` (`x => x.User!`) is an `IQueryable` call. The mock will return `IQueryable<StaffClinic>` whose provider supports `FirstOrDefaultAsync` / `CountAsync` / `ToListAsync` via `MockQueryable.Moq`.
- No production refactor is required.

## Planned test cases

All tests must follow the strict four-literal-step pattern (`//Arrange 1`, `//Arrange 2`, `//Act`, `//Assert`) and use `[Method]_[State]_[Outcome]` naming. `MockQueryable.Moq` is used for the staff-clinic repository.

### `Process`

1. `Process_NullHttpContext_Returns4001AuthenticationError`
   - `_httpContextAccessor.HttpContext` returns null. Covers `RetrieveUserId` null-HttpContext branch, `RetrieveClinicId` short-circuit, `CreateErrorResponse` `!isUserValid` branch (`APP_MESSAGE_4001`). StaffClinic repo never reached.
2. `Process_NullHttpContextUser_Returns4001AuthenticationError`
   - HttpContext is non-null but `HttpContext.User` is null. Covers the `?.User` short-circuit branch in `RetrieveUserId`.
3. `Process_MissingNameIdentifierClaim_Returns4001AuthenticationError`
   - HttpContext has no `ClaimTypes.NameIdentifier` claim. Covers `RetrieveUserId` missing-claim branch.
4. `Process_InvalidGuidClaim_Returns4001AuthenticationError`
   - Claim value is `"not-a-guid"` → `Guid.TryParse` fails. Covers `RetrieveUserId` invalid-Guid branch.
5. `Process_ValidUserNoActiveClinic_Returns4020ClinicNotFound`
   - Valid Guid claim, but `StaffClinic` query returns empty (no active assignment). Covers `RetrieveClinicId` `FirstOrDefaultAsync` returns-null branch and `CreateErrorResponse` `!isClinicExist` branch (`APP_MESSAGE_4020`).
6. `Process_HappyPath_EmptyStaffList_Returns2000WithEmptyData`
   - Valid Guid + active StaffClinic + empty staff list. Covers `BuildStaffQuery` happy path, `ComputeTotalCount` zero-count, `FetchPagedStaffData` empty-list path, `MapToResponse` empty list, `CreateResponse` success path, `MetaResponse` with `Total = 0`.
7. `Process_HappyPath_SearchTermTrimsAndLowercases`
   - `SearchTerm = " nguyen "` → exercises `Trim().ToLower()` and the `FullName.ToLower().Contains(cleanSearch)` arm of the SearchTerm OR.
8. `Process_HappyPath_SearchTermMatchesEmail`
   - `SearchTerm` matches `Email` (not `FullName`). Covers `Email.ToLower().Contains(cleanSearch)` arm.
9. `Process_HappyPath_SearchTermMatchesPhone`
   - `SearchTerm` matches `Phone` (not `FullName` or `Email`). Covers `Phone.Contains(cleanSearch)` arm (note: Phone is matched WITHOUT ToLower — this is the production behavior).
10. `Process_HappyPath_SearchTermNull_SkipsNameMatching`
    - `SearchTerm = null` exercises `IsNullOrWhiteSpace` true branch (skip filter).
11. `Process_HappyPath_SearchTermWhitespace_SkipsNameMatching`
    - `SearchTerm = "   "` exercises `IsNullOrWhiteSpace` true branch (whitespace-only considered empty).
12. `Process_HappyPath_IsActiveTrueFilter`
    - `IsActive = true` exercises `request.IsActive.HasValue` true branch + `x.IsActive == request.IsActive.Value` true branch.
13. `Process_HappyPath_IsActiveFalseFilter`
    - `IsActive = false` exercises `request.IsActive.HasValue` true branch + `x.IsActive == request.IsActive.Value` false branch.
14. `Process_HappyPath_IsActiveNull_SkipsIsActiveFilter`
    - `IsActive = null` exercises `request.IsActive.HasValue` false branch (skip filter).
15. `Process_HappyPath_PaginationMeta_PopulatedFromRequest`
    - Multi-row dataset with `PageNumber=2`, `PageSize=5` → `Skip(5) Take(5)` → exactly 5 items returned. Confirms `OrderByDescending` + `Skip` + `Take` chain and `MetaResponse` construction.
16. `Process_HappyPath_MapToResponse_DoctorRole`
    - Single staff with `Role = StaffRole.DOCTOR` → maps to `"Bác sĩ"`.
17. `Process_HappyPath_MapToResponse_ReceptionistRole`
    - Single staff with `Role = StaffRole.RECEPTIONIST` → maps to `"Tiếp tân"`.
18. `Process_HappyPath_MapToResponse_ClinicAdminRole`
    - Single staff with `Role = StaffRole.CLINIC_ADMIN` → maps to `"Quản lý phòng khám"`.
19. `Process_HappyPath_MapToResponse_UnknownRoleFallback`
    - `Role` is a value not enumerated in the switch (use the underlying `(StaffRole)999` cast or rely on the default branch with a non-matching value). Covers the default fallback `_ => item.Role.ToString()`.
20. `Process_HappyPath_MapToResponse_NullUserFieldsFallbackToEmpty`
    - Staff with `User = null` → `FullName`, `Email`, `Phone` all become `string.Empty`. Covers `?.` null-coalescing arms.

Each test must explicitly `Verify` (or `Verify ... Times.Never`) the repository calls it expects to validate both the success and the failure paths.

> **Implementation note for TC-VLS-19**: Because `StaffRole` enum only has 3 values, the only way to exercise the default branch is when the underlying byte value does not match any case. C#'s switch with explicit case values does NOT call the default branch simply because no enum member matches the runtime value when the variable is typed as `StaffRole`. The default branch can therefore be exercised by casting an integer to `StaffRole` (e.g., `(StaffRole)999`) — this is what the production code's switch transform will see.

> **Implementation note for TC-VLS-20**: The `User` navigation property is `virtual User User { get; set; } = null!`. Assigning `User = null!` in the test will make `item.User?.FullName ?? string.Empty` evaluate to `string.Empty`, covering the null-coalescing arms.

## Execution plan

1. Create `ViewListStaffServiceTests.cs` in the test folder listed above with the cases above.
2. Run `.\run-tests.bat` from `ECS.Test` to build in Release, run all tests, collect XPlat coverage, and generate the service-only HTML report.
3. Filter the HTML report to `ViewListStaffService` and parse the Cobertura XML to verify the exact line percentage.
4. If line coverage is below 100%, inspect the report for uncovered lines, add the smallest focused test, rerun, and repeat until 100% line coverage is achieved.
5. Report test pass/fail status, exact line coverage, branch coverage as supplemental information, and the absolute `file:///` link to the final HTML report.

## Coverage tooling prerequisite

The test project already references `coverlet.collector`, `MockQueryable.Moq`, xUnit, Moq, and FluentAssertions. The `reportgenerator` tool will be installed automatically by `run-tests.bat` if it is not already present.

## Expected changes

- A new test file `ViewListStaffServiceTests.cs` will be created.
- Production code will remain unchanged unless a coverage run proves a behavior-preserving refactor is necessary and separately approved.

## Approval gate

`request_feedback = true`

Please approve this plan before test execution or test-file changes proceed.
