using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicAdminManagementServices.EditStaffAccountServices
{
    /// <summary>
    /// Coordinates internal persistent datastore transactional updates to modify existing staff profiles safely under security scope rules.
    /// </summary>
    public class EditStaffService : IEditStaffService
    {
        private readonly IRepositoryBaseAsync<User, Guid, AppDbContext> _userRepository;
        private readonly IRepositoryBaseAsync<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new operational instance of <see cref="EditStaffService"/> mapped with data engine references.
        /// </summary>
        public EditStaffService(
            IRepositoryBaseAsync<User, Guid, AppDbContext> userRepository,
            IRepositoryBaseAsync<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Core orchestration handling transactional logic to modify valid clinic staff records without any direct execution routing conditionals.
        /// </summary>
        public async Task<ApiResponse<EditStaffResponse>> Process(EditStaffRequest request)
        {
            bool isCurrentAdminValid = true;
            bool isTargetStaffExist = true;
            bool isBelongToSameClinic = true;

            // Bước 1: Lấy thông tin Admin đang thao tác
            var creatorAdminUserId = RetrieveAdminUserId(ref isCurrentAdminValid);
            var adminClinicId = await RetrieveContextClinicId(creatorAdminUserId, isCurrentAdminValid);

            // Bước 2: Tìm kiếm dữ liệu nhân viên mục tiêu
            var targetUserRecord = await FetchTargetUserEntity(request.StaffUserId);
            isTargetStaffExist = (targetUserRecord != null);

            // Bước 3: Tìm Clinic ID của nhân viên (Dùng hàm mới không bị chặn bởi trạng thái IsActive)
            var targetClinicId = await RetrieveTargetStaffClinicId(request.StaffUserId, isTargetStaffExist);

            // Bước 4: Kiểm tra xem nhân viên mục tiêu có thuộc cùng Clinic với Admin không
            ValidateClinicBoundaryRelationship(adminClinicId, targetClinicId, ref isBelongToSameClinic);

            // Bước 5: Kiểm tra tính duy nhất của Phone và Email
            bool isPhoneUnique = await CheckPhoneUniqueness(request.Phone, request.StaffUserId, isBelongToSameClinic);
            bool isEmailUnique = await CheckEmailUniqueness(request.Email, request.StaffUserId, isBelongToSameClinic);

            // Bước 6: Tiến hành cập nhật đồng thời trạng thái và phân quyền
            var (committedStaffClinicNode, isExecutionSuccess) = await MutateAndPersistStaffGraph(
                targetUserRecord,
                request,
                adminClinicId, // Truyền adminClinicId vào để định vị chính xác bản ghi StaffClinic cần sửa
                isCurrentAdminValid && isTargetStaffExist && isBelongToSameClinic && isPhoneUnique && isEmailUnique
            );

            return CreateResponse(targetUserRecord, committedStaffClinicNode, isCurrentAdminValid, isTargetStaffExist, isBelongToSameClinic, isPhoneUnique, isEmailUnique, isExecutionSuccess);
        }

        private Guid RetrieveAdminUserId(ref bool isCurrentAdminValid)
        {
            var principalIdValue = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(principalIdValue, out var parsedAdminId))
            {
                isCurrentAdminValid = false;
                return Guid.Empty;
            }
            return parsedAdminId;
        }

        /// <summary>
        /// Hàm lấy Clinic ID của Admin (chỉ chấp nhận Admin đang Active)
        /// </summary>
        private async Task<Guid> RetrieveContextClinicId(Guid userId, bool isPreConditionValid)
        {
            if (!isPreConditionValid || userId == Guid.Empty)
            {
                return Guid.Empty;
            }

            var bindingNode = await _staffClinicRepository
                .FindByCondition(link => link.UserId == userId && link.IsActive)
                .FirstOrDefaultAsync();

            return bindingNode?.ClinicId ?? Guid.Empty;
        }

        /// <summary>
        /// Hàm lấy Clinic ID của Nhân viên mục tiêu (Bỏ lọc IsActive để có thể kích hoạt lại tài khoản đang bị Khóa)
        /// </summary>
        private async Task<Guid> RetrieveTargetStaffClinicId(Guid staffUserId, bool isPreConditionValid)
        {
            if (!isPreConditionValid || staffUserId == Guid.Empty)
            {
                return Guid.Empty;
            }

            var bindingNode = await _staffClinicRepository
                .FindByCondition(link => link.UserId == staffUserId)
                .OrderByDescending(link => link.UpdatedAt)
                .FirstOrDefaultAsync();

            return bindingNode?.ClinicId ?? Guid.Empty;
        }

        private async Task<User?> FetchTargetUserEntity(Guid staffUserId)
        {
            if (staffUserId == Guid.Empty)
            {
                return null;
            }

            return await _userRepository
                .FindByCondition(x => x.Id == staffUserId)
                .Include(x => x.StaffClinics)
                .FirstOrDefaultAsync();
        }

        private void ValidateClinicBoundaryRelationship(Guid adminClinicId, Guid targetClinicId, ref bool isBelongToSameClinic)
        {
            if (adminClinicId == Guid.Empty || targetClinicId == Guid.Empty || adminClinicId != targetClinicId)
            {
                isBelongToSameClinic = false;
            }
        }

        private async Task<bool> CheckPhoneUniqueness(string testingPhone, Guid currentStaffUserId, bool isPreConditionValid)
        {
            if (!isPreConditionValid) return false;

            var recordConflictExists = await _userRepository
                .FindByCondition(account => account.Phone == testingPhone && account.Id != currentStaffUserId)
                .AnyAsync();

            return !recordConflictExists;
        }

        private async Task<bool> CheckEmailUniqueness(string testingEmail, Guid currentStaffUserId, bool isPreConditionValid)
        {
            if (!isPreConditionValid) return false;

            var recordConflictExists = await _userRepository
                .FindByCondition(account => account.Email == testingEmail && account.Id != currentStaffUserId)
                .AnyAsync();

            return !recordConflictExists;
        }

        /// <summary>
        /// Thực hiện thay đổi dữ liệu đồng thời trên cả 2 thực thể User và StaffClinic một cách tường minh.
        /// </summary>
        private async Task<(StaffClinic? StaffClinicNode, bool IsSuccess)> MutateAndPersistStaffGraph(
            User? userGraph,
            EditStaffRequest input,
            Guid adminClinicId,
            bool canCommit)
        {
            if (!canCommit || userGraph == null)
            {
                return (null, false);
            }

            if (!Enum.TryParse<StaffRole>(input.StaffRole.ToString(), true, out var parsedStaffRole))
            {
                return (null, false);
            }

            UserRole mappedUserRole = UserRole.RECEPTIONIST;
            switch (parsedStaffRole)
            {
                case StaffRole.DOCTOR:
                    mappedUserRole = UserRole.DOCTOR;
                    break;
                case StaffRole.CLINIC_ADMIN:
                    mappedUserRole = UserRole.CLINIC_ADMIN;
                    break;
                case StaffRole.RECEPTIONIST:
                    mappedUserRole = UserRole.RECEPTIONIST;
                    break;
            }

            using var transactionalScope = await _userRepository.BeginTransactionAsync();
            try
            {
                // 1. Cập nhật bảng User
                userGraph.Phone = input.Phone;
                userGraph.Email = input.Email;
                userGraph.FullName = input.FullName;
                userGraph.Role = mappedUserRole;
                userGraph.IsActive = input.IsActive; // Cập nhật trạng thái User từ Request
                userGraph.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(userGraph);

                // 2. Tìm kiếm bản ghi StaffClinic của nhân viên dựa theo ClinicId mà Admin quản lý
                var targetClinicMapping = userGraph.StaffClinics?
                    .FirstOrDefault(sc => sc.ClinicId == adminClinicId);

                if (targetClinicMapping != null)
                {
                    targetClinicMapping.Role = parsedStaffRole;
                    targetClinicMapping.IsActive = input.IsActive; // Cập nhật trạng thái liên kết từ Request
                    targetClinicMapping.UpdatedAt = DateTime.UtcNow;

                    // Ép cập nhật tường minh bằng Repository riêng biệt
                    await _staffClinicRepository.UpdateAsync(targetClinicMapping);
                }

                // 3. Thực thi lưu dữ liệu của cả 2 Repository xuống Database
                await _userRepository.SaveChangesAsync();
                await _staffClinicRepository.SaveChangesAsync();

                await transactionalScope.CommitAsync();

                return (targetClinicMapping, true);
            }
            catch (Exception)
            {
                await _userRepository.RollbackTransactionAsync();
                return (null, false);
            }
        }

        private ApiResponse<EditStaffResponse> CreateResponse(
            User? coreUser,
            StaffClinic? operationalNode,
            bool adminState,
            bool staffExistState,
            bool clinicBoundaryState,
            bool phoneState,
            bool emailState,
            bool successState)
        {
            var functionalErrorsEnvelope = FilterSystemicValidationFailures(adminState, staffExistState, clinicBoundaryState, phoneState, emailState, successState);
            if (functionalErrorsEnvelope != null)
            {
                return functionalErrorsEnvelope;
            }

            return ApiResponse<EditStaffResponse>.Success(
                GeneralCode.APP_MESSAGE_2006.ToString(),
                MapToResponse(coreUser!, operationalNode!));
        }

        private ApiResponse<EditStaffResponse>? FilterSystemicValidationFailures(
            bool adminState,
            bool staffExistState,
            bool clinicBoundaryState,
            bool phoneState,
            bool emailState,
            bool successState)
        {
            if (!adminState) return ApiResponse<EditStaffResponse>.Fail(GeneralCode.APP_MESSAGE_4014.ToString());
            if (!staffExistState || !clinicBoundaryState) return ApiResponse<EditStaffResponse>.Fail(GeneralCode.APP_MESSAGE_4020.ToString());
            if (!phoneState) return ApiResponse<EditStaffResponse>.Fail(GeneralCode.APP_MESSAGE_4018.ToString());
            if (!emailState) return ApiResponse<EditStaffResponse>.Fail(GeneralCode.APP_MESSAGE_4017.ToString());
            if (!successState) return ApiResponse<EditStaffResponse>.Fail(GeneralCode.APP_MESSAGE_5001.ToString());
            return null;
        }

        private EditStaffResponse MapToResponse(User accountSource, StaffClinic allocationLink)
        {
            return new EditStaffResponse
            {
                UserId = accountSource.Id,
                Phone = accountSource.Phone,
                Email = accountSource.Email,
                FullName = accountSource.FullName,
                IsActive = accountSource.IsActive,
                UpdatedRole = allocationLink.Role.ToString()
            };
        }
    }
}