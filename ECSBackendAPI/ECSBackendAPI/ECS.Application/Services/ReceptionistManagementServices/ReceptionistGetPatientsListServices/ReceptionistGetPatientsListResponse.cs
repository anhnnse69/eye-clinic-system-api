namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientsListServices
{
    /// <summary>
    /// Data transfer object representing a row within the paginated patient records data layout.
    /// </summary>
    public class ReceptionistGetPatientsListResponse
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Gender { get; set; } = null!; // Returns standard enumeration codes: "MALE", "FEMALE", "OTHER"
        public string Dob { get; set; } = null!; // Date format pattern: "yyyy-MM-dd" for UI splitting layouts
        public string? IdentityNumber { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BhytNumber { get; set; }
        public string? BloodType { get; set; }
        public string CreatedAt { get; set; } = null!; // Standard ISO UTC format timestamp representation string
    }
}