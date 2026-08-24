namespace ECS.Application.Services.RecordApprovalServices;

public interface IRecordApprovalService
{
    Task<RecordApprovalResponseDto> CreateRequestAsync(CreateRecordApprovalRequest request, CancellationToken ct = default);
    Task<List<RecordApprovalResponseDto>> GetRequestsAsync(string? status = null, string? search = null, CancellationToken ct = default);
    Task<RecordApprovalResponseDto?> GetByRecordIdAsync(string recordId, CancellationToken ct = default);
    Task<RecordApprovalResponseDto?> ApproveRequestAsync(string recordId, string? reviewerId = null, CancellationToken ct = default);
    Task<RecordApprovalResponseDto?> RejectRequestAsync(string recordId, string? reviewerId = null, string? reviewNote = null, CancellationToken ct = default);
    Task<bool> CheckPermissionAsync(string recordId, CancellationToken ct = default);
    Task<bool> ResetApprovalAsync(string recordId, CancellationToken ct = default);
}
