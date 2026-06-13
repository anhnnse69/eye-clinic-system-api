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
        /// <param name="userRepository">Repository handle dealing with read-write capabilities on User entities.</param>
        /// <param name="staffClinicQueryRepository">Repository handle querying links mapping staff to clinic spaces.</param>
        /// <param name="httpContextAccessor">Accessor system abstraction providing entry points to active user headers.</param>
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
        /// <param name="request">The detailed registration application parameters metadata transfer container.</param>
        /// <returns>An encapsulation envelope enclosing operation results status details.</returns>
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

            // Step 4: Verify unique validation status on incoming email information references asynchronously
            bool isEmailUnique = await CheckEmailUniqueness(request.Email);

            // Step 5: Build physical model state definitions containing structural entities inside context limits
            var targetUserRecord = ConstructUserEntityTree(request, contextClinicId, isCurrentAdminValid, isPhoneUnique, isEmailUnique);

            // Step 6: Persist the combined structural data graph down onto physical repositories utilizing a value Tuple pattern
            var (committedStaffClinicNode, isExecutionSuccess) = await PersistStaffDataGraph(targetUserRecord, isCurrentAdminValid, isPhoneUnique, isEmailUnique);

            // Step 7: Compile the execution state and return transactional tracking structure results
            return CreateResponse(targetUserRecord, committedStaffClinicNode, isCurrentAdminValid, isPhoneUnique, isEmailUnique, isExecutionSuccess);
        }

        /// <summary>
        /// Resolves claims structures inside requests wrappers onto concrete identification indicators safely.
        /// </summary>
        /// <param name="isCurrentAdminValid">A monitoring reference state updated to false if claims parsing steps drop out.</param>
        /// <returns>A safe parsed structural token indicator matching target context users.</returns>
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
        /// Queries persistence spaces to fetch identification limits governing current clinic instances mapped onto executing admins.
        /// </summary>
        /// <param name="adminUserId">The physical identity primary indicator associated with creator sessions.</param>
        /// <param name="isCurrentAdminValid">Pre-condition processing flag metrics stating whether lookup operations can proceed.</param>
        /// <returns>A tracking key target pointing back at operational boundaries if resolved; otherwise empty.</returns>
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

        /// <summary>
        /// Determines whether targeted customer verification phones are unmapped inside persistence frameworks systemwide.
        /// </summary>
        /// <param name="testingPhone">The literal contact verification attribute tracking tag string.</param>
        /// <returns>A true boolean state indicator if records map out cleanly with zero conflicts.</returns>
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

        /// <summary>
        /// Maps domain logic configurations to build structured storage model hierarchies cleanly.
        /// </summary>
        /// <param name="dataInput">The direct structural initialization entity metrics reference parameters block.</param>
        /// <param name="targetClinicId">The unique business operating clinic namespace tracking identity.</param>
        /// <param name="adminState">State evaluation flag parameter recording current user session validity status.</param>
        /// <param name="phoneState">State evaluation flag parameter indicating phone uniqueness verification success.</param>
        /// <param name="emailState">State evaluation flag parameter capturing system email duplication diagnostics state.</param>
        /// <returns>An entity tree structure object holding context state maps; otherwise null if inputs error out.</returns>
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

        /// <summary>
        /// Handles isolated low-level persistent storage commands wrapping mutations into explicit unit blocks safely.
        /// </summary>
        /// <param name="userGraph">The memory tracking entity map schema reference intended for database persistence.</param>
        /// <param name="adminState">State metrics validating operational authorization permissions details.</param>
        /// <param name="phoneState">Status identifier confirming identity number structure verification.</param>
        /// <param name="emailState">Diagnostic parameter tracking target authentication mailbox integrity tags.</param>
        /// <returns>A structural state tuple wrapping operation status alongside populated references if executed.</returns>
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

        /// <summary>
        /// Transforms processing contexts into appropriate application response payloads tracking logical runtime statuses.
        /// </summary>
        /// <param name="coreUser">The domain entity representation tracking data records values.</param>
        /// <param name="operationalNode">The linking entity component establishing workspace membership contexts.</param>
        /// <param name="adminState">The execution tracking context flag monitoring requestor verification.</param>
        /// <param name="phoneState">The execution tracking context flag checking telephone collisions status.</param>
        /// <param name="emailState">The execution tracking context flag capturing registry name duplications indicators.</param>
        /// <param name="successState">The tracking transactional validation evaluation framework response result state.</param>
        /// <returns>A configured standard API system transaction tracking envelope container.</returns>
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

        /// <summary>
        /// Analyzes runtime flags status metrics to convert issues directly into clear system response error structures.
        /// </summary>
        /// <param name="adminState">Flag state specifying whether supervisor credentials map out cleanly.</param>
        /// <param name="phoneState">Flag monitoring collision occurrences across corporate storage channels.</param>
        /// <param name="emailState">Flag identifying matching duplicate mailbox markers across application frameworks.</param>
        /// <param name="successState">State tracing metric measuring database unit boundary commitments actions.</param>
        /// <returns>A populated bad request envelope wrapper framework instance on failure; otherwise null.</returns>
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

        /// <summary>
        /// Performs isolation transforms mapping physical database fields structures down to serialization schemas safely.
        /// </summary>
        /// <param name="accountSource">The user domain physical table snapshot structure entity instance source.</param>
        /// <param name="allocationLink">The relational model tracking entity intersection dataset mapping instance target.</param>
        /// <returns>A clean DTO model container carrying populated tracking metadata fields values.</returns>
        private CreateStaffResponse MapToResponse(User accountSource, StaffClinic allocationLink)
        {
            return new CreateStaffResponse
            {
                // Core user unique identifier projection field mapping
                UserId = accountSource.Id,
                // Junction record allocation identifier token mapping
                StaffClinicId = allocationLink.Id,
                // Contact telephone parameter sequence projection mapping
                Phone = accountSource.Phone,
                // Role definition type converted string presentation mapping
                AssignedRole = allocationLink.Role.ToString()
            };
        }
    }
}