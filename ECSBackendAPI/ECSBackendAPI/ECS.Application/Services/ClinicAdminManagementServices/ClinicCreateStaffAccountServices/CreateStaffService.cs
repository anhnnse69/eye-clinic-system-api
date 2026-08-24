using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices
{
    /// <summary>
    /// Implements specific domain process pipelines to seamlessly on-board administrative personnel matching authorized session tenant.
    /// </summary>
    public class CreateStaffService : ICreateStaffService
    {
        private readonly IRepositoryBaseAsync<User, Guid, AppDbContext> _userRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicQueryRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CreateStaffService(
            IRepositoryBaseAsync<User, Guid, AppDbContext> userRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicQueryRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _staffClinicQueryRepository = staffClinicQueryRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResponse<CreateStaffResponse>> Process(CreateStaffRequest request)
        {
            bool isCurrentAdminValid = true;

            // Step 1: Extract Creator Admin User ID from context
            var creatorAdminUserId = RetrieveAdminUserId(ref isCurrentAdminValid);

            // Step 2: Fetch linked Clinic ID boundary
            var contextClinicId = await RetrieveContextClinicId(creatorAdminUserId, isCurrentAdminValid);

            // Step 3: Verify unique phone availability
            bool isPhoneUnique = await CheckPhoneUniqueness(request.Phone);

            // Step 4: Verify unique email availability
            bool isEmailUnique = await CheckEmailUniqueness(request.Email);

            // Step 5: Verify receptionist count constraint (Max 1 active receptionist per clinic)
            bool isReceptionistLimitValid = await CheckReceptionistLimit(contextClinicId, request.StaffRole);

            // Step 6: Build physical model state definitions
            var targetUserRecord = ConstructUserEntityTree(request, contextClinicId, isCurrentAdminValid, isPhoneUnique, isEmailUnique, isReceptionistLimitValid);

            // Step 7: Persist data graph
            var (committedStaffClinicNode, isExecutionSuccess) = await PersistStaffDataGraph(targetUserRecord, isCurrentAdminValid, isPhoneUnique, isEmailUnique, isReceptionistLimitValid);

            // Step 8: Compile execution state and return response
            return CreateResponse(targetUserRecord, committedStaffClinicNode, isCurrentAdminValid, isPhoneUnique, isEmailUnique, isReceptionistLimitValid, isExecutionSuccess);
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

        private async Task<Guid> RetrieveContextClinicId(Guid adminUserId, bool isCurrentAdminValid)
        {
            if (!isCurrentAdminValid)
            {
                return Guid.Empty;
            }

            var bindingNode = await _staffClinicQueryRepository
                .FindByCondition(link => link.UserId == adminUserId && link.IsActive)
                .FirstOrDefaultAsync();

            if (bindingNode == null)
            {
                return Guid.Empty;
            }

            return bindingNode.ClinicId;
        }

        private async Task<bool> CheckPhoneUniqueness(string testingPhone)
        {
            var recordConflictExists = await _userRepository
                .FindByCondition(account => account.Phone == testingPhone)
                .AnyAsync();

            return !recordConflictExists;
        }

        private async Task<bool> CheckEmailUniqueness(string testingEmail)
        {
            var recordConflictExists = await _userRepository
                .FindByCondition(account => account.Email == testingEmail)
                .AnyAsync();

            return !recordConflictExists;
        }

        /// <summary>
        /// Checks whether the target clinic already has an active receptionist account.
        /// Allows creation if target role is not RECEPTIONIST or if no active RECEPTIONIST exists.
        /// </summary>
        private async Task<bool> CheckReceptionistLimit(Guid targetClinicId, StaffRole targetRole)
        {
            if (targetRole != StaffRole.RECEPTIONIST || targetClinicId == Guid.Empty)
            {
                return true;
            }

            bool activeReceptionistExists = await _staffClinicQueryRepository
                .FindByCondition(link => link.ClinicId == targetClinicId
                                      && link.Role == StaffRole.RECEPTIONIST
                                      && link.IsActive)
                .AnyAsync();

            return !activeReceptionistExists;
        }

        private User? ConstructUserEntityTree(
            CreateStaffRequest dataInput,
            Guid targetClinicId,
            bool adminState,
            bool phoneState,
            bool emailState,
            bool receptionistState)
        {
            if (!adminState || !phoneState || !emailState || !receptionistState || targetClinicId == Guid.Empty)
            {
                return null;
            }

            string computingPasswordHash = BCrypt.Net.BCrypt.HashPassword(dataInput.Password);
            StaffRole parsedStaffRole = dataInput.StaffRole;

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

            var createdUser = new User
            {
                Id = Guid.NewGuid(),
                Phone = dataInput.Phone,
                Email = dataInput.Email,
                PasswordHash = computingPasswordHash,
                FullName = dataInput.FullName,
                Role = mappedUserRole,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            createdUser.StaffClinics = new List<StaffClinic>
            {
                new StaffClinic
                {
                    Id = Guid.NewGuid(),
                    ClinicId = targetClinicId,
                    Role = parsedStaffRole,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            if (parsedStaffRole == StaffRole.DOCTOR)
            {
                createdUser.DoctorProfiles = new List<DoctorProfile>
                {
                    new DoctorProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = createdUser.Id,
                        ClinicId = targetClinicId,
                        SpecialtyId = null,
                        Title = "Bác sĩ",
                        ExperienceYears = 0,
                        Bio = "Thông tin giới thiệu bác sĩ chưa được cập nhật.",
                        IsActive = true,
                        RatingAvg = 0,
                        ReviewCount = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };
            }

            return createdUser;
        }

        private async Task<(StaffClinic? StaffClinicNode, bool IsSuccess)> PersistStaffDataGraph(
            User? userGraph,
            bool adminState,
            bool phoneState,
            bool emailState,
            bool receptionistState)
        {
            if (userGraph == null || !adminState || !phoneState || !emailState || !receptionistState)
            {
                return (null, false);
            }

            using var transactionalScope = await _userRepository.BeginTransactionAsync();
            try
            {
                await _userRepository.CreateAsync(userGraph);
                await _userRepository.SaveChangesAsync();
                await transactionalScope.CommitAsync();

                return (userGraph.StaffClinics!.First(), true);
            }
            catch (Exception)
            {
                await transactionalScope.RollbackAsync();
                return (null, false);
            }
        }

        private ApiResponse<CreateStaffResponse> CreateResponse(
            User? coreUser,
            StaffClinic? operationalNode,
            bool adminState,
            bool phoneState,
            bool emailState,
            bool receptionistState,
            bool successState)
        {
            var functionalErrorsEnvelope = FilterSystemicValidationFailures(adminState, phoneState, emailState, receptionistState, successState);
            if (functionalErrorsEnvelope != null)
            {
                return functionalErrorsEnvelope;
            }

            return ApiResponse<CreateStaffResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                MapToResponse(coreUser!, operationalNode!));
        }

        private ApiResponse<CreateStaffResponse>? FilterSystemicValidationFailures(
            bool adminState,
            bool phoneState,
            bool emailState,
            bool receptionistState,
            bool successState)
        {
            if (!adminState)
            {
                return ApiResponse<CreateStaffResponse>.Fail(GeneralCode.APP_MESSAGE_4014.ToString());
            }
            if (!phoneState)
            {
                return ApiResponse<CreateStaffResponse>.Fail(GeneralCode.APP_MESSAGE_4018.ToString());
            }
            if (!emailState)
            {
                return ApiResponse<CreateStaffResponse>.Fail(GeneralCode.APP_MESSAGE_4017.ToString());
            }
            if (!receptionistState)
            {
                // TODO: Thay APP_MESSAGE_4020 bằng Enum Code tương ứng với lỗi "Phòng khám đã có lễ tân" trong hệ thống của bạn
                return ApiResponse<CreateStaffResponse>.Fail(GeneralCode.APP_MESSAGE_4019.ToString());
            }
            if (!successState)
            {
                return ApiResponse<CreateStaffResponse>.Fail(GeneralCode.APP_MESSAGE_5001.ToString());
            }
            return null;
        }

        private CreateStaffResponse MapToResponse(User accountSource, StaffClinic allocationLink)
        {
            return new CreateStaffResponse
            {
                UserId = accountSource.Id,
                StaffClinicId = allocationLink.Id,
                Phone = accountSource.Phone,
                AssignedRole = allocationLink.Role.ToString()
            };
        }
    }
}