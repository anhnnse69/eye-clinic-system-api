using ECS.Domain.Enums;
using System.Text.Json.Serialization;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreateDoctorScheduleService
{
    /// <summary>
    /// Response returned after processing doctor schedule creation.
    /// Contains successfully created schedules and skipped schedules.
    /// </summary>
    public class CreateDoctorScheduleResponse
    {
        public List<CreatedScheduleItem> Created { get; set; } = [];
        public List<SkippedScheduleItem> Skipped { get; set; } = [];
    }

    /// <summary>
    /// Represents a schedule that was created successfully.
    /// </summary>
    public class CreatedScheduleItem
    {
        public Guid ScheduleId { get; set; }
        public DateOnly WorkDate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShiftType ShiftType { get; set; }
        public string RoomName { get; set; } = null!;
        public int SlotCount { get; set; }
    }

    /// <summary>
    /// Represents a schedule request that was skipped.
    /// </summary>
    public class SkippedScheduleItem
    {
        public DateOnly WorkDate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShiftType ShiftType { get; set; }
        public string Reason { get; set; } = null!;
    }
}
