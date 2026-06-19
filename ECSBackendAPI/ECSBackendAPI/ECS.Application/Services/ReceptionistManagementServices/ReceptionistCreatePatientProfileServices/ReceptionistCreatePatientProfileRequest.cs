namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreatePatientProfileServices
{
    /// <summary>
    /// Payload request mapping directly to front-end receptionist patient intake layout.
    /// </summary>
    public class ReceptionistCreatePatientProfileRequest
    {
        /// <summary>
        /// Context flags representing if patient already owns an account.
        /// </summary>
        public bool IsHasAccount { get; set; }

        /// <summary>
        /// Optional target Account ID if linking an existing user.
        /// </summary>
        public Guid? SelectedUserId { get; set; }

        /// <summary>
        /// The full name string matching official administrative credentials.
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
        /// The primary telephone communication sequence line layout serving as cross-domain context identifiers.
        /// </summary>
        public string PhoneNumber { get; set; } = null!;

        /// <summary>
        /// The digital mail routing indicator, required dynamically depending on user linkage instructions.
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