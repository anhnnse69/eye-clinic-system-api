using System.Text.Json.Serialization;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetMyQueueListServices
{
    /// <summary>
    /// Response DTO for getting my queue list.
    /// </summary>
    public class GetMyQueueListResponse
    {
        public DateOnly Date { get; set; }
        public int TotalPatients { get; set; }
        public int WaitingCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public List<MyQueueItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO representing a queue item for current doctor.
    /// </summary>
    public class MyQueueItemDto
    {
        public Guid QueueId { get; set; }
        public int QueueNumber { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientPhone { get; set; }
        public DateTime? PatientDateOfBirth { get; set; }
        public string? PatientGender { get; set; }
        public DateTime AppointmentTime { get; set; }
        public string? Symptoms { get; set; }
        public Guid? RoomId { get; set; }
        public string? RoomName { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Domain.Enums.QueueStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public DateTime? CalledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool HasMedicalRecord { get; set; }
        public string? ServiceName { get; set; }
        public string BookingSource { get; set; } = string.Empty;
    }
}
