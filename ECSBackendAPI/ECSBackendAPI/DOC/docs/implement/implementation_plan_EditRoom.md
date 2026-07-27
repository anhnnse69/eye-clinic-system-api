# Implementation Plan: ClinicEditRoomServices Unit Tests

## Scope

Generate xUnit unit tests for `EditRoomService` (1 public method: `Process`) targeting **100% line coverage** and ≥90% branch coverage.

**Source file:** `ECS.Application/Services/ClinicAdminManagementServices/ClinicEditRoomServices/EditRoomService.cs`

**Interface:** `IEditRoomService.Process(EditRoomRequest request)` → `Task<ApiResponse<EditRoomResponse>>`

## Public Method Behavior

`Process` orchestrates an 8-step workflow using private helper methods:

1. `RetrieveUserId(out isUserValid)` — parses `ClaimTypes.NameIdentifier` from `HttpContext`. On failure → `isUserValid=false`.
2. `RetrieveClinicId(userId, isUserValid)` — looks up active `StaffClinic`. Returns `Guid? clinicId`.
3. `FetchRoomData(roomId, clinicId)` — fetches `FacilityRoom` by id and clinicId. Returns `FacilityRoom?`.
4. `ValidateRoomExistence(originalRoom, ref isRoomExist)` — sets `isRoomExist=false` if `originalRoom == null`.
5. `VerifyUniqueRoomName(roomName, roomId, clinicId, isRoomExist)` — checks for duplicate room name (case-insensitive, trimmed) excluding current room id. Returns `bool isNameUnique`.
6. `ApplyRoomUpdates(room, request, isRoomExist, isNameUnique)` — updates properties (`RoomName.Trim()`, `RoomType`) and persists via `UpdateAsync` + `SaveChangesAsync`.
7. `MapToResponseDto(room)` — projects to `EditRoomResponse` (Id, ClinicId, RoomName, RoomType, IsActive).
8. `CreateResponse(updatedRoom, isUserValid, isRoomExist, isNameUnique)` — maps state flags to `ApiResponse<T>` via `CreateErrorResponse`.

## Error Code Mapping (from `CreateErrorResponse`)

| Flag | Code | Meaning |
|---|---|---|
| `!isUserValid` | `APP_MESSAGE_4001` | Authentication failure / invalid user id |
| `!isRoomExist` | `APP_MESSAGE_4020` | Room not found in clinic |
| `!isNameUnique` | `APP_MESSAGE_4019` | Room name duplication |
| All true | `APP_MESSAGE_2000` | Success |

## Testability Audit (Step 2)

The implementation is testable as-is — no refactor required.

| Concern | Status |
|---|---|
| `private` helpers — `RetrieveUserId`, `RetrieveClinicId`, `FetchRoomData`, `ValidateRoomExistence`, `VerifyUniqueRoomName`, `ApplyRoomUpdates`, `MapToResponseDto`, `CreateResponse`, `CreateErrorResponse` | ✅ Testable indirectly through `Process`; no reflection needed |
| `_httpContextAccessor.HttpContext?.User.FindFirst(...)?.Value` null-conditional chain | ✅ Both null branches covered by separate tests |
| `Guid.TryParse(claim, out userId)` true/false branches | ✅ Cover both with valid and invalid Guid strings |
| `EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(...)` and `AnyAsync(...)` for `StaffClinic` and `FacilityRoom` | ✅ Mock via `MockQueryable.Moq` |
| `_facilityRoomRepository.UpdateAsync` / `SaveChangesAsync` | ✅ Mock via `Mock<IRepositoryBaseAsync<...>>` |

## Files to Create

| File | Purpose |
|---|---|
| `ECS.Test/MockData/EditRoomMockData.cs` | Reusable mock entities, request factories, deterministic IDs |
| `ECS.Test/Services/ClinicAdminManagementServices/ClinicEditRoomServices/EditRoomServiceTests.cs` | `[Fact]` test cases following strict 4-step pattern |

## Test Plan — 8 Test Cases

| # | Test Name | Branch Coverage | Mocks |
|---|-----------|-----------------|-------|
| TC-ER-01 | `Process_NullHttpContext_Returns4001AuthenticationError` | `RetrieveUserId` early `null-conditional` (`_httpContextAccessor.HttpContext == null`) | `HttpContext → null` |
| TC-ER-02 | `Process_MissingNameIdentifierClaim_Returns4001AuthenticationError` | `RetrieveUserId` `User.FindFirst → null` branch | `HttpContext → principal without NameIdentifier claim` |
| TC-ER-03 | `Process_InvalidUserIdClaim_Returns4001AuthenticationError` | `RetrieveUserId` `Guid.TryParse` returns false branch | claim value `"not-a-guid"` |
| TC-ER-04 | `Process_ValidUserWithoutActiveClinic_Returns4020RoomNotFoundError` | `RetrieveClinicId` `staffClinic == null` branch → `FetchRoomData` `clinicId null` short-circuit → `ValidateRoomExistence` `originalRoom == null` → `isRoomExist = false` → `VerifyUniqueRoomName` `!isRoomExist` short-circuit → `ApplyRoomUpdates` early return → `CreateErrorResponse` `!isRoomExist → APP_MESSAGE_4020` | staffClinic repo empty; room repo not queried |
| TC-ER-05 | `Process_RoomNotFoundInClinic_Returns4020RoomNotFoundError` | `RetrieveClinicId` returns valid clinicId → `FetchRoomData` `FirstOrDefaultAsync → null` → `ValidateRoomExistence` false branch → `VerifyUniqueRoomName` `!isRoomExist` short-circuit → `ApplyRoomUpdates` early return → `CreateErrorResponse` `!isRoomExist → APP_MESSAGE_4020` | staffClinic OK; room repo empty |
| TC-ER-06 | `Process_DuplicateRoomName_Returns4019DuplicateNameError` | `VerifyUniqueRoomName` duplicate branch → `ApplyRoomUpdates` `!isNameUnique` early return → `CreateErrorResponse` `!isNameUnique → APP_MESSAGE_4019` | staffClinic OK; room OK; second FindByCondition returns true (duplicate exists) |
| TC-ER-07 | `Process_ValidRequest_ReturnsSuccessWithUpdatedRoom` | Happy path: `ApplyRoomUpdates` success branch → `UpdateAsync` + `SaveChangesAsync` invoked → `MapToResponseDto` populates DTO → `CreateResponse` `success` branch → `APP_MESSAGE_2000`; DTO fields (Id, ClinicId, RoomName, RoomType, IsActive) asserted | staffClinic OK; room OK; second FindByCondition returns false (no duplicate) |
| TC-ER-08 | `Process_ValidRequestCaseInsensitiveDuplicateName_Returns4019DuplicateNameError` | `VerifyUniqueRoomName` case-insensitive duplicate branch | staffClinic OK; room OK; second FindByCondition returns true (case-insensitive duplicate) |

### Branch Coverage Notes

- **`RetrieveUserId`** has 4 distinct paths:
  - `_httpContextAccessor.HttpContext == null` → TC-ER-01
  - `HttpContext.User` non-null but `FindFirst → null` → TC-ER-02
  - `FindFirst` returns a claim but `Guid.TryParse` fails → TC-ER-03
  - `Guid.TryParse` succeeds → covered by TC-ER-04..08
- **`RetrieveClinicId`** branches:
  - `isUserValid == false` short-circuit → covered by TC-ER-01/02/03
  - `staffClinic == null` → TC-ER-04
  - `staffClinic != null` → TC-ER-05/06/07/08
- **`FetchRoomData`** branches:
  - `clinicId.HasValue == false` short-circuit → TC-ER-04
  - `FirstOrDefaultAsync → null` → TC-ER-05
  - `FirstOrDefaultAsync → room` → TC-ER-06/07/08
- **`ValidateRoomExistence`** branches:
  - `originalRoom == null` → TC-ER-04/05
  - `originalRoom != null` (no change) → TC-ER-06/07/08
- **`VerifyUniqueRoomName`** branches:
  - `!isRoomExist || !clinicId.HasValue` short-circuit → TC-ER-04/05
  - `AnyAsync → true` (duplicate) → TC-ER-06/08
  - `AnyAsync → false` (unique) → TC-ER-07
- **`ApplyRoomUpdates`** branches:
  - `!isRoomExist || !isNameUnique || room == null` early return → TC-ER-04/05/06
  - Full update path → TC-ER-07/08
- **`CreateErrorResponse`** branches:
  - `!isUserValid` → TC-ER-01/02/03
  - `!isRoomExist` → TC-ER-04/05
  - `!isNameUnique` → TC-ER-06/08
  - All valid (returns null) → TC-ER-07
- **`CreateResponse`** branches:
  - `errorResponse != null` (error branch) → TC-ER-01..06/08
  - `errorResponse == null` (success branch with `MapToResponseDto(updatedRoom!)`) → TC-ER-07
- **`MapToResponseDto`** mapping all 5 fields → TC-ER-07 (asserts all DTO fields)

### Repository Call Sequencing Challenge

`EditRoomService.Process` calls `_facilityRoomRepository.FindByCondition` **twice** with different predicates:
1. `FetchRoomData` — uses `x.Id == roomId && x.ClinicId == clinicId.Value` (expects single `FirstOrDefaultAsync`)
2. `VerifyUniqueRoomName` — uses `x.ClinicId == ... && x.Id != roomId && ...` (expects `AnyAsync`)

To mock these distinct queries, we use a **sequence-based mock setup**: the first call returns the original room (or null), and the second call returns whether duplicates exist.

```csharp
private void SetupFacilityRoomRepoSequence(FacilityRoom? originalRoom, bool hasDuplicates)
{
    var originalRoomList = originalRoom != null
        ? new List<FacilityRoom> { originalRoom }
        : new List<FacilityRoom>();
    var duplicateList = hasDuplicates
        ? new List<FacilityRoom> { new FacilityRoom { Id = Guid.NewGuid(), ClinicId = TestClinicId, RoomName = "X" } }
        : new List<FacilityRoom>();

    var sequence = new Queue<Mock<DbSet<FacilityRoom>>>();
    sequence.Enqueue(originalRoomList.BuildMockDbSet<FacilityRoom>());
    sequence.Enqueue(duplicateList.BuildMockDbSet<FacilityRoom>());

    _roomRepoMock
        .Setup(r => r.FindByCondition(
            It.IsAny<Expression<Func<FacilityRoom, bool>>>(),
            It.IsAny<bool>()))
        .ReturnsUsing(sequence.Dequeue, () => /* queue ref */);
}
```

**Pragmatic simplification:** Since `MockQueryable.Moq` provides an `IQueryable<FacilityRoom>`, both `FirstOrDefaultAsync` and `AnyAsync` can be called on the **same** mocked queryable. The repository mock returns a queryable that has the original room (so `FirstOrDefaultAsync` finds it) but ALSO has a duplicate match (so `AnyAsync` returns true).

**Simpler approach — use a single mock queryable that contains both the original room and a separate duplicate:**

- If we want `FirstOrDefaultAsync` to return the original room and `AnyAsync` (with `x.Id != roomId` predicate) to return true, we need the duplicate query to be evaluated differently from the fetch query.

Since both FindByCondition calls are independent and return separate `IQueryable<FacilityRoom>` instances, we must set up a **two-call sequence**. We use `MockSequence`:

```csharp
var seq = new MockSequence();
_roomRepoMock.InSequence(seq).Setup(r => r.FindByCondition(/* fetch predicate */, ...))
    .Returns(originalRoomQueryable);
_roomRepoMock.InSequence(seq).Setup(r => r.FindByCondition(/* dup predicate */, ...))
    .Returns(duplicateQueryable);
```

However, because Moq's `It.IsAny<Expression<...>>()` would match both setups, we need to either:
- Use `.Returns()` overrides via callback matching, OR
- Use a **setup that varies by queryable enumeration** (the same mock returns same queryable, but the test logic arranges data so `FirstOrDefaultAsync` returns the original room while `AnyAsync` (with proper predicate) also returns true when duplicates exist).

**Final approach:** The test for the duplicate-name case sets up the room repo to return a list containing the original room PLUS a duplicate (different Id, same normalized name). Since `FetchRoomData` uses `FirstOrDefaultAsync` on `x.Id == roomId && x.ClinicId == clinicId.Value`, it returns the original. Since `VerifyUniqueRoomName` uses `AnyAsync` on `x.ClinicId == ... && x.Id != roomId && x.RoomName.Trim().ToLower() == normalizedName`, it returns true because the duplicate has a different Id and same name.

This works **with a single mock queryable setup** because both predicates are evaluated against the same in-memory queryable backed by `MockQueryable.Moq`.

### Strict 4-Step Pattern Applied

Every test will include literal `//Arrange 1`, `//Arrange 2`, `//Act`, and `//Assert` comments.

### Mock Setup Helpers

```csharp
private void SetupHttpContextClaim(string? rawClaimValue)
private void SetupHttpContextUserId(Guid userId)
private void SetupStaffClinicRepo(StaffClinic? returnStaffClinic)
private void SetupFacilityRoomRepo(IList<FacilityRoom> rooms)   // shared queryable for both calls
```

## Coverage Validation Workflow

1. `cd` to `ECS.Test`, run `run-tests.bat`
2. Open `TestResults/coverage-report/index.html`
3. Filter for class `ECS.Application.Services.ClinicAdminManagementServices.ClinicEditRoomServices.EditRoomService`
4. Confirm 100% line coverage and ≥90% branch coverage
5. Iterate on tests if uncovered lines remain

## Cleanup

After plan approval, any `implementation_plan*.md` files in `BE\api\ECSBackendAPI\ECSBackendAPI\ECS.Test\` (project root) will be moved to this `DOC\docs\implement\` folder.