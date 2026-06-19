using ECS.Domain.Enums;
using System.Text.Json.Serialization;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorPersonalScheduleServices
{
    /// <summary>
    /// Request model for retrieving a doctor's personal schedule
    /// based on a specific work date and optional shift type filter.
    /// </summary>
    public class ViewDoctorPersonalScheduleRequest
    {
        public DateOnly WorkDate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShiftType? ShiftType { get; set; }
    }
}
