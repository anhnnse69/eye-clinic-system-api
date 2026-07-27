# Implementation Plan: EditServiceService

Target file:
`BE/api/ECSBackendAPI/ECSBackendAPI/ECS.Application/Services/ClinicAdminManagementServices/ClinicEditServiceServices/EditServiceService.cs`

## 1. Required Refactors

**None.** `EditServiceService.Process(...)` already returns
`Task<ApiResponse<EditServiceResponse>>`, making it fully testable. All side
effects (update + save changes) are captured by the `UpdateAsync` and
`SaveChangesAsync` repository calls which can be verified with Moq.

## 2. Testability Audit

| Method                              | Testable? | Notes |
|-------------------------------------|-----------|-------|
| `Process(...)`                      | Yes       | Single public entry point. Returns envelope → straightforward assert on `CodeMessage` and `Data`. |
| `RetrieveUserId(out bool)` (private)| Yes (via reflection) | Needed to cover the `null HttpContext`, missing-claim, and invalid-GUID branches. |
| `RetrieveClinicId(...)` (private)   | Yes (via reflection) | Needed to cover the early return when `isUserValid` is false. |
| `ValidateClinicContext(...)` (private) | Yes (via reflection) | Needed to cover the `clinicId == null` branch. |
| `RetrieveServiceEntity(...)` (private) | Yes (via reflection) | Needed to cover the early return when `isClinicValid` is false. |
| `ValidateServiceExistence(...)` (private) | Yes (via reflection) | Needed to cover the `service == null` branch. |
| `ValidateServiceOwnership(...)` (private) | Yes (via reflection) | Needed to cover the ownership-mismatch branch. |
| `VerifyServiceNameUniqueness(...)` (private) | Yes (via reflection) | Needed to cover the early `false` return and the duplicate-name branch. |
| `ApplyStateChanges(...)` (private)  | Yes (via reflection) | Needed to cover the early-return when `canExecute == false`. |
| `MapToResponseDto(...)` (private)   | Yes (via reflection) | Needed to cover the `service == null` early return. |
| `CreateResponse(...)` (private)     | Indirectly via Process | Both success and error paths are exercised through `Process`. |
| `CreateErrorResponse(...)` (private)| Indirectly via Process | Each error branch is exercised through `Process`. |

## 3. Coverage Strategy

**Line coverage target: 100%** for `EditServiceService.cs`.

The service uses an `out`/`ref` flag pattern (`isUserValid`, `isClinicValid`,
`isServiceExist`, `isOwnershipValid`, `isNameUnique`) that flows through several
private helpers. Covering every private helper is necessary because:

1. Each helper contains branches that are unreachable without first hitting
   the upstream flag setter (e.g., `ValidateClinicContext` only executes when
   `clinicId == null`, which only happens when `RetrieveClinicId` returned null,
   which only happens when `_staffClinicRepository.FindByCondition(...).FirstOrDefaultAsync()`
   returns null).

2. Several branches live inside the early-return helpers
   (`RetrieveServiceEntity`, `VerifyServiceNameUniqueness`, `ApplyStateChanges`,
   `MapToResponseDto`) that are only reachable by forcing the upstream flag to
   be false.

The cleanest approach is to invoke the private helpers via reflection with
`BindingFlags.NonPublic | BindingFlags.Instance`. This makes the test suite
intention-revealing: each `[Method]_[State]_[Outcome]` test targets one specific
branch of one specific private method.

### 3.1 Mocking Conventions

* Use **Moq 4.20.72** for repository and `IHttpContextAccessor`.
* Use **MockQueryable.Moq 10.0.5** to mock `IQueryable<T>` returned by
  `IRepositoryQueryBase.FindByCondition(...)`.
* Use **FluentAssertions 8.8.0** for result assertions.
* Follow the **strict 4-step pattern** (`//Arrange 1`, `//Arrange 2`,
  `//Act`, `//Assert`).

### 3.2 HttpContext setup

`RetrieveUserId` walks `_httpContextAccessor.HttpContext?.User.FindFirst(...).Value`.
Use a helper `SetupClaim(string? value)` that builds a `DefaultHttpContext` with
a `ClaimsPrincipal` containing a single `ClaimTypes.NameIdentifier` claim. To
exercise the null-HttpContext branch, set up
`_httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null)`.

### 3.3 Reflection helper

```csharp
private T Invoke<T>(object target, string methodName, params object[] args)
    => (T)typeof(EditServiceService)
        .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!
        .Invoke(target, args)!;
```

For async methods returning `Task<T>`, await the cast value:
`(Task<T>)mi.Invoke(...)` → `await (Task<T>)...`.

## 4. Test Cases (per method)

### 4.1 `EditServiceService.Process(...)`

| # | Test name | Branch covered |
|---|-----------|----------------|
| 1 | `Process_NullHttpContext_ReturnsAuthenticationError` | `RetrieveUserId` null HttpContext → `isUserValid=false` |
| 2 | `Process_MissingNameIdentifierClaim_ReturnsAuthenticationError` | `RetrieveUserId` claim missing |
| 3 | `Process_InvalidGuidClaim_ReturnsAuthenticationError` | `RetrieveUserId` GUID parse fails |
| 4 | `Process_ValidUserNoActiveClinic_ReturnsClinicNotFoundError` | `RetrieveClinicId` returns null → `isClinicValid=false` |
| 5 | `Process_ValidClinicServiceNotFound_ReturnsServiceNotFoundError` | `RetrieveServiceEntity` returns null → `isServiceExist=false` |
| 6 | `Process_ServiceBelongsToAnotherClinic_ReturnsForbiddenError` | `ValidateServiceOwnership` mismatch → `isOwnershipValid=false` |
| 7 | `Process_DuplicateNameInClinic_ReturnsDuplicateNameError` | `VerifyServiceNameUniqueness` finds conflict → `isNameUnique=false` |
| 8 | `Process_ValidRequest_UpdatesAndReturnsSuccess` | All flags true → `ApplyStateChanges` commits → `CreateResponse` success |
| 9 | `Process_TrimsServiceNameBeforePersisting` | Verifies the `.Trim()` mutation in `ApplyStateChanges` |

### 4.2 `RetrieveUserId(out bool)` (private, via reflection)

| # | Test name | Branch covered |
|---|-----------|----------------|
| 10 | `RetrieveUserId_NullHttpContext_SetsFlagFalse` | null HttpContext |
| 11 | `RetrieveUserId_NullUser_SetsFlagFalse` | null `HttpContext.User` |
| 12 | `RetrieveUserId_NoClaim_SetsFlagFalse` | `FindFirst(...)` returns null |
| 13 | `RetrieveUserId_InvalidGuidClaim_SetsFlagFalse` | `Guid.TryParse` fails |
| 14 | `RetrieveUserId_ValidGuidClaim_ReturnsParsedGuid` | success path |

### 4.3 `RetrieveClinicId(...)` (private, via reflection)

| # | Test name | Branch covered |
|---|-----------|----------------|
| 15 | `RetrieveClinicId_UserInvalid_ReturnsNullWithoutQueryingRepo` | early return on `isUserValid=false` |
| 16 | `RetrieveClinicId_NoStaffRow_ReturnsNull` | repo returns empty |
| 17 | `RetrieveClinicId_StaffRowExists_ReturnsClinicId` | success |

### 4.4 `ValidateClinicContext(...)` (private, via reflection)

| # | Test name | Branch covered |
|---|-----------|----------------|
| 18 | `ValidateClinicContext_NullClinicId_FlagsFalse` | null branch |
| 19 | `ValidateClinicContext_HasClinicId_LeavesFlagTrue` | non-null branch |

### 4.5 `RetrieveServiceEntity(...)` (private, via reflection)

| # | Test name | Branch covered |
|---|-----------|----------------|
| 20 | `RetrieveServiceEntity_ClinicInvalid_ReturnsNullWithoutRepoCall` | early return on `isClinicValid=false` |
| 21 | `RetrieveServiceEntity_ClinicValid_ReturnsRepoResult` | success path |

### 4.6 `ValidateServiceExistence(...)` (private, via reflection)

| # | Test name | Branch covered |
|---|-----------|----------------|
| 22 | `ValidateServiceExistence_NullService_FlagsFalse` | null branch |
| 23 | `ValidateServiceExistence_NonNullService_LeavesFlagTrue` | non-null branch |

### 4.7 `ValidateServiceOwnership(...)` (private, via reflection)

| # | Test name | Branch covered |
|---|-----------|----------------|
| 24 | `ValidateServiceOwnership_ServiceNotExist_FlagsFalse` | `isServiceExist=false` branch |
| 25 | `ValidateServiceOwnership_ClinicMismatch_FlagsFalse` | `service.ClinicId != clinicId` branch |
| 26 | `ValidateServiceOwnership_AllValid_LeavesFlagTrue` | success branch |

### 4.8 `VerifyServiceNameUniqueness(...)` (private, via reflection)

| # | Test name | Branch covered |
|---|-----------|----------------|
| 27 | `VerifyServiceNameUniqueness_OwnershipInvalid_ReturnsFalseWithoutQuery` | early return on `isOwnershipValid=false` |
| 28 | `VerifyServiceNameUniqueness_DuplicateNameFound_ReturnsFalse` | `AnyAsync()` true |
| 29 | `VerifyServiceNameUniqueness_NoConflict_ReturnsTrue` | `AnyAsync()` false |

### 4.9 `ApplyStateChanges(...)` (private, via reflection)

| # | Test name | Branch covered |
|---|-----------|----------------|
| 30 | `ApplyStateChanges_CannotExecute_ReturnsWithoutCommit` | `canExecute=false` early return |
| 31 | `ApplyStateChanges_CanExecute_AppliesAndCommits` | success path: trims name, copies price/duration, calls UpdateAsync + SaveChangesAsync |

### 4.10 `MapToResponseDto(...)` (private, via reflection)

| # | Test name | Branch covered |
|---|-----------|----------------|
| 32 | `MapToResponseDto_NullService_ReturnsNull` | null branch |
| 33 | `MapToResponseDto_NonNullService_MapsAllFields` | mapping branch (including formatted `UpdatedAt`) |

### 4.11 `CreateErrorResponse(...)` + `CreateResponse(...)` (private, via reflection)

`CreateResponse` and `CreateErrorResponse` are fully covered indirectly by the
`Process(...)` tests above:

* Success path → test #8 (`Process_ValidRequest_UpdatesAndReturnsSuccess`)
* `!isUserValid` → tests #1, #2, #3
* `!isClinicValid` → test #4
* `!isServiceExist` → test #5
* `!isOwnershipValid` → test #6
* `!isNameUnique` → test #7
* `errorResponse == null` → test #8

## 5. Files to Create

* `BE/api/ECSBackendAPI/ECSBackendAPI/ECS.Test/Services/ClinicAdminManagementServices/ClinicEditServiceServices/EditServiceServiceTests.cs`
  (single file containing all `[Fact]` methods above, ~33 tests).

## 6. Coverage Validation Steps

1. `dotnet test ECS.Test/ECS.Test.csproj --configuration Release --no-build --collect:"XPlat Code Coverage" --results-directory ECS.Test/TestResults`
2. `reportgenerator "-reports:ECS.Test/TestResults/**/coverage.cobertura.xml" "-targetdir:ECS.Test/TestResults/coverage-report-EditService" "-reporttypes:Html_Dark" "-classfilters:+ECS.Application.Services.ClinicAdminManagementServices.EditServiceServices.EditServiceService*"`
3. Inspect `index.html` → confirm **100% line coverage** for `EditServiceService`.

## 7. Cleanup

After plan approval:
* Move any leftover `implementation_plan_*.md` from project root to `DOC/docs/implement/`.
* No additional refactors required for the production code.