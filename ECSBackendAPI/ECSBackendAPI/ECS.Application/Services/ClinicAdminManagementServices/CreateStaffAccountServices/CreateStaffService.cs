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

        /// <summary>
        /// Initializes a new operational instance of <see cref="CreateStaffService"/> mapped with data engine references.
        /// </summary>
        public CreateStaffService(
            IRepositoryBaseAsync<User, Guid, AppDbContext> userRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicQueryRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _staffClinicQueryRepository = staffClinicQueryRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Core orchestration handling transactional logic to provision valid unique clinic staff records without any direct execution routing conditionals.
        /// </summary>
        public async Task<ApiResponse<CreateStaffResponse>> Process(CreateStaffRequest request)
        {
            // Initialize status tracking flags to allow complete process structural tracking flow without using block conditionals
            bool isCurrentAdminValid = true;

            // Step 1: Extract Creator Admin User ID from context via synchronous method
            var creatorAdminUserId = RetrieveAdminUserId(ref isCurrentAdminValid);

            // Step 2: Fetch linked Clinic ID boundary configured against the executing supervisor context session mapping
            var contextClinicId = await RetrieveContextClinicId(creatorAdminUserId, isCurrentAdminValid);

            // Step 3: Verify unique availability properties across standard interaction phone attributes asynchronously
            bool isPhoneUnique = await CheckPhoneUniqueness(request.Phone);

            // Step 4: Verify unique validation status on incoming email information references asynchronously (Email is non-nullable)
            bool isEmailUnique = await CheckEmailUniqueness(request.Email);

            // Step 5: Build physical model state definitions containing structural entities inside context limits
            var targetUserRecord = ConstructUserEntityTree(request, contextClinicId, isCurrentAdminValid, isPhoneUnique, isEmailUnique);

            // Step 6: Persist the combined structural data graph down onto physical repositories utilizing a value Tuple pattern
            var (committedStaffClinicNode, isExecutionSuccess) = await PersistStaffDataGraph(targetUserRecord, isCurrentAdminValid, isPhoneUnique, isEmailUnique);

            // Step 7: Compile the execution state and return transactional tracking structure results
            return CreateResponse(targetUserRecord, committedStaffClinicNode, isCurrentAdminValid, isPhoneUnique, isEmailUnique, isExecutionSuccess);
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

        /// <summary>
        /// Checks system-wide constraints to verify email registration availability.
        /// </summary>
        /// <param name="testingEmail">The mandatory raw email address context target string value being checked.</param>
        /// <returns>True if the email address field configuration is distinct; otherwise, false.</returns>
        private async Task<bool> CheckEmailUniqueness(string testingEmail)
        {
            var recordConflictExists = await _userRepository
                .FindByCondition(account => account.Email == testingEmail)
                .AnyAsync();

            return !recordConflictExists;
        }

        private User? ConstructUserEntityTree(CreateStaffRequest dataInput, Guid targetClinicId, bool adminState, bool phoneState, bool emailState)
        {
            if (!adminState || !phoneState || !emailState || targetClinicId == Guid.Empty)
            {
                return null;
            }
            string computingPasswordHash = BCrypt.Net.BCrypt.HashPassword(dataInput.Password);
            if (!Enum.TryParse<StaffRole>(dataInput.StaffRole.ToString(), true, out var parsedStaffRole))
            {
                return null;
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

            return createdUser;
        }

        private async Task<(StaffClinic? StaffClinicNode, bool IsSuccess)> PersistStaffDataGraph(
            User? userGraph,
            bool adminState,
            bool phoneState,
            bool emailState)
        {
            if (userGraph == null || !adminState || !phoneState || !emailState)
            {
                return (null, false);
            }

            using var transactionalScope = await _userRepository.BeginTransactionAsync();
            try
            {
                await _userRepository.CreateAsync(userGraph);
                await _userRepository.SaveChangesAsync();
                await transactionalScope.CommitAsync();

                return (userGraph.StaffClinics.First(), true);
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
            bool successState)
        {
            var functionalErrorsEnvelope = FilterSystemicValidationFailures(adminState, phoneState, emailState, successState);
            if (functionalErrorsEnvelope != null)
            {
                return functionalErrorsEnvelope;
            }

            return ApiResponse<CreateStaffResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                MapToResponse(coreUser!, operationalNode!));
        }

        private ApiResponse<CreateStaffResponse>? FilterSystemicValidationFailures(bool adminState, bool phoneState, bool emailState, bool successState)
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