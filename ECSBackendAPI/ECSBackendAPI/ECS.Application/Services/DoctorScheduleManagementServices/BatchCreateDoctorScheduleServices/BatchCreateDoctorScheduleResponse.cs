using ECS.Application.Services.DoctorScheduleManagementServices.CreateDoctorScheduleService;

namespace ECS.Application.Services.DoctorScheduleManagementServices.BatchCreateDoctorScheduleServices
{
    /// <summary>
    /// Represents the response of a batch doctor schedule creation request.
    /// </summary>
    public class BatchCreateDoctorScheduleResponse
    {
        public List<DoctorBatchResult> Results { get; set; } = [];
        public int TotalCreated => Results.Sum(r => r.Created.Count);
        public int TotalSkipped => Results.Sum(r => r.Skipped.Count);
    }

    /// <summary>
    /// Represents the schedule creation result for a doctor.
    /// </summary>
    public class DoctorBatchResult
    {
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = null!;
        public List<CreatedScheduleItem> Created { get; set; } = [];
        public List<SkippedScheduleItem> Skipped { get; set; } = [];
    }
}
