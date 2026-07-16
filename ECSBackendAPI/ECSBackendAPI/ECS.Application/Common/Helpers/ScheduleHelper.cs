using ECS.Application.Services.DoctorScheduleManagementServices.CreateDoctorScheduleService;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Application.Common.Helpers;

/// <summary>
/// Provides common helper methods for schedule management operations.
/// Contains shared logic for shift range calculation, generating time slots,
/// validating schedules, and handling shift operations across different services.
/// </summary>
public static class ScheduleHelper
{
    private const int SlotDurationMinutes = 30;
    private const int DefaultMaxPatients = 1;

    /// <summary>
    /// Defines the ideal time bounds for each shift type.
    /// These are the standard shift hours that clinics typically operate with.
    /// </summary>
    public static readonly Dictionary<ShiftType, (TimeOnly IdealStart, TimeOnly IdealEnd)> ShiftIdealBounds = new()
    {
        [ShiftType.MORNING] = (new TimeOnly(8, 0), new TimeOnly(12, 0)),
        [ShiftType.AFTERNOON] = (new TimeOnly(12, 0), new TimeOnly(17, 0)),
        [ShiftType.EVENING] = (new TimeOnly(17, 0), new TimeOnly(20, 0)),
    };

    private static readonly TimeOnly MorningStart = new(8, 0);
    private static readonly TimeOnly MorningEnd = new(12, 0);
    private static readonly TimeOnly AfternoonStart = new(12, 0);
    private static readonly TimeOnly AfternoonEnd = new(17, 0);
    private static readonly TimeOnly EveningStart = new(17, 0);

    /// <summary>
    /// Builds a dictionary of shift types with their valid time ranges
    /// based on the clinic's operating hours.
    /// </summary>
    /// <param name="openTime">The clinic's opening time.</param>
    /// <param name="closeTime">The clinic's closing time.</param>
    /// <returns>A dictionary mapping shift types to their start and end times.</returns>
    /// <exception cref="ArgumentException">Thrown when close time is not after open time (overnight shifts not supported).</exception>
    public static Dictionary<ShiftType, (TimeOnly Start, TimeOnly End)> BuildShiftRanges(
        TimeOnly openTime,
        TimeOnly closeTime)
    {
        if (closeTime <= openTime)
            throw new ArgumentException(
                $"Giờ đóng cửa phải sau giờ mở cửa. " +
                $"Hiện tại: mở={openTime:HH\\:mm}, đóng={closeTime:HH\\:mm}. " +
                $"Hệ thống không hỗ trợ ca đêm qua đêm.");

        var result = new Dictionary<ShiftType, (TimeOnly, TimeOnly)>();

        AddShift(result, ShiftType.MORNING, MorningStart, MorningEnd, openTime, closeTime);
        AddShift(result, ShiftType.AFTERNOON, AfternoonStart, AfternoonEnd, openTime, closeTime);
        AddShift(result, ShiftType.EVENING, EveningStart, closeTime, openTime, closeTime);

        return result;
    }

    /// <summary>
    /// Adds a shift to the result dictionary if it overlaps with clinic operating hours.
    /// </summary>
    private static void AddShift(
        Dictionary<ShiftType, (TimeOnly Start, TimeOnly End)> result,
        ShiftType shift,
        TimeOnly shiftStart,
        TimeOnly shiftEnd,
        TimeOnly clinicOpen,
        TimeOnly clinicClose)
    {
        var start = Max(shiftStart, clinicOpen);
        var end = Min(shiftEnd, clinicClose);

        if (start < end)
        {
            result[shift] = (start, end);
        }
    }

    /// <summary>
    /// Returns the larger of two TimeOnly values.
    /// </summary>
    private static TimeOnly Max(TimeOnly a, TimeOnly b) => a > b ? a : b;

    /// <summary>
    /// Returns the smaller of two TimeOnly values.
    /// </summary>
    private static TimeOnly Min(TimeOnly a, TimeOnly b) => a < b ? a : b;

    /// <summary>
    /// Generates a list of 30-minute time slots for a given schedule.
    /// </summary>
    /// <param name="scheduleId">The ID of the schedule to associate slots with.</param>
    /// <param name="workDate">The date of the schedule.</param>
    /// <param name="start">The start time of the shift.</param>
    /// <param name="end">The end time of the shift.</param>
    /// <returns>A list of TimeSlot entities ready for persistence.</returns>
    public static List<TimeSlot> GenerateTimeSlots(Guid scheduleId, DateOnly workDate, TimeOnly start, TimeOnly end)
    {
        var slots = new List<TimeSlot>();
        var current = start;

        while (current.AddMinutes(SlotDurationMinutes) <= end)
        {
            var startDateTime = workDate.ToDateTime(current);
            slots.Add(new TimeSlot
            {
                Id = Guid.NewGuid(),
                ScheduleId = scheduleId,
                StartTime = startDateTime,
                EndTime = startDateTime.AddMinutes(SlotDurationMinutes),
                MaxPatients = DefaultMaxPatients,
                CurrentPatients = 0,
                Status = SlotStatus.AVAILABLE,
            });
            current = current.AddMinutes(SlotDurationMinutes);
        }

        return slots;
    }

    /// <summary>
    /// Attempts to clip a shift's ideal time range to fit within a clinic's operating hours.
    /// </summary>
    /// <param name="shiftType">The type of shift to clip.</param>
    /// <param name="clinic">The clinic with open/close times.</param>
    /// <param name="start">The resulting clipped start time.</param>
    /// <param name="end">The resulting clipped end time.</param>
    /// <returns>True if the shift overlaps with clinic hours; otherwise, false.</returns>
    public static bool TryClipShiftToClinicHours(
        ShiftType shiftType,
        Clinic clinic,
        out TimeOnly start,
        out TimeOnly end)
    {
        var (idealStart, idealEnd) = ShiftIdealBounds[shiftType];
        start = idealStart < clinic.OpenTime ? clinic.OpenTime : idealStart;
        end = idealEnd > clinic.CloseTime ? clinic.CloseTime : idealEnd;
        return start < end;
    }

    /// <summary>
    /// Determines the shift type from a given time range.
    /// Used when the shift type needs to be inferred from clipped times.
    /// </summary>
    /// <param name="start">The start time of the shift.</param>
    /// <param name="end">The end time of the shift.</param>
    /// <returns>The detected ShiftType based on overlapping bounds.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no matching shift type is found.</exception>
    public static ShiftType GetShiftTypeFromBounds(TimeOnly start, TimeOnly end)
    {
        foreach (var kv in ShiftIdealBounds)
        {
            var (idealStart, idealEnd) = kv.Value;
            var overlaps = start < idealEnd && end > idealStart;
            if (overlaps) return kv.Key;
        }
        throw new InvalidOperationException("Không xác định được loại ca từ khung giờ đã cắt.");
    }

    /// <summary>
    /// Validates that work dates are not empty and are not in the past.
    /// </summary>
    /// <param name="workDates">List of work dates to validate.</param>
    /// <exception cref="ArgumentException">Thrown when dates are invalid.</exception>
    public static void ValidateWorkDates(List<DateOnly> workDates)
    {
        if (workDates == null || workDates.Count == 0)
            throw new ArgumentException(GeneralCode.APP_MESSAGE_4001.ToString());

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (workDates.Any(d => d < today))
            throw new ArgumentException(GeneralCode.APP_MESSAGE_4001.ToString());
    }

    /// <summary>
    /// Validates that shift types list is not empty.
    /// </summary>
    /// <param name="shiftTypes">List of shift type strings to validate.</param>
    /// <exception cref="ArgumentException">Thrown when shift types list is empty.</exception>
    public static void ValidateShiftTypes(List<string> shiftTypes)
    {
        if (shiftTypes == null || shiftTypes.Count == 0)
            throw new ArgumentException(GeneralCode.APP_MESSAGE_4001.ToString());
    }

    /// <summary>
    /// Parses a list of string shift type values into their corresponding enum values.
    /// </summary>
    /// <param name="rawShiftTypes">List of shift type strings to parse.</param>
    /// <returns>A list of valid ShiftType enum values. Invalid entries are ignored.</returns>
    public static List<ShiftType> ParseShiftTypes(List<string> rawShiftTypes)
    {
        return rawShiftTypes
            .Select(s => Enum.TryParse<ShiftType>(s, true, out var parsed) ? parsed : (ShiftType?)null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Creates a skipped schedule item with a custom reason.
    /// </summary>
    /// <param name="workDate">The work date of the skipped schedule.</param>
    /// <param name="shiftType">The shift type of the skipped schedule.</param>
    /// <param name="reason">The reason for skipping.</param>
    /// <returns>A SkippedScheduleItem instance.</returns>
    public static SkippedScheduleItem CreateSkipItem(DateOnly workDate, ShiftType shiftType, string reason)
    {
        return new SkippedScheduleItem
        {
            WorkDate = workDate,
            ShiftType = shiftType,
            Reason = reason
        };
    }

    /// <summary>
    /// Creates a skipped schedule item for duplicate schedules.
    /// </summary>
    /// <param name="workDate">The work date of the duplicate schedule.</param>
    /// <param name="shiftType">The shift type of the duplicate schedule.</param>
    /// <returns>A SkippedScheduleItem with duplicate reason.</returns>
    public static SkippedScheduleItem CreateDuplicateSkipItem(DateOnly workDate, ShiftType shiftType)
    {
        return CreateSkipItem(workDate, shiftType, "Đã tồn tại ca này trong ngày đã chọn");
    }

    /// <summary>
    /// Creates a skipped schedule item for shifts that fall outside clinic operating hours.
    /// </summary>
    /// <param name="workDate">The work date of the schedule.</param>
    /// <param name="shiftType">The shift type being skipped.</param>
    /// <param name="clinic">The clinic with operating hours.</param>
    /// <returns>A SkippedScheduleItem with outside hours reason.</returns>
    public static SkippedScheduleItem CreateOutsideClinicHoursSkipItem(
        DateOnly workDate,
        ShiftType shiftType,
        Clinic clinic)
    {
        var (s, e) = ShiftIdealBounds[shiftType];
        return CreateSkipItem(
            workDate,
            shiftType,
            $"Ca {s:HH\\:mm}–{e:HH\\:mm} nằm ngoài giờ hoạt động ({clinic.OpenTime:HH\\:mm}–{clinic.CloseTime:HH\\:mm})");
    }

    /// <summary>
    /// Creates a skipped schedule item for room conflicts.
    /// </summary>
    /// <param name="workDate">The work date of the schedule.</param>
    /// <param name="shiftType">The shift type being skipped.</param>
    /// <param name="roomName">The name of the conflicted room.</param>
    /// <returns>A SkippedScheduleItem with room conflict reason.</returns>
    public static SkippedScheduleItem CreateRoomConflictSkipItem(
        DateOnly workDate,
        ShiftType shiftType,
        string roomName)
    {
        return CreateSkipItem(
            workDate,
            shiftType,
            $"Phòng {roomName} đã được bác sĩ khác sử dụng trong ca này");
    }
}