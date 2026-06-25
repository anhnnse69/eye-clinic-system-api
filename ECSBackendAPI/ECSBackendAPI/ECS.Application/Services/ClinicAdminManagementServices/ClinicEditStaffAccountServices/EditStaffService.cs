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

            // Step 1: Retrieve info of the Admin currently performing the operation
            var creatorAdminUserId = RetrieveAdminUserId(ref isCurrentAdminValid);
            var adminClinicId = await RetrieveContextClinicId(creatorAdminUserId, isCurrentAdminValid);

            // Step 2: Search for the target staff member's user data
            var targetUserRecord = await FetchTargetUserEntity(request.StaffUserId);
            isTargetStaffExist = (targetUserRecord != null);

            // Step 3: Find the staff member's Clinic ID (Uses a new method that is not restricted by the IsActive status)
            var targetClinicId = await RetrieveTargetStaffClinicId(request.StaffUserId, isTargetStaffExist);

            // Step 4: Verify if the target staff member belongs to the same Clinic as the Admin
            ValidateClinicBoundaryRelationship(adminClinicId, targetClinicId, ref isBelongToSameClinic);

            // Step 5: Check uniqueness of Phone and Email
            bool isPhoneUnique = await CheckPhoneUniqueness(request.Phone, request.StaffUserId, isBelongToSameClinic);
            bool isEmailUnique = await CheckEmailUniqueness(request.Email, request.StaffUserId, isBelongToSameClinic);

            // Step 6: Proceed to concurrently update status and authorization permissions
            var (committedStaffClinicNode, isExecutionSuccess) = await MutateAndPersistStaffGraph(
                targetUserRecord,
                request,
                adminClinicId,
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
        /// Retrieves the Clinic ID of the Admin (only accepts currently Active Admins)
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
        /// Retrieves the Clinic ID of the target staff member (Bypasses IsActive filter to allow re-activating locked accounts)
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
        /// Explicitly executes data mutations concurrently across both User and StaffClinic entities.
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

            UserRole currentRole = userGraph.Role;

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

            if (currentRole != UserRole.CLINIC_ADMIN)
            {
                if (mappedUserRole == UserRole.CLINIC_ADMIN)
                {
                    return (null, false); 
                }
            }
            // ------------------------------------------------

            using var transactionalScope = await _userRepository.BeginTransactionAsync();
            try
            {
                // 1. Update User table
                userGraph.Phone = input.Phone;
                userGraph.Email = input.Email;
                userGraph.FullName = input.FullName;
                userGraph.Role = mappedUserRole;
                userGraph.IsActive = input.IsActive;
                userGraph.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(userGraph);

                // 2. Find the staff member's StaffClinic record based on the ClinicId managed by the Admin
                var targetClinicMapping = userGraph.StaffClinics?
                    .FirstOrDefault(sc => sc.ClinicId == adminClinicId);

                if (targetClinicMapping != null)
                {
                    targetClinicMapping.Role = parsedStaffRole;
                    targetClinicMapping.IsActive = input.IsActive;
                    targetClinicMapping.UpdatedAt = DateTime.UtcNow;

                    // Force explicit update using the dedicated Repository
                    await _staffClinicRepository.UpdateAsync(targetClinicMapping);
                }

                // 3. Persist data from both Repositories down to the Database
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