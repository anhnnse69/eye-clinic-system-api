namespace ECS.Application.Services.RecordApprovalServices;

public class RecordApprovalResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string PermissionDoc { get; set; } = string.Empty;
    public string? AttachedFileName { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNote { get; set; }
}
