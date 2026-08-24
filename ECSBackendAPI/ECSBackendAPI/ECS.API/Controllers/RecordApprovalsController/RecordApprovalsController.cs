using ECS.Application.Common.Response;
using ECS.Application.Services.RecordApprovalServices;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.RecordApprovalsController
{
    /// <summary>
    /// Handles medical record edit request approvals stored in MongoDB.
    /// </summary>
    [ApiController]
    [Route("api/v1/record-approvals")]
    public class RecordApprovalsController : ControllerBase
    {
        private readonly IRecordApprovalService _approvalService;

        public RecordApprovalsController(IRecordApprovalService approvalService)
        {
            _approvalService = approvalService;
        }

        /// <summary>
        /// Doctor submits an edit approval request for a medical record.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRecordApprovalRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.RecordId))
            {
                return BadRequest(ApiResponse<RecordApprovalResponseDto>.Fail("RecordId is required."));
            }

            var result = await _approvalService.CreateRequestAsync(request, ct);
            return Ok(ApiResponse<RecordApprovalResponseDto>.Success("Tạo đơn đề nghị phê duyệt thành công.", result));
        }

        /// <summary>
        /// Clinic Admin gets list of record approval requests (filtered by status or search keyword).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRequests([FromQuery] string? status, [FromQuery] string? search, CancellationToken ct)
        {
            var result = await _approvalService.GetRequestsAsync(status, search, ct);
            return Ok(ApiResponse<List<RecordApprovalResponseDto>>.Success("Lấy danh sách đơn phê duyệt thành công.", result));
        }

        /// <summary>
        /// Get detail of record approval request by RecordId.
        /// </summary>
        [HttpGet("{recordId}")]
        public async Task<IActionResult> GetByRecordId(string recordId, CancellationToken ct)
        {
            var result = await _approvalService.GetByRecordIdAsync(recordId, ct);
            if (result == null)
            {
                return NotFound(ApiResponse<RecordApprovalResponseDto>.Fail("Không tìm thấy đơn phê duyệt cho bệnh án này."));
            }
            return Ok(ApiResponse<RecordApprovalResponseDto>.Success("Lấy chi tiết đơn phê duyệt thành công.", result));
        }

        /// <summary>
        /// Clinic Admin approves the edit request.
        /// </summary>
        [HttpPost("{recordId}/approve")]
        public async Task<IActionResult> ApproveRequest(string recordId, CancellationToken ct)
        {
            var result = await _approvalService.ApproveRequestAsync(recordId, "ClinicAdmin", ct);
            if (result == null)
            {
                return NotFound(ApiResponse<RecordApprovalResponseDto>.Fail("Không tìm thấy đơn phê duyệt để duyệt."));
            }
            return Ok(ApiResponse<RecordApprovalResponseDto>.Success("Đã phê duyệt cấp quyền thành công.", result));
        }

        /// <summary>
        /// Clinic Admin rejects the edit request.
        /// </summary>
        [HttpPost("{recordId}/reject")]
        public async Task<IActionResult> RejectRequest(string recordId, [FromBody] RejectRecordApprovalPayload? payload, CancellationToken ct)
        {
            var result = await _approvalService.RejectRequestAsync(recordId, "ClinicAdmin", payload?.Note, ct);
            if (result == null)
            {
                return NotFound(ApiResponse<RecordApprovalResponseDto>.Fail("Không tìm thấy đơn phê duyệt để từ chối."));
            }
            return Ok(ApiResponse<RecordApprovalResponseDto>.Success("Đã từ chối đơn đề nghị.", result));
        }

        /// <summary>
        /// Check if a medical record edit request has been approved.
        /// </summary>
        [HttpGet("check-permission/{recordId}")]
        public async Task<IActionResult> CheckPermission(string recordId, CancellationToken ct)
        {
            var isApproved = await _approvalService.CheckPermissionAsync(recordId, ct);
            return Ok(ApiResponse<bool>.Success(isApproved ? "Bệnh án đã được phê duyệt." : "Chưa được phê duyệt.", isApproved));
        }

        /// <summary>
        /// Reset approval status for a medical record after doctor has updated it.
        /// </summary>
        [HttpPost("{recordId}/reset")]
        public async Task<IActionResult> ResetApproval(string recordId, CancellationToken ct)
        {
            var result = await _approvalService.ResetApprovalAsync(recordId, ct);
            return Ok(ApiResponse<bool>.Success(result ? "Đã reset quyền phê duyệt." : "Không có đơn phê duyệt để reset.", result));
        }
    }

    public class RejectRecordApprovalPayload
    {
        public string? Note { get; set; }
    }
}
