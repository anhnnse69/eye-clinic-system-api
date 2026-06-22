using System.ComponentModel.DataAnnotations;

namespace ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordDetailServices
{
    /// <summary>
    /// Request object for retrieving a single medical record detail.
    /// UC39 - View Medical Record Detail
    /// </summary>
    public class GetMedicalRecordDetailRequest
    {
        /// <summary>
        /// Medical record ID
        /// </summary>
        [Required(ErrorMessage = "Medical record ID is required")]
        public Guid Id { get; set; }
    }
}
