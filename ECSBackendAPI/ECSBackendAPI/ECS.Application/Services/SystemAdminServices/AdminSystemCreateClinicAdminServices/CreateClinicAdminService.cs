using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemCreateClinicAdminServices
{
    /// <summary>
    /// Implementation handler orchestrating secure account provisioning workflows for Clinic Administrators.
    /// </summary>
    public class CreateClinicAdminService : ICreateClinicAdminService
    {
        private readonly IRepositoryBaseAsync<User, Guid, AppDbContext> _userRepository;
        private readonly IRepositoryBaseAsync<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IRepositoryBaseAsync<Clinic, Guid, AppDbContext> _clinicRepository;
        private readonly IEmailService _emailService;

        // Cache mapping to retain raw text credentials for communication operations safely before final serialization response
        private readonly ConcurrentDictionary<Guid, string> _generatedPasswords = new ConcurrentDictionary<Guid, string>();

        /// <summary>
        /// Initializes dependencies handling infrastructure bindings.
        /// </summary>
        /// <param name="userRepository">Repository bound instance for mutating user records.</param>
        /// <param name="staffClinicRepository">Repository bound instance managing staff-clinic linkages.</param>
        /// <param name="clinicRepository">Repository bound instance for looking up clinic setups.</param>
        /// <param name="emailService">Infrastructure email service interface handling secure transmissions.</param>
        public CreateClinicAdminService(
            IRepositoryBaseAsync<User, Guid, AppDbContext> userRepository,
            IRepositoryBaseAsync<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IRepositoryBaseAsync<Clinic, Guid, AppDbContext> clinicRepository,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _staffClinicRepository = staffClinicRepository;
            _clinicRepository = clinicRepository;
            _emailService = emailService;
        }

        /// <summary>
        /// Executes the application workflow process to provision a new clinic administrator account.
        /// </summary>
        /// <param name="request">The detailed target clinic and personnel payload definitions.</param>
        /// <returns>Execution tracking metadata containing transaction outcomes.</returns>
        public async Task<ApiResponse<CreateClinicAdminResponse>> Process(CreateClinicAdminRequest request)
        {
            // Step 1: Query and verify existential operational parameters for target clinic entity definition
            var targetClinic = await VerifyClinicAvailabilityAsync(request.ClinicId);

            // Step 2: Ensure specified phone registry and email definitions remain unique across active systems
            await ValidateIdentityConflictAsync(request.Phone, request.Email);

            // Step 3: Populate baseline systemic structures injecting unique credentials 
            var newUserEntity = InitializeUserAccountModel(request);

            // Step 4: Persist structural user records securely into application storage
            await _userRepository.CreateAsync(newUserEntity);

            // Step 5: Establish core staff structural linkage referencing administrative clinic permissions
            var staffRelationship = InitializeStaffClinicModel(newUserEntity.Id, request.ClinicId);
            await _staffClinicRepository.CreateAsync(staffRelationship);

            // Step 6: Finalize change tracking blocks by committing ongoing operations to persistence layers
            await _userRepository.SaveChangesAsync();

            // Step 7: Dispatch infrastructure communication transmissions distributing raw passwords safely over target credential email
            await ExecuteCredentialDispatchNotificationAsync(newUserEntity, targetClinic);

            // Step 8: Map completed domain mutations onto external tracking schemas
            var outputDto = MapToResponseDto(newUserEntity, request.ClinicId);

            // Step 9: Finalize integration wrappers delivering operational statuses
            return CreateResponse(outputDto);
        }

        /// <summary>
        /// Validates that the requested target clinic exists within active data schemas.
        /// </summary>
        /// <param name="clinicId">The targeted clinic unique lookup constraint parameter token.</param>
        /// <returns>The verified active clinic data model record.</returns>
        private async Task<Clinic> VerifyClinicAvailabilityAsync(Guid clinicId)
        {
            var targetClinic = await _clinicRepository
                .FindByCondition(c => c.Id == clinicId)
                .FirstOrDefaultAsync<Clinic>();

            if (targetClinic == null)
            {
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4008.ToString());
            }

            return targetClinic;
        }

        /// <summary>
        /// Verifies that phone numbers or email strings do not conflict with existing entity registrations.
        /// </summary>
        /// <param name="phone">The text string tracking user communication credentials.</param>
        /// <param name="email">The email account registration value verifying identity matching records.</param>
        private async Task ValidateIdentityConflictAsync(string phone, string email)
        {
            var uniqueConflict = await _userRepository
                .FindByCondition(u => u.Phone == phone || u.Email == email)
                .AnyAsync();

            if (uniqueConflict)
            {
                throw new InvalidOperationException(GeneralCode.APP_MESSAGE_4017.ToString());
            }
        }

        /// <summary>
        /// Generates identity core instances with initial security configurations.
        /// </summary>
        /// <param name="request">The structural entity request context containing data payload information.</param>
        /// <returns>An initialized domain physical entity matching account structures.</returns>
        private User InitializeUserAccountModel(CreateClinicAdminRequest request)
        {
            var userId = Guid.NewGuid();
            string plainPassword = GenerateRandomTextPassword(10);

            _generatedPasswords.TryAdd(userId, plainPassword);

            return new User
            {
                // Unique Identifier tracking user account creation outcomes
                Id = userId,
                // Contact phone communication metric fields
                Phone = request.Phone,
                // Primary administrative email contact parameter
                Email = request.Email,
                // Whole clear text identity label string
                FullName = request.FullName,
                // Fixed scope mapping context utilizing uppercase domain definition
                Role = UserRole.CLINIC_ADMIN,
                // Context marker tracking operational usage permissions
                IsActive = true,
                // Hashed cryptographic baseline representation data stream
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
                // Operational creation benchmark timing tracker
                CreatedAt = DateTime.UtcNow,
                // System timeline record mutation tracker
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Prepares the junction table mapping relation binding the user to administrative roles inside a specific clinic.
        /// </summary>
        /// <param name="userId">The system identity tracker key bound onto core accounts.</param>
        /// <param name="clinicId">The contextual location domain entity reference filter identity value.</param>
        /// <returns>A configured mapping entity linking staff roles to active environments.</returns>
        private StaffClinic InitializeStaffClinicModel(Guid userId, Guid clinicId)
        {
            return new StaffClinic
            {
                // Relationship identification token coordinates
                Id = Guid.NewGuid(),
                // Identity binding map pointer reference coordinate
                UserId = userId,
                // Operations deployment location coordinate pointer
                ClinicId = clinicId,
                // Designated clinic administrator authority level mapped via uppercase configuration
                Role = StaffRole.CLINIC_ADMIN,
                // Control flag handling layout permission visibility constraints
                IsActive = true,
                // Timeline record creation timestamp metric
                CreatedAt = DateTime.UtcNow,
                // System modification trace tracker log entry
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Sends generated passwords and emails safely to the newly created clinic administrator contact channel.
        /// </summary>
        /// <param name="user">The physical user structure tracking security settings endpoints.</param>
        /// <param name="clinic">The persistent target context tracking physical attributes references.</param>
        private async Task ExecuteCredentialDispatchNotificationAsync(User user, Clinic clinic)
        {
            _generatedPasswords.TryGetValue(user.Id, out string? plaintextPassword);

            if (!string.IsNullOrEmpty(plaintextPassword) && !string.IsNullOrEmpty(user.Email))
            {
                string adminSubject = "[ECS System] Welcome! Your Clinic Administrator Account is Ready";
                string adminBody = $@"
                    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <h2>Welcome to ECS System, {user.FullName}!</h2>
                        <p>An administrator account associated with <strong>{clinic.Name}</strong> has been successfully created for you.</p>
                        <p>You can now access your management workspace using the following temporary credentials:</p>
                        <table style='border-collapse: collapse; width: 100%; max-width: 500px;'>
                            <tr>
                                <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Login Email:</td>
                                <td style='padding: 8px; border: 1px solid #ddd; color: #0066cc;'>{user.Email}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Temporary Password:</td>
                                <td style='padding: 8px; border: 1px solid #ddd; font-family: monospace; font-size: 1.1em; background-color: #f9f9f9;'>{plaintextPassword}</td>
                            </tr>
                        </table>
                        <br/>
                        <div style='padding: 12px; background-color: #fff3cd; border-left: 4px solid #ffc107; margin-bottom: 15px;'>
                            <strong>⚠️ Action Required:</strong> For system security compliance, you are <strong>required to change this password</strong> immediately upon your first successful login session.
                        </div>
                        <p>If you have any questions or did not request this credential setup, please contact your clinical operations lead or the system administration team.</p>
                        <br/>
                        <p>Best regards,<br/><strong>ECS System Administration Team</strong></p>
                    </div>";

                await _emailService.SendEmailAsync(user.Email, adminSubject, adminBody);
            }

            _generatedPasswords.TryRemove(user.Id, out _);
        }

        /// <summary>
        /// Converts data models cleanly into designated schema response instances.
        /// </summary>
        /// <param name="sourceUser">The persistent source user domain entity instance.</param>
        /// <param name="clinicId">The target clinic identifier token.</param>
        /// <returns>A completely populated response presentation data model.</returns>
        private CreateClinicAdminResponse MapToResponseDto(User sourceUser, Guid clinicId)
        {
            return new CreateClinicAdminResponse
            {
                // Unique Identifier tracking user account creation outcomes
                UserId = sourceUser.Id,
                // Operational location bounding admin boundaries
                ClinicId = clinicId,
                // Email communication registry identifier
                Email = sourceUser.Email ?? string.Empty,
                // Text representation detailing functional user classification level
                Role = sourceUser.Role.ToString()
            };
        }

        /// <summary>
        /// Packages structural responses cleanly inside consistent transmission frames.
        /// </summary>
        /// <param name="dto">The compiled data mapping output contract object instance.</param>
        /// <returns>A configured standard API payload frame handling serialization workflows.</returns>
        private ApiResponse<CreateClinicAdminResponse> CreateResponse(CreateClinicAdminResponse dto)
        {
            return ApiResponse<CreateClinicAdminResponse>.Success(GeneralCode.APP_MESSAGE_2000.ToString(), dto);
        }

        /// <summary>
        /// Generates a structured randomized text sequence for baseline password setup.
        /// </summary>
        /// <param name="length">The requested quantity parameter checking string generation requirements.</param>
        /// <returns>A validated sequence tracking security requirements rule parameters.</returns>
        private string GenerateRandomTextPassword(int length)
        {
            if (length < 8) length = 8;
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specials = "@#$%!*&?";
            var rnd = new Random();
            var passwordChars = new char[length];

            passwordChars[0] = uppercase[rnd.Next(uppercase.Length)];
            passwordChars[1] = lowercase[rnd.Next(lowercase.Length)];
            passwordChars[2] = digits[rnd.Next(digits.Length)];
            passwordChars[3] = specials[rnd.Next(specials.Length)];

            string allAllowed = uppercase + lowercase + digits + specials;
            for (int i = 4; i < length; i++)
            {
                passwordChars[i] = allAllowed[rnd.Next(allAllowed.Length)];
            }
            return new string(passwordChars.OrderBy(x => rnd.Next()).ToArray());
        }
    }
}