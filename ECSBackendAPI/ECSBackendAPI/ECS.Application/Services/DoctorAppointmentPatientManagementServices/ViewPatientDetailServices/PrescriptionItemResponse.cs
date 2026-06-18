namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDetailServices
{
    /// <summary>
    /// Represents a prescribed medicine item.
    /// </summary>
    public class PrescriptionItemResponse
    {
        public Guid Id { get; set; }
        public string MedicineName { get; set; } = null!;
        public string Dosage { get; set; } = null!;
        public string? Frequency { get; set; }
        public int? DurationDays { get; set; }
        public int Quantity { get; set; }
        public string? Instruction { get; set; }
    }
}
