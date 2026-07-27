# Implementation Plan: BlockUnblockSlotService

## 1. Service Overview

**Service**: `BlockUnblockSlotService`
**Namespace**: `ECS.Application.Services.DoctorScheduleManagementServices.BlockUnblockSlotServices`
**File**: `BE/api/ECSBackendAPI/ECSBackendAPI/ECS.Application/Services/DoctorScheduleManagementServices/BlockUnblockSlotServices/BlockUnblockSlotService.cs`

### Purpose
Service responsible for toggling a specific doctor schedule time slot between `AVAILABLE` and `BLOCKED` states. Enforces clinic cross-boundaries and locks adjustments if a slot is already booked by a patient.

## 2. Dependencies

| Dependency | Type | Purpose |
|-----------|------|---------|
| `IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>` | Mock | Verify receptionist identities and clinic mapping boundaries |
| `IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>` | Mock | Query and validate active doctor profiles |
| `AppDbContext` | Real (In-Memory) | Track and save changes (used for `Include(s => s.Schedule)`) |

## 3. Public Method: `Process(...)`

```csharp
Task<ApiResponse<string>> Process(
    Guid receptionistUserId,
    Guid doctorId,
    Guid slotId,
    BlockUnblockSlotRequest request)
```

### Execution Flow

```
1. ResolveReceptionistClinicIdAsync(receptionistUserId)
   └── FindByCondition(sc => sc.UserId == receptionistUserId && sc.IsActive)
       └── [KeyNotFoundException: APP_MESSAGE_4008] if staffClinic is null

2. ResolveActiveDoctorProfileAsync(doctorId)
   └── FindByCondition(d => d.Id == doctorId && d.IsActive)
       └── [KeyNotFoundException: APP_MESSAGE_4011] if doctorProfile is null

3. EnsureSameClinic(receptionistClinicId, doctorProfile.ClinicId)
   └── [UnauthorizedAccessException: APP_MESSAGE_4008] if clinic mismatch

4. ResolveDoctorTimeSlotAsync(slotId, doctorProfile.Id)
   └── _dbContext.Set<TimeSlot>().Include(s => s.Schedule)
       .FirstOrDefaultAsync(s => s.Id == slotId && s.Schedule.DoctorId == doctorId)
       └── [KeyNotFoundException: APP_MESSAGE_4004] if slot is null

5. EnsureSlotNotBooked(slot)
   └── [InvalidOperationException: APP_MESSAGE_4009] if slot.Status == BOOKED

6. UpdateSlotStatus(slot, request.Block)
   └── slot.Status = request.Block ? SlotStatus.BLOCKED : SlotStatus.AVAILABLE

7. _dbContext.SaveChangesAsync()

8. CreateSuccessResponse(slot.Status)
   └── return ApiResponse<string>.Success(APP_MESSAGE_2000, status.ToString())
```

## 4. Private Methods (to cover via Reflection)

| Method | Visibility | Purpose |
|--------|------------|---------|
| `ResolveReceptionistClinicIdAsync` | private async | Resolves receptionist's clinic ID |
| `ResolveActiveDoctorProfileAsync` | private async | Resolves active doctor profile |
| `EnsureSameClinic` | private static | Validates clinic cross-boundaries |
| `ResolveDoctorTimeSlotAsync` | private async | Resolves and validates time slot |
| `EnsureSlotNotBooked` | private static | Validates slot is not booked |
| `UpdateSlotStatus` | private static | Updates slot status |
| `CreateSuccessResponse` | private static | Creates success response |

## 5. Required Test Cases (11 tests for 100% coverage)

### TC-BUS-01: Receptionist StaffClinic Not Found
- **Setup**: `_staffClinicRepo.FindByCondition` returns `null`
- **Act**: Call `Process(...)`
- **Assert**: Throws `KeyNotFoundException` with message `APP_MESSAGE_4008`

### TC-BUS-02: Receptionist StaffClinic Inactive (filtered at query level)
- **Setup**: `_staffClinicRepo.FindByCondition` returns empty (inactive filtered)
- **Act**: Call `Process(...)`
- **Assert**: Throws `KeyNotFoundException` with message `APP_MESSAGE_4008`

### TC-BUS-03: Doctor Profile Not Found
- **Setup**: `_doctorRepo.FindByCondition` returns `null`
- **Act**: Call `Process(...)`
- **Assert**: Throws `KeyNotFoundException` with message `APP_MESSAGE_4011`

### TC-BUS-04: Doctor Profile Inactive (filtered at query level)
- **Setup**: `_doctorRepo.FindByCondition` returns empty (inactive filtered)
- **Act**: Call `Process(...)`
- **Assert**: Throws `KeyNotFoundException` with message `APP_MESSAGE_4011`

### TC-BUS-05: Clinic Cross-Boundary Violation
- **Setup**: Receptionist belongs to Clinic A, Doctor belongs to Clinic B
- **Act**: Call `Process(...)`
- **Assert**: Throws `UnauthorizedAccessException` with message `APP_MESSAGE_4008`

### TC-BUS-06: Time Slot Not Found
- **Setup**: `_dbContext.TimeSlots` is empty (slot not found)
- **Act**: Call `Process(...)`
- **Assert**: Throws `KeyNotFoundException` with message `APP_MESSAGE_4004`

### TC-BUS-07: Time Slot Belongs to Different Doctor
- **Setup**: Slot exists but belongs to different doctor
- **Act**: Call `Process(...)`
- **Assert**: Throws `KeyNotFoundException` with message `APP_MESSAGE_4004`

### TC-BUS-08: Time Slot Already Booked
- **Setup**: Slot exists with `Status = SlotStatus.BOOKED`
- **Act**: Call `Process(...)`
- **Assert**: Throws `InvalidOperationException` with message `APP_MESSAGE_4009`

### TC-BUS-09: Happy Path - Block Available Slot
- **Setup**: Valid receptionist, doctor, slot (AVAILABLE), same clinic
- **Input**: `request.Block = true`
- **Act**: Call `Process(...)`
- **Assert**:
  - `result.CodeMessage == APP_MESSAGE_2000`
  - `result.Data == "BLOCKED"`
  - `_dbContext.SaveChangesAsync()` called once
  - Slot status updated to `BLOCKED`

### TC-BUS-10: Happy Path - Unblock Blocked Slot
- **Setup**: Valid receptionist, doctor, slot (BLOCKED), same clinic
- **Input**: `request.Block = false`
- **Act**: Call `Process(...)`
- **Assert**:
  - `result.CodeMessage == APP_MESSAGE_2000`
  - `result.Data == "AVAILABLE"`
  - Slot status updated to `AVAILABLE`

### TC-BUS-11: Verify SaveChangesAsync Called
- **Setup**: Happy path with AVAILABLE slot
- **Input**: `request.Block = true`
- **Act**: Call `Process(...)`
- **Assert**: `_dbContext.SaveChangesAsync()` is called exactly once

## 6. MockData Required

Create `BlockUnblockSlotMockData.cs` in `ECS.Test/MockData/`:

```csharp
public static class BlockUnblockSlotMockData
{
    // Receptionist User ID
    public static readonly Guid ReceptionistUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Clinic IDs
    public static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid OtherClinicId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    // Doctor IDs
    public static readonly Guid DoctorProfileId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid DoctorUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    // Slot ID
    public static readonly Guid SlotId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    // Schedule ID
    public static readonly Guid ScheduleId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    public static StaffClinic GetActiveStaffClinic(
        Guid? clinicId = null,
        Guid? userId = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId ?? ReceptionistUserId,
        ClinicId = clinicId ?? ClinicId,
        Role = StaffRole.RECEPTIONIST,
        IsActive = true
    };

    public static DoctorProfile GetActiveDoctorProfile(
        Guid? clinicId = null,
        Guid? id = null,
        bool isActive = true) => new()
    {
        Id = id ?? DoctorProfileId,
        UserId = DoctorUserId,
        ClinicId = clinicId ?? ClinicId,
        IsActive = isActive,
        User = new User
        {
            Id = DoctorUserId,
            FullName = "Dr. Smith",
            Email = "dr.smith@example.com",
            IsActive = true
        }
    };

    public static TimeSlot GetAvailableSlot(
        Guid? doctorId = null,
        Guid? scheduleId = null) => new()
    {
        Id = SlotId,
        ScheduleId = scheduleId ?? ScheduleId,
        Status = SlotStatus.AVAILABLE,
        StartTime = DateTime.UtcNow.AddHours(1),
        EndTime = DateTime.UtcNow.AddHours(2),
        Schedule = new DoctorSchedule
        {
            Id = scheduleId ?? ScheduleId,
            DoctorId = doctorId ?? DoctorProfileId,
            WorkDate = DateTime.UtcNow.Date,
            ShiftType = ShiftType.MORNING
        }
    };

    public static TimeSlot GetBlockedSlot(
        Guid? doctorId = null,
        Guid? scheduleId = null) => new()
    {
        Id = SlotId,
        ScheduleId = scheduleId ?? ScheduleId,
        Status = SlotStatus.BLOCKED,
        StartTime = DateTime.UtcNow.AddHours(1),
        EndTime = DateTime.UtcNow.AddHours(2),
        Schedule = new DoctorSchedule
        {
            Id = scheduleId ?? ScheduleId,
            DoctorId = doctorId ?? DoctorProfileId,
            WorkDate = DateTime.UtcNow.Date,
            ShiftType = ShiftType.MORNING
        }
    };

    public static TimeSlot GetBookedSlot(
        Guid? doctorId = null,
        Guid? scheduleId = null) => new()
    {
        Id = SlotId,
        ScheduleId = scheduleId ?? ScheduleId,
        Status = SlotStatus.BOOKED,
        StartTime = DateTime.UtcNow.AddHours(1),
        EndTime = DateTime.UtcNow.AddHours(2),
        Schedule = new DoctorSchedule
        {
            Id = scheduleId ?? ScheduleId,
            DoctorId = doctorId ?? DoctorProfileId,
            WorkDate = DateTime.UtcNow.Date,
            ShiftType = ShiftType.MORNING
        }
    };

    public static BlockUnblockSlotRequest GetBlockRequest() =>
        new() { Block = true };

    public static BlockUnblockSlotRequest GetUnblockRequest() =>
        new() { Block = false };
}
```

## 7. Test File Location

```
ECS.Test/
└── Services/
    └── DoctorScheduleManagementServices/
        └── BlockUnblockSlotServices/
            └── BlockUnblockSlotServiceTests.cs
```

## 8. Testability Assessment

**Score**: 8/10 (Good)

### Strengths
- All dependencies are injectable via constructor
- Clear error handling with specific exception types
- Synchronous validation helpers are easily testable

### Challenges
- `_dbContext` uses `Include(s => s.Schedule)` - requires real in-memory DbContext
- Private async methods need reflection for unit testing

### Recommended Approach
- Use **In-Memory DbContext** for tests that exercise `ResolveDoctorTimeSlotAsync`
- Use **Reflection** for testing private helper methods
- Mock `IRepositoryQueryBase<T>` for repository tests

## 9. Line Coverage Targets

| Method | Lines | Target |
|--------|-------|--------|
| `Process` | 14 | 100% |
| `ResolveReceptionistClinicIdAsync` | 7 | 100% |
| `ResolveActiveDoctorProfileAsync` | 7 | 100% |
| `EnsureSameClinic` | 4 | 100% |
| `ResolveDoctorTimeSlotAsync` | 9 | 100% |
| `EnsureSlotNotBooked` | 4 | 100% |
| `UpdateSlotStatus` | 3 | 100% |
| `CreateSuccessResponse` | 5 | 100% |
| **Total** | **53** | **100%** |
