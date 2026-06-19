using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Helper.Utility;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Globalization;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreatePatientProfileServices
{
    /// <summary>
    /// Handles business generation rules and multi-entity orchestrations driving receptionist patient ingestion flows.
    /// </summary>
    public class ReceptionistCreatePatientProfileService : IReceptionistCreatePatientProfileService
    {
        private readonly IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> _patientRepository;
        private readonly IRepositoryBaseAsync<User, Guid, AppDbContext> _userRepository;

        // Thread-safe storage frame caching non-persisted credential blocks bridging context allocation onto telemetry feedback blocks
        private readonly ConcurrentDictionary<Guid, string> _generatedPasswords = new ConcurrentDictionary<Guid, string>();

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistCreatePatientProfileService"/> targeting required transactional layers.
        /// </summary>
        /// <param name="patientRepository">The transactional repository monitoring structural patient partitions.</param>
        /// <param name="userRepository">The transactional repository monitoring system security identity configurations.</param>
        public ReceptionistCreatePatientProfileService(
            IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> patientRepository,
            IRepositoryBaseAsync<User, Guid, AppDbContext> userRepository)
        {
            _patientRepository = patientRepository;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Pipeline coordinator driving patient creation workflow without ANY if conditions.
        /// </summary>
        /// <param name="request">The dynamic structural incoming data package describing metadata layers.</param>
        /// <returns>A standardized <see cref="ApiResponse{T}"/> wrapping tracking creation telemetry structures.</returns>
        public async Task<ApiResponse<ReceptionistCreatePatientProfileResponse>> Process(ReceptionistCreatePatientProfileRequest request)
        {
            // Step 1: Establish / automatically provision Account User structures matching layout parameters
            var resolvedUserContext = await StrategyResolveUserContext(request);
            // Step 2: Assemble target Patient Profile graph contexts adhering to intake layout order hierarchies
            var preparedPatient = AssemblePatientProfileGraph(request, resolvedUserContext);
            // Step 3: Flush transactional operation clusters down into persistence tracking frameworks
            await PersistTransactionalOperations(preparedPatient);
            // Step 4: Structurally map entity tracking graphs into short presentation metrics blocks
            var outputResponse = BuildResponsePayload(preparedPatient, request);
            // Step 5: Seal results inside standardized API packet wrappers
            return WrapSuccessOutcome(outputResponse);
        }

        /// <summary>
        /// Resolves user mapping strategy avoiding code-level explicit if branching.
        /// </summary>
        private async Task<User?> StrategyResolveUserContext(ReceptionistCreatePatientProfileRequest request)
        {
            var taskResolver = new Dictionary<bool, Func<Task<User?>>>
            {
                { true, async () => await FetchExistingUserOrThrow(request.SelectedUserId) },
                { false, async () => await ExecuteAutoAccountGeneration(request) }
            };
            return await taskResolver[request.IsHasAccount]();
        }

        /// <summary>
        /// Queries the core system identity boundary matching the assigned token parameter or throws execution boundaries.
        /// </summary>
        private async Task<User?> FetchExistingUserOrThrow(Guid? userId)
        {
            var userGuid = userId ?? throw new ArgumentException("APP_MESSAGE_4019");
            var user = await _userRepository.FindByCondition(u => u.Id == userGuid, trackChanges: true).FirstOrDefaultAsync();
            return user ?? throw new KeyNotFoundException("USER_NOT_FOUND");
        }

        /// <summary>
        /// Generates and mounts system-level authentication nodes along tracked structures.
        /// </summary>
        private async Task<User?> ExecuteAutoAccountGeneration(ReceptionistCreatePatientProfileRequest request)
        {
            // 1. Generate an automated randomized 8-character plaintext string configuration meeting system criteria
            string rawPassword = GenerateRandomTextPassword(8);
            Guid newUserId = Guid.NewGuid();
            // 2. Transcribe cryptographic hashes utilizing security helpers matching onboarding conventions
            string hashedPwd = PasswordHelper.HashPassword(rawPassword);
            var autoUser = new User
            {
                Id = newUserId,
                FullName = request.FullName.Trim(),
                Phone = request.PhoneNumber.Trim(),
                Email = request.Email?.Trim().ToLower(),
                PasswordHash = hashedPwd,
                Role = UserRole.PATIENT,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Hold temporary raw string records inside execution pipelines for telemetry lookup down the sequence
            _generatedPasswords[newUserId] = rawPassword;
            await _userRepository.CreateAsync(autoUser);
            return autoUser;
        }

        /// <summary>
        /// Maps presentation metrics elements onto core structural domain model objects.
        /// </summary>
        private PatientProfile AssemblePatientProfileGraph(ReceptionistCreatePatientProfileRequest request, User? userContext)
        {
            return new PatientProfile
            {
                Id = Guid.NewGuid(),
                UserId = userContext?.Id,
                FullName = request.FullName.Trim(),
                Gender = Enum.Parse<Gender>(request.Gender.ToUpper(), true),
                Dob = DateTime.ParseExact(request.Dob, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None),
                PhoneNumber = request.PhoneNumber.Trim(),
                Address = request.Address?.Trim(),
                IdentityNumber = request.IdentityNumber?.Trim(),
                BhytNumber = request.BhytNumber?.Trim()?.ToUpper(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Commits accumulated multative memory frames safely down onto underlying relational databases.
        /// </summary>
        private async Task PersistTransactionalOperations(PatientProfile patient)
        {
            await _patientRepository.CreateAsync(patient);
            await _patientRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Converts data graphs down into dynamic flat data responses tracking security parameters extraction.
        /// </summary>
        private ReceptionistCreatePatientProfileResponse BuildResponsePayload(PatientProfile patient, ReceptionistCreatePatientProfileRequest request)
        {
            string? clearTextPassword = null;
            if (patient.UserId.HasValue)
            {
                // Purge the cleartext sequence cache frame to keep processing iterations closed
                _generatedPasswords.TryRemove(patient.UserId.Value, out clearTextPassword);
            }
            return new ReceptionistCreatePatientProfileResponse
            {
                PatientProfileId = patient.Id.ToString(),
                FullName = patient.FullName,
                LinkedUserId = patient.UserId?.ToString(),
                IsAccountAutoCreated = !request.IsHasAccount,
                GeneratedPassword = clearTextPassword,
                CreatedAt = patient.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        /// <summary>
        /// Packages compiled structures inside normalized API payload boundaries.
        /// </summary>
        private ApiResponse<ReceptionistCreatePatientProfileResponse> WrapSuccessOutcome(ReceptionistCreatePatientProfileResponse dto)
        {
            return ApiResponse<ReceptionistCreatePatientProfileResponse>.Success("APP_MESSAGE_2000", dto);
        }

        /// <summary>
        /// Generates a structured randomized text sequence for baseline password setup.
        /// </summary>
        private string GenerateRandomTextPassword(int length)
        {
            if (length < 8) length = 8;
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specials = "@#$%!*&?";
            var rnd = new Random();
            var passwordChars = new char[length];
            // 1. Force baseline compliance injection covering vital char tracking patterns
            passwordChars[0] = uppercase[rnd.Next(uppercase.Length)];
            passwordChars[1] = lowercase[rnd.Next(lowercase.Length)];
            passwordChars[2] = digits[rnd.Next(digits.Length)];
            passwordChars[3] = specials[rnd.Next(specials.Length)];
            // 2. Saturate remaining sequence intervals extracting characters across shared sets
            string allAllowed = uppercase + lowercase + digits + specials;
            for (int i = 4; i < length; i++)
            {
                passwordChars[i] = allAllowed[rnd.Next(allAllowed.Length)];
            }
            // 3. Shuffle structural tokens eliminating systematic generation patterns
            return new string(passwordChars.OrderBy(x => rnd.Next()).ToArray());
        }
    }
}