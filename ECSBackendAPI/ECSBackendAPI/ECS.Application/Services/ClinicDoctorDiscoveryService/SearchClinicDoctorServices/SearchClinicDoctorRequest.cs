namespace ECS.Application.Services.SearchClinicDoctorServices
{
    /// <summary>
    /// Request model for searching clinics and doctors.
    /// </summary>
    public class SearchClinicDoctorRequest
    {
        /// <summary>
        /// Text to search by clinic name or doctor name.
        /// </summary>
        public string? Keyword { get; set; }
    }
}