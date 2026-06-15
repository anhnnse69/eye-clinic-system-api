namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewDoctorSlotsServices
{
    /// <summary>
    /// An individual bookable time slot.
    /// </summary>
    public class DoctorTimeSlot
    {
        public Guid SlotId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int MaxPatients { get; set; }
        public int CurrentPatients { get; set; }
        public int Remaining { get; set; }
        public string Status { get; set; } = null!;
    }
}
