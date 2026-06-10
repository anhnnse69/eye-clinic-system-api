using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.Clinics
{
    /// <summary>
    /// Clinic registration request from new clinics.
    /// </summary>
    public class ClinicRegistrationRequest : EntityBase<Guid>
    {
        public string ClinicName { get; set; } = null!;
        public string ClinicAddress { get; set; } = null!;
        public string ContactName { get; set; } = null!;
        public string ContactPhone { get; set; } = null!;
        public string ContactEmail { get; set; } = null!;
        public string? BusinessLicenseUrl { get; set; }
        public string Status { get; set; } = "PENDING";
        public Guid? ReviewedBy { get; set; }
        public string? ReviewNote { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }

        public virtual User? Reviewer { get; set; }
    }
}
