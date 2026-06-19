using ECS.Domain.Enums;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetDailyAppointmentsServices
{
    /// <summary>
    /// Request criteria parameters packet tracking target scheduling dates, criteria, and filters boundaries.
    /// </summary>
    public class ReceptionistGetDailyAppointmentsRequest
    {
        /// <summary>
        /// Mapped explicitly from identity authorization claims at routing entry layers.
        /// </summary>
        public Guid CurrentUserId { get; set; }

        /// <summary>
        /// Targeted operation date constraint context. Defaults to DateTime.UtcNow if empty.
        /// </summary>
        public DateTime? TargetDate { get; set; }

        /// <summary>
        /// Optional enumeration criteria tracking MORNING, AFTERNOON, or EVENING shifts blocks.
        /// </summary>
        public ShiftType? ShiftFilter { get; set; }

        /// <summary>
        /// Substring keywords targeting Patient profiles fullNames or Phone numbers.
        /// </summary>
        public string? SearchPatient { get; set; }

        /// <summary>
        /// Substring keyword targeting assigned Physician fullname sequences.
        /// </summary>
        public string? SearchDoctor { get; set; }

        /// <summary>
        /// Target page row pointer index segment.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Evaluation capacity boundaries bounding items returned per grid array.
        /// </summary>
        public int PageSize { get; set; } = 5;
    }
}