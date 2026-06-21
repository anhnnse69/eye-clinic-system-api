using ECS.Domain.Enums;
using System.Text.Json.Serialization;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorPersonalScheduleServices
{
    /// <summary>
    /// Response containing the doctor's schedule
    /// for a specific work date.
    /// </summary>
    public class ViewDoctorPersonalScheduleResponse
    {
        public DateOnly WorkDate { get; set; }
        public List<ScheduleShiftItem> Shifts { get; set; } = [];
    }

    /// <summary>
    /// Represents a work shift in the doctor's schedule.
    /// </summary>
    public class ScheduleShiftItem
    {
        public Guid ScheduleId { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShiftType ShiftType { get; set; }
        public string? Note { get; set; }
        public Guid? RoomId { get; set; }
        public string? RoomName { get; set; }
        public List<ScheduleSlotItem> Slots { get; set; } = [];
    }

    /// <summary>
    /// Represents a schedule time slot.
    /// </summary>
    public class ScheduleSlotItem
    {
        public Guid SlotId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int MaxPatients { get; set; }
        public int CurrentPatients { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SlotStatus Status { get; set; }
        public List<SlotAppointmentItem> Appointments { get; set; } = [];
    }

    /// <summary>
    /// Represents an appointment within a schedule slot.
    /// </summary>
    public class SlotAppointmentItem
    {
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = null!;
        public string? PatientPhone { get; set; }
        public string Status { get; set; } = null!;
        public string? Symptoms { get; set; }
    }
}
