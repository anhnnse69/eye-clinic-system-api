namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientDetailsServices
{
    /// <summary>
    /// Request parameters package capturing targeted patient identifier and active token credentials.
    /// </summary>
    public class ReceptionistGetPatientDetailsRequest
    {
        /// <summary>
        /// Mapped explicitly from validation identity claims on the API routing layer.
        /// </summary>
        public Guid CurrentUserId { get; set; }

        /// <summary>
        /// The structural database reference to the patient record file.
        /// </summary>
        public Guid PatientId { get; set; }
    }
}