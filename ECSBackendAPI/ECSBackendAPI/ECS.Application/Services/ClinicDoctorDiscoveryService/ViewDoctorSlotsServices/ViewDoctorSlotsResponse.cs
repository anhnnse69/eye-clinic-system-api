namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewDoctorSlotsServices
{
    /// <summary>
    /// Response for doctor available slots.
    /// </summary>
    public class ViewDoctorSlotsResponse
    {
        public Guid DoctorId { get; set; }
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? Title { get; set; }
        public string? Specialty { get; set; }
        public string ClinicName { get; set; } = null!;
        public string ClinicAddress { get; set; } = null!;
        public int ExperienceYears { get; set; }
        public string? Bio { get; set; }
        public decimal? RatingAvg { get; set; }
        public int? ReviewCount { get; set; }
        public List<DoctorScheduleDay> ScheduleDays { get; set; } = [];
    }
}
