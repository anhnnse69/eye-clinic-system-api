namespace ECS.Application.Services.SearchClinicDoctorServices
{
    /// <summary>
    /// Combined search result containing matched clinics and doctors.
    /// </summary>
    public class SearchClinicDoctorResponse
    {
        public List<ClinicSearchItem> Clinics { get; set; } = [];
        public List<DoctorSearchItem> Doctors { get; set; } = [];
    }
}