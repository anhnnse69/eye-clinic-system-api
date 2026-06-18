namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistUpdatePatientProfileServices
{
    /// <summary>
    /// Payload request mapping directly to front-end administrative editing layouts.
    /// </summary>
    public class ReceptionistUpdatePatientProfileRequest
    {
        /// <summary>
        /// The full name string matching administrative records.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// The structural representation gender code matching layout definitions. Expected: "MALE", "FEMALE", "OTHER".
        /// </summary>
        public string Gender { get; set; } = null!;

        /// <summary>
        /// The string expression of patient date of birth formatted strictly as "yyyy-MM-dd".
        /// </summary>
        public string Dob { get; set; } = null!;

        /// <summary>
        /// The primary telephone communication sequence line layout.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Kept in payload if synchronization to root system User details is needed.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// The regional localization living placement details.
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// National structural identity allocation record mappings.
        /// </summary>
        public string? IdentityNumber { get; set; }

        /// <summary>
        /// Social healthcare coverage registration block markers.
        /// </summary>
        public string? BhytNumber { get; set; }
    }
}
