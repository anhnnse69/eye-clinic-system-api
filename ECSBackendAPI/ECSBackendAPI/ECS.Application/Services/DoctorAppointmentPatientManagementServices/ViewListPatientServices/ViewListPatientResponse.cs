namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewListPatientServices
{
    /// <summary>
    /// Represents the paginated patient list response
    /// for a doctor.
    /// </summary>
    public class ViewListPatientResponse
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public List<PatientAppointmentItem> Patients { get; set; } = [];
    }
}
