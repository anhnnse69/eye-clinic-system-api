using ECS.Domain.Enums;
using System.Text.Json.Serialization;

namespace ECS.Application.Services.DoctorScheduleManagementServices.EditDoctorScheduleServices
{
    /// <summary>
    /// Response returned after updating a doctor schedule.
    /// </summary>
    public class EditDoctorScheduleResponse
    {
        public Guid ScheduleId { get; set; }
        public DateOnly WorkDate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShiftType ShiftType { get; set; }
        public string RoomName { get; set; } = null!;
        public DateTime UpdatedAt { get; set; }
    }
}
