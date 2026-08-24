namespace ECS.Application.Services.RecordApprovalServices;

public class CreateRecordApprovalRequest
{
    public string RecordId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string PermissionDoc { get; set; } = string.Empty;
    public string? AttachedFileName { get; set; }
}
