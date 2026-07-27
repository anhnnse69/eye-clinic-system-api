# Implementation Plan — GetClinicRoomsService

**Target**: `ECS.Application.Services.ClinicAdminManagementServices.ClinicViewListRoomServices.GetClinicRoomsService`
**Goal**: 100% line coverage on `GetClinicRoomsService.cs`
**Test file location**: `BE/api/ECSBackendAPI/ECSBackendAPI/ECS.Test/Services/ClinicAdminManagementServices/ClinicViewListRoomServices/GetClinicRoomsServiceTests.cs`

## 1. Service Under Test

`GetClinicRoomsService.Process(GetClinicRoomsRequest request)` resolves a clinic context from the authenticated administrator and returns a paginated list of rooms.

### Public surface
- `Task<ApiResponse<List<GetClinicRoomResponse>>> Process(GetClinicRoomsRequest request)`

### Private helpers (covered through `Process`)
| # | Method | Purpose |
|---|--------|---------|
| 1 | `RetrieveUserId(out bool)` | Parse `ClaimTypes.NameIdentifier` into `Guid`. |
| 2 | `RetrieveClinicId(Guid userId, bool isUserValid)` | Lookup `StaffClinic` row to obtain `ClinicId`. |
| 3 | `BuildFilterExpression(Guid clinicId, GetClinicRoomsRequest request)` | Compose `Where` predicate. |
| 4 | `ExecutePagedQuery(Expression<Func<FacilityRoom, bool>>, GetClinicRoomsRequest)` | Count + Skip/Take. |
| 5 | `MapToResponseDto(List<FacilityRoom>)` | Convert entity → DTO with `RoomType` fallback to `"General"`. |
| 6 | `BuildPaginationMeta(GetClinicRoomsRequest, int)` | Build `MetaResponse`. |
| 7 | `CreateResponse(...)` | Final wrapper, delegates to `CreateErrorResponse`. |
| 8 | `CreateErrorResponse(bool, bool)` | Returns `APP_MESSAGE_4001` or `APP_MESSAGE_4020` or `null`. |

### Dependencies
- `IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext>` — final paged data source.
- `IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>` — staff → clinic lookup.
- `IHttpContextAccessor` — JWT identity extraction.

## 2. Testability Audit

- `Process` returns `Task<ApiResponse<…>>` — easily testable.
- All private helpers are reachable through `Process` therefore no refactoring is required.
- No defensive code to remove.

## 3. Test Cases

Test pattern: `[Method]_[State]_[Outcome]`. All tests target 100% line coverage.

| # | Test name | What it covers |
|---|-----------|----------------|
| T1 | `Process_NullHttpContext_Returns4001AuthenticationError` | `HttpContext` null branch. |
| T2 | `Process_NullHttpContextUser_Returns4001AuthenticationError` | `HttpContext.User` null branch. |
| T3 | `Process_MissingNameIdentifierClaim_Returns4001AuthenticationError` | `FindFirst` returns null. |
| T4 | `Process_InvalidGuidClaim_Returns4001AuthenticationError` | `Guid.TryParse` false branch. |
| T5 | `Process_ValidUserNoActiveClinic_Returns4020ClinicNotFound` | `StaffClinic` query returns null. |
| T6 | `Process_HappyPath_EmptyRoomList_Returns2000WithEmptyData` | success path, empty list, default paging. |
| T7 | `Process_HappyPath_SearchTermTrimsAndLowercases` | `searchTerm` Trim + ToLower arm. |
| T8 | `Process_HappyPath_SearchTermNull_SkipsNameMatching` | `IsNullOrEmpty(searchTerm)` true branch. |
| T9 | `Process_HappyPath_RoomTypeFilter` | `RoomType` filter active. |
| T10 | `Process_HappyPath_RoomTypeNull_SkipsTypeFilter` | `IsNullOrEmpty(typeFilter)` true branch. |
| T11 | `Process_HappyPath_IsActiveTrueFilter` | `IsActive.HasValue = true`, value true. |
| T12 | `Process_HappyPath_IsActiveFalseFilter` | `IsActive.HasValue = true`, value false. |
| T13 | `Process_HappyPath_IsActiveNull_SkipsIsActiveFilter` | `IsActive.HasValue = false`. |
| T14 | `Process_HappyPath_PaginationMeta_PopulatedFromRequest` | `PageNumber=2, PageSize=5`, 20 rows. |
| T15 | `Process_HappyPath_MapToResponseDto_NullRoomType_DefaultsToGeneral` | `MapToResponseDto` fallback to `"General"`. |
| T16 | `Process_HappyPath_MapToResponseDto_PopulatedRoomType_PassedThrough` | `MapToResponseDto` populated path. |

### Framework versions
- xUnit 2.5.3+, Moq 4.20.72+, FluentAssertions 8.8.0+, MockQueryable.Moq 10.0.5+

### Strict 4-step format
Every test must contain literal `//Arrange 1`, `//Arrange 2`, `//Act`, `//Assert` markers.

### Helper factories in the test class
- `SetupHttpContextClaim(string? rawClaimValue)` — wires HttpContext with a single `NameIdentifier` claim.
- `SetupHttpContextUserId(Guid userId)` — overload.
- `SetupStaffClinicRepo(StaffClinic? returnStaffClinic)` — uses `BuildMockDbSet<StaffClinic>()`.
- `SetupRoomRepo(IEnumerable<FacilityRoom> rooms)` — uses `BuildMockDbSet<FacilityRoom>()`.
- `ActiveStaffClinic()` factory and `Room(...)` factory.

## 4. Execution Steps (after approval)

1. Create folder `ECS.Test/Services/ClinicAdminManagementServices/ClinicViewListRoomServices/`.
2. Write `GetClinicRoomsServiceTests.cs` with 16 `[Fact]` tests using the strict 4-step layout.
3. Run `.\run-tests.bat` to build, run tests, generate coverage report.
4. Verify 100% line coverage on `GetClinicRoomsService.cs`.
5. If coverage gap exists, refactor tests (add missing branches) and re-run.
6. Provide the HTML report path.

## 5. Notes / Risks

- The repo previously referenced `ECS.Test/MockData/` but it doesn't exist on disk. Sibling tests (e.g. `GetMedicineCatalogServiceTests`) inline their factories, so I will follow the same pattern for consistency.
- The production code calls `_roomRepository.FindByCondition(...)` UNCONDITIONALLY even on failure paths because `ExecutePagedQuery` is invoked after the `RetrieveClinicId` early-out but before `CreateErrorResponse`. Therefore every test must wire the room repo with `Array.Empty<FacilityRoom>()` so the async provider doesn't throw, just like the medicine catalog sibling tests do.
- `BuildMockDbSet<T>()` returns a `DbSet<T>` mock that implements `IAsyncEnumerable<T>` semantics, so `ToListAsync` / `CountAsync` / `FirstOrDefaultAsync` will work without additional `Provider` setup.
