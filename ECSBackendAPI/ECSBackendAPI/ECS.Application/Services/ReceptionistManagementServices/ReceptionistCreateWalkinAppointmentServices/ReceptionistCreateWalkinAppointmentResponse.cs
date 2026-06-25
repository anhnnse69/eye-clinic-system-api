namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreateWalkinAppointmentServices
{
    /// <summary>
    /// Data transfer item contract providing transaction logs mapping the finalized room allocation sequences.
    /// </summary>
    public class ReceptionistCreateWalkinAppointmentResponse
    {
        public string AppointmentId { get; set; } = null!;
        public string Status { get; set; } = null!;
        public QueueInlineRowDto WalkInQueue { get; set; } = null!;
    }

    /// <summary>
    /// Nested child item data transfer structure tracking sequential queue arrays and real-time tracking properties.
    /// </summary>
    public class QueueInlineRowDto
    {
        public string Id { get; set; } = null!;
        public int QueueNumber { get; set; }
        public string Status { get; set; } = null!;
        public string? CalledAt { get; set; }
    }
}
