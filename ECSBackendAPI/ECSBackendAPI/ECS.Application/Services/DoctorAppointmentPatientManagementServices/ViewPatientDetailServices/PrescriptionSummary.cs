namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDetailServices
{
    /// <summary>
    /// Represents a prescription summary.
    /// </summary>
    public class PrescriptionSummary
    {
        public Guid Id { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PrescriptionItemResponse> Items { get; set; } = [];
    }
}
