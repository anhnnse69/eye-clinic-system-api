namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetDetailPatientDemographicsServices
{
    /// <summary>
    /// Request object for getting patient demographics details by patient ID.
    /// </summary>
    public class GetDetailPatientDemographicsRequest
    {
        /// <summary>
        /// Gets or sets the patient profile identifier.
        /// </summary>
        public Guid PatientId { get; set; }
    }
}
