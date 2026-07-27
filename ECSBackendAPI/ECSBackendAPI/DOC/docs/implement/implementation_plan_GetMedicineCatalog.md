# Implementation Plan — GetMedicineCatalog

## Scope

Target folder:

- `BE/api/ECSBackendAPI/ECSBackendAPI/ECS.Application/Services/ClinicAdminManagementServices/ClinicViewListMedicineServices/`

The folder contains one concrete application service and its public contract:

- `ECS.Application.Services.ClinicAdminManagementServices.ClinicViewListMedicineServices.GetMedicineCatalogService`
- `IGetMedicineCatalogService.Process(GetMedicineCatalogRequest)`

The request, response DTOs are support types, not additional application services. The service-specific coverage target is therefore `GetMedicineCatalogService`.

## Current test status

Inspection of `BE/api/ECSBackendAPI/ECSBackendAPI/ECS.Test/Services/ClinicAdminManagementServices/` found no test folder for `ClinicViewListMedicineServices`. The service has **no existing unit tests**.

A new test class will be created at:

- `BE/api/ECSBackendAPI/ECSBackendAPI/ECS.Test/Services/ClinicAdminManagementServices/ClinicViewListMedicineServices/GetMedicineCatalogServiceTests.cs`

## Read and analysis

`Process` runs the following sequential steps:

1. `RetrieveUserId(out isUserValid)` — extracts `ClaimTypes.NameIdentifier` from `IHttpContextAccessor.HttpContext.User` and parses to `Guid`. Returns `Guid.Empty` and sets `isUserValid = false` if any link in the chain is null or the value is not a valid Guid.
2. `RetrieveClinicId(userId, isUserValid)` — if `isUserValid` is false, returns `null` immediately. Otherwise calls `_staffClinicRepository.FindByCondition(...).AsQueryable()` and `EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(query)` to find the active `StaffClinic` row for that user. Returns `staffClinic?.ClinicId`.
3. `BuildFilterExpression(clinicId ?? Guid.Empty, request)` — composes a composite predicate:
   - `x.ClinicId == clinicId`
   - `!request.IsActive.HasValue || x.IsActive == request.IsActive.Value`
   - `string.IsNullOrEmpty(searchTerm) || x.MedicineName.ToLower().Contains(searchTerm) || (x.GenericName != null && x.GenericName.ToLower().Contains(searchTerm)) || (x.Manufacturer != null && x.Manufacturer.ToLower().Contains(searchTerm))`
4. `ExecutePagedQuery(filterExpression, request)` — calls `_medicineRepository.FindByCondition(filterExpression, false).AsQueryable()`, counts via `EntityFrameworkQueryableExtensions.CountAsync(query)`, then `OrderBy(x => x.MedicineName).Skip((PageNumber - 1) * PageSize).Take(PageSize)` and `ToListAsync`.
5. `MapToResponseDto(medicines)` — projects each `MedicineCatalog` to `GetMedicineCatalogResponse`, including `CreatedAt` formatted as `dd/MM/yyyy HH:mm`.
6. `BuildPaginationMeta(request, totalRecords)` — `new MetaResponse(request.PageNumber, request.PageSize, totalRecords)`.
7. `CreateResponse(result, meta, isUserValid, clinicId.HasValue)` — checks `CreateErrorResponse` first. If no error, returns `ApiResponse<List<GetMedicineCatalogResponse>>.Success(APP_MESSAGE_2000, result, meta)`.
8. `CreateErrorResponse(isUserValid, isClinicExist)` — returns `Fail(APP_MESSAGE_4001)` if `!isUserValid`, `Fail(APP_MESSAGE_4020)` if `!isClinicExist`, otherwise `null`.

The guard flag `isUserValid` is propagated through `out` parameters and `clinicId.HasValue` is used to detect the "no clinic bound" case. The full success path requires both `isUserValid == true` and `clinicId.HasValue == true`.

## Testability audit

- The public method returns `ApiResponse<List<GetMedicineCatalogResponse>>`, so its result is observable without reflection.
- `IRepositoryQueryBase<MedicineCatalog, Guid, AppDbContext>` and `IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>` are mockable.
- `IHttpContextAccessor` is mockable.
- `MockQueryable.Moq` is required for the EF Core async queries (`FirstOrDefaultAsync`, `CountAsync`, `ToListAsync` after `OrderBy/Skip/Take`).
- No production refactor is required.

## Planned test cases

All tests must follow the strict four-literal-step pattern (`//Arrange 1`, `//Arrange 2`, `//Act`, `//Assert`) and use `[Method]_[State]_[Outcome]` naming. `MockQueryable.Moq` is used for both repositories.

### `Process`

1. `Process_NullHttpContext_ReturnsAuthenticationError`
   - `_httpContextAccessor.HttpContext` returns null. Covers `RetrieveUserId` null-HttpContext branch, `RetrieveClinicId` short-circuit, `CreateErrorResponse` `!isUserValid` branch (`APP_MESSAGE_4001`). StaffClinic repo never reached.
2. `Process_MissingNameIdentifierClaim_ReturnsAuthenticationError`
   - HttpContext has no `ClaimTypes.NameIdentifier` claim. Covers `RetrieveUserId` missing-claim branch. StaffClinic repo never reached.
3. `Process_InvalidGuidClaim_ReturnsAuthenticationError`
   - Claim value is `"not-a-guid"` → `Guid.TryParse` fails. Covers `RetrieveUserId` invalid-Guid branch.
4. `Process_NullHttpContextUser_ReturnsAuthenticationError`
   - HttpContext is non-null but `HttpContext.User` is null. Covers the `?.User` short-circuit branch in `RetrieveUserId`.
5. `Process_ValidUserNoActiveClinic_ReturnsClinicNotFoundError`
   - Valid Guid claim, but `StaffClinic` query returns empty (no active assignment). Covers `RetrieveClinicId` `FirstOrDefaultAsync` returns-null branch and `CreateErrorResponse` `!isClinicExist` branch (`APP_MESSAGE_4020`). Medicine repo never reached.
6. `Process_HappyPath_EmptyMedicineList_Returns2000WithEmptyData`
   - Valid Guid + active StaffClinic + empty medicine list. Covers `ExecutePagedQuery` zero-count path, `MapToResponseDto` empty-list path, `BuildPaginationMeta` with `Total = 0`, `CreateResponse` success path.
7. `Process_HappyPath_SearchTermTrimsAndLowercases`
   - Valid Guid + active StaffClinic + matching medicine. `SearchTerm = " PARA "` exercises `Trim().ToLower()` and the `MedicineName.Contains` arm of the SearchTerm OR.
8. `Process_HappyPath_SearchTermMatchesGenericName`
   - `SearchTerm` matches `GenericName` (not `MedicineName`). Covers the `x.GenericName != null && x.GenericName.ToLower().Contains(searchTerm)` arm.
9. `Process_HappyPath_SearchTermMatchesManufacturer`
   - `SearchTerm` matches `Manufacturer` (not `MedicineName` or `GenericName`). Covers the `x.Manufacturer != null && x.Manufacturer.ToLower().Contains(searchTerm)` arm.
10. `Process_HappyPath_SearchTermNull_SkipsNameMatching`
    - `SearchTerm = null` exercises `request.SearchTerm?.Trim()?.ToLower()` null-conditional and `string.IsNullOrEmpty(searchTerm)` true branch.
11. `Process_HappyPath_IsActiveTrueFilter`
    - `IsActive = true` exercises `request.IsActive.HasValue` true branch + `x.IsActive == request.IsActive.Value` true branch.
12. `Process_HappyPath_IsActiveFalseFilter`
    - `IsActive = false` exercises `request.IsActive.HasValue` true branch + `x.IsActive == request.IsActive.Value` false branch.
13. `Process_HappyPath_IsActiveNull_SkipsIsActiveFilter`
    - `IsActive = null` exercises `request.IsActive.HasValue` false branch.
14. `Process_HappyPath_PaginationMeta_PopulatedFromRequest`
    - Multi-row dataset with `PageNumber=2`, `PageSize=5` → `Skip(5) Take(5)` → exactly 5 items returned. Confirms `OrderBy` + `Skip` + `Take` chain and `MetaResponse` construction.
15. `Process_HappyPath_MapToResponseDto_AllFieldsCopied`
    - Single medicine with non-null `GenericName`, `Unit`, `DosageForm`, `Concentration`, `Manufacturer`, `Notes`. Verifies every property mapping and the `CreatedAt` `dd/MM/yyyy HH:mm` format string.
16. `Process_HappyPath_MapToResponseDto_NullableFieldsAreNull`
    - Single medicine with `GenericName/Unit/DosageForm/Concentration/Manufacturer/Notes` set to null. Verifies nullable fields map to null and `IsActive=false` is preserved.

Each test must explicitly `Verify` (or `Verify ... Times.Never`) the repository calls it expects to validate both the success and the failure paths.

## Execution plan after approval

1. Create `GetMedicineCatalogServiceTests.cs` in the test folder listed above with the cases above.
2. Run `.\run-tests.bat` from `ECS.Test` to build in Release, run all tests, collect XPlat coverage, and generate the service-only HTML report.
3. Filter the HTML report to `GetMedicineCatalogService` and parse the Cobertura XML to verify the exact line percentage.
4. If line coverage is below 100%, inspect the report for uncovered lines, add the smallest focused test, rerun, and repeat until 100% line coverage is achieved.
5. Report test pass/fail status, exact line coverage, branch coverage as supplemental information, and the absolute `file:///` link to the final HTML report.

## Coverage tooling prerequisite

The test project already references `coverlet.collector`, `MockQueryable.Moq`, xUnit, Moq, and FluentAssertions. The `reportgenerator` tool will be installed automatically by `run-tests.bat` if it is not already present.

## Expected changes

- A new test file `GetMedicineCatalogServiceTests.cs` will be created.
- Production code will remain unchanged unless a coverage run proves a behavior-preserving refactor is necessary and separately approved.

## Approval gate

`request_feedback = true`

Please approve this plan before test execution or test-file changes proceed.
