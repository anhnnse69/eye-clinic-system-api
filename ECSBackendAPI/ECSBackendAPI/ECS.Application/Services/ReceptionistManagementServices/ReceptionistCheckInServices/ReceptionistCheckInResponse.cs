namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCheckInServices
{
    /// <summary>
    /// Core structural response packet detailing verified arrival state records and sequence markers optimized for live viewport updates.
    /// </summary>
    public class ReceptionistCheckInResponse
    {
        /// <summary>
        /// The unique identifier reference string mapping the processed backend appointment record.
        /// </summary>
        public string AppointmentId { get; set; } = null!;

        /// <summary>
        /// The dynamic execution milestone indicator string standardized to uppercase layouts.
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// The real-time internal sequence progression tracker showing the patient's line location context.
        /// </summary>
        public QueueInlineRowDto Queue { get; set; } = null!;
    }

    /// <summary>
    /// Inline dynamic arrangement layout tracker carrying telemetry tokens for sequential waiting structures.
    /// </summary>
    public class QueueInlineRowDto
    {
        /// <summary>
        /// The individual sequence tracking key mapping the operational queue line entry database record.
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// The localized numeric integer detailing the precise order index within the assigned clinical room grid.
        /// </summary>
        public int QueueNumber { get; set; }

        /// <summary>
        /// The active transition state token mapping line statuses like WAITING, CALLING, or COMPLETED.
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// The system coordination token timestamp pointing to when the row context was pulled forward.
        /// </summary>
        public string? CalledAt { get; set; }
    }
}
