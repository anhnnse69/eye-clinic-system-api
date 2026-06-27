namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CompleteQueueServices
{
    /// <summary>
    /// Request object for completing a queue item.
    /// </summary>
    public class CompleteQueueRequest
    {
        /// <summary>
        /// The queue ID to mark as completed.
        /// </summary>
        public string QueueId { get; set; } = string.Empty;
    }
}
