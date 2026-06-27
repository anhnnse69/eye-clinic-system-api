namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CompleteQueueServices
{
    /// <summary>
    /// Response object for completing a queue item.
    /// </summary>
    public class CompleteQueueResponse
    {
        /// <summary>
        /// The completed queue ID.
        /// </summary>
        public string QueueId { get; set; } = string.Empty;

        /// <summary>
        /// The appointment ID associated with the queue.
        /// </summary>
        public string AppointmentId { get; set; } = string.Empty;

        /// <summary>
        /// Patient name.
        /// </summary>
        public string? PatientName { get; set; }

        /// <summary>
        /// Queue number.
        /// </summary>
        public int QueueNumber { get; set; }

        /// <summary>
        /// Previous queue status.
        /// </summary>
        public string PreviousStatus { get; set; } = string.Empty;

        /// <summary>
        /// Completion timestamp.
        /// </summary>
        public string CompletedAt { get; set; } = string.Empty;

        /// <summary>
        /// Success flag.
        /// </summary>
        public bool IsSuccess { get; set; }
    }
}
