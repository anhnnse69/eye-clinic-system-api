# Implementation Plan: ClinicShiftService

## 1. Service Overview

**Class**: `ClinicShiftService`
**Interface**: `IClinicShiftService`
**Path**: `ECS.Application/Services/DoctorScheduleManagementServices/ClinicShiftServices/ClinicShiftService.cs`

### Public Method

```csharp
Task<ApiResponse<List<ShiftRangeItem>>> Process(Guid receptionistUserId)
```

### Private Methods (to cover via reflection if needed)

| Method | Lines | Purpose |
|--------|-------|---------|
| `ResolveReceptionistClinicAsync` | 60-73 | Resolve active clinic from receptionist's StaffClinic |
| `BuildShiftRanges` | 80-83 | Call ScheduleHelper.BuildShiftRanges |
| `MapToShiftRangeItems` | 90-102 | Map dictionary to ordered list |
| `CreateSuccessResponse` | 109-114 | Create ApiResponse wrapper |

---

## 2. Dependencies

| Dependency | Type | Purpose |
|------------|------|---------|
| `IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>` | Mock | Query receptionist's clinic assignment |
| `IRepositoryQueryBase<Clinic, Guid, AppDbContext>` | Mock | Query clinic information |

---

## 3. Execution Paths

### Happy Path (TC-CSS-01)
1. `_staffClinicRepo.FindByCondition(...)` returns active `StaffClinic`
2. `_clinicRepo.FindByCondition(...)` returns active `Clinic`
3. `ScheduleHelper.BuildShiftRanges(clinic.OpenTime, clinic.CloseTime)` returns dictionary
4. Map to ordered `List<ShiftRangeItem>`
5. Return `ApiResponse<List<ShiftRangeItem>>.Success(APP_MESSAGE_2000, items)`

### Error Path 1: StaffClinic Not Found (TC-CSS-02)
1. `_staffClinicRepo.FindByCondition(...)` returns empty/null
2. `FirstOrDefaultAsync()` returns null
3. Line 65: `if (staffClinic is null)` → throw `KeyNotFoundException(APP_MESSAGE_4008)`

### Error Path 2: Clinic Not Found (TC-CSS-03)
1. `_staffClinicRepo.FindByCondition(...)` returns active `StaffClinic`
2. `_clinicRepo.FindByCondition(...)` returns empty/null
3. `FirstOrDefaultAsync()` returns null
4. Line 70: `if (clinic is null)` → throw `KeyNotFoundException(APP_MESSAGE_4008)`

---

## 4. Test Cases

| ID | Test Name | Arrange | Act | Assert |
|----|-----------|---------|-----|--------|
| TC-CSS-01 | `Process_ValidReceptionistWithActiveClinic_ReturnsSuccessWithShiftRanges` | Both repos return valid data; clinic with OpenTime=07:30, CloseTime=17:30 | Call `Process(receptionistUserId)` | Code=APP_MESSAGE_2000, Data.Count==3 (Morning/Afternoon/Evening) |
| TC-CSS-02 | `Process_ReceptionistStaffClinicNotFound_ThrowsKeyNotFoundException` | `_staffClinicRepo` returns empty | Call `Process(receptionistUserId)` | Throws `KeyNotFoundException` with message `APP_MESSAGE_4008` |
| TC-CSS-03 | `Process_ClinicNotFoundOrInactive_ThrowsKeyNotFoundException` | `_staffClinicRepo` returns valid, `_clinicRepo` returns empty | Call `Process(receptionistUserId)` | Throws `KeyNotFoundException` with message `APP_MESSAGE_4008` |

---

## 5. ShiftRangeItem DTO Coverage

To achieve 100% line coverage, we must verify the DTO mapping:

### TC-CSS-01 Additional Assertions
- Verify `ShiftType` is set correctly (Morning, Afternoon, Evening)
- Verify `StartTime` and `EndTime` are within clinic operating hours
- Verify items are ordered by `StartTime`

### Special Shift Ranges

| Clinic OpenTime | Clinic CloseTime | Expected Shifts |
|-----------------|-----------------|----------------|
| 08:00 | 20:00 | Morning (08:00-12:00), Afternoon (12:00-17:00), Evening (17:00-20:00) |
| 07:30 | 17:30 | Morning (08:00-12:00), Afternoon (12:00-17:00), Evening (17:00-17:30) |
| 07:00 | 12:00 | Morning (08:00-12:00) only |
| 12:00 | 20:00 | Afternoon (12:00-17:00), Evening (17:00-20:00) |
| 12:00 | 17:00 | Afternoon (12:00-17:00) only |

---

## 6. Private Method Coverage (via Reflection)

### ResolveReceptionistClinicAsync (lines 60-73)
Covered via public `Process()` tests.

### BuildShiftRanges (lines 80-83)
Static method delegates to `ScheduleHelper.BuildShiftRanges`.  
Covered via `Process()` tests by verifying output.

### MapToShiftRangeItems (lines 90-102)
Covered via `Process()` tests by asserting `ShiftRangeItem` properties.

### CreateSuccessResponse (lines 109-114)
Covered via `Process()` tests by asserting `ApiResponse` structure.

---

## 7. Repository Setup Pattern

```csharp
private void SetupStaffClinicRepo(StaffClinic? staffClinic)
{
    var list = staffClinic != null 
        ? new List<StaffClinic> { staffClinic } 
        : new List<StaffClinic>();
    _staffClinicRepoMock
        .Setup(r => r.FindByCondition(
            It.IsAny<Expression<Func<StaffClinic, bool>>>(),
            It.IsAny<bool>()))
        .Returns(list.BuildMockDbSet<StaffClinic>().Object);
}

private void SetupClinicRepo(Clinic? clinic)
{
    var list = clinic != null 
        ? new List<Clinic> { clinic } 
        : new List<Clinic>();
    _clinicRepoMock
        .Setup(r => r.FindByCondition(
            It.IsAny<Expression<Func<Clinic, bool>>>(),
            It.IsAny<bool>()))
        .Returns(list.BuildMockDbSet<Clinic>().Object);
}
```

---

## 8. Expected Line Coverage

| File | Lines | Covered |
|------|-------|---------|
| `ClinicShiftService.cs` | 116 | 100% |
| `ShiftRangeItem.cs` | 17 | 100% |

---

## 9. References

- Example test: `ECS.Test/Services/AuthServices/LoginServiceTests.cs`
- Example mock data: `ECS.Test/MockData/ClinicProfileMockData.cs`
- Similar pattern: `ECS.Test/Services/DoctorScheduleManagementServices/BlockUnblockSlotServices/BlockUnblockSlotServiceTests.cs`
