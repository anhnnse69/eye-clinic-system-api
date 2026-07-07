using ECS.Domain.Enums;
using System.Text.Json.Serialization;

namespace ECS.Application.Services.DoctorScheduleManagementServices.ClinicShiftServices
{
    /// <summary>
    /// Represents the actual time range of a single shift,
    /// computed from the clinic's OpenTime/CloseTime.
    /// </summary>
    public class ShiftRangeItem
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShiftType ShiftType { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}
