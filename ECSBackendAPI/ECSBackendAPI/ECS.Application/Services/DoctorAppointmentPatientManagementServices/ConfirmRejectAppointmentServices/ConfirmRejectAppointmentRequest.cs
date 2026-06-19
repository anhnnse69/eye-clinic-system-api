using System.Text.Json.Serialization;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ConfirmRejectAppointmentServices
{
    /// <summary>
    /// Request model for confirming or rejecting an appointment.
    /// </summary>
    public class ConfirmRejectAppointmentRequest
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AppointmentDecision Decision { get; set; }
        public string? RejectReason { get; set; }
    }

    /// <summary>
    /// Decision options for appointment processing.
    /// </summary>
    public enum AppointmentDecision
    {
        CONFIRM,
        REJECT,
    }
}
