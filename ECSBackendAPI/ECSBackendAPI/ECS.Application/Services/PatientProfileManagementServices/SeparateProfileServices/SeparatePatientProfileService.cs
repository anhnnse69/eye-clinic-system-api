using System.Security.Claims;
using System.Linq;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Configurations;
using ECS.Domain.Enums;
using ECS.Infrastructure.Helper.Utility;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.ConfigService.EmailService;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientProfileManagementServices.SeparateProfileServices
{
    /// <summary>
    /// Service implementation for separating a dependent child profile into an independent account.
    /// Handles all business logic including user creation, profile ownership transfer,
    /// access revocation, and email notification.
    /// </summary>
    public class SeparatePatientProfileService : ISeparatePatientProfileService
    {
        private readonly IRepositoryQueryBase<User, Guid, AppDbContext> _userQueryRepository;
        private readonly IRepositoryBaseAsync<User, Guid, AppDbContext> _userRepository;
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientQueryRepository;
        private readonly IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> _patientRepository;
        private readonly IRepositoryBaseAsync<AuditLog, Guid, AppDbContext> _auditLogRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidator<SeparateProfileRequest> _validator;
        private readonly IEmailService _emailService;
        private readonly AppDbContext _dbContext;

        public SeparatePatientProfileService(
            IRepositoryQueryBase<User, Guid, AppDbContext> userQueryRepository,
            IRepositoryBaseAsync<User, Guid, AppDbContext> userRepository,
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientQueryRepository,
            IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> patientRepository,
            IRepositoryBaseAsync<AuditLog, Guid, AppDbContext> auditLogRepository,
            IHttpContextAccessor httpContextAccessor,
            IValidator<SeparateProfileRequest> validator,
            IEmailService emailService,
            AppDbContext dbContext)
        {
            _userQueryRepository = userQueryRepository;
            _userRepository = userRepository;
            _patientQueryRepository = patientQueryRepository;
            _patientRepository = patientRepository;
            _auditLogRepository = auditLogRepository;
            _httpContextAccessor = httpContextAccessor;
            _validator = validator;
            _emailService = emailService;
            _dbContext = dbContext;
        }

        public async Task<ApiResponse<SeparateProfileResponse>> Process(SeparateProfileRequest request)
        {
            bool isValidationPassed = true;
            bool isCurrentUserValid = true;
            string? validationErrorCode = null;

            ValidateRequest(request, ref isValidationPassed, ref validationErrorCode);

            var parentUserId = RetrieveAuthenticatedUserId(ref isCurrentUserValid);

            var (childProfile, isChildProfileValid, isOwnProfile) = await VerifyChildProfileOwnership(request.ChildPatientProfileId, parentUserId, isValidationPassed, isCurrentUserValid);

            bool isEmailPhoneUnique = await VerifyEmailPhoneUniqueness(request.NewEmail, request.NewPhone, isValidationPassed, isCurrentUserValid, isChildProfileValid);

            var (isExecutionSuccess, newUserId, newProfileId) = await ExecuteSeparationTransaction(childProfile, request.NewEmail, request.NewPhone, parentUserId, isValidationPassed, isCurrentUserValid, isChildProfileValid, isEmailPhoneUnique);

            return CreateResponse(validationErrorCode, isValidationPassed, isCurrentUserValid, isChildProfileValid, isOwnProfile, isEmailPhoneUnique, isExecutionSuccess, newUserId, newProfileId);
        }

        private void ValidateRequest(SeparateProfileRequest request, ref bool isValidationPassed, ref string? validationErrorCode)
        {
            var result = _validator.Validate(request);
            isValidationPassed = result.IsValid;
            validationErrorCode = result.IsValid ? null : result.Errors.FirstOrDefault()?.ErrorCode ?? GeneralCode.APP_MESSAGE_4019.ToString();
        }

        private Guid RetrieveAuthenticatedUserId(ref bool isCurrentUserValid)
        {
            var principalIdValue = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(principalIdValue, out var parsedUserId))
            {
                isCurrentUserValid = false;
                return Guid.Empty;
            }
            return parsedUserId;
        }

        private async Task<(PatientProfile? Profile, bool IsValid, bool IsOwnProfile)> VerifyChildProfileOwnership(Guid childProfileId, Guid parentUserId, bool isValidationPassed, bool isCurrentUserValid)
        {
            if (!isValidationPassed || !isCurrentUserValid) return (null, true, false);

            var childProfile = await _patientQueryRepository
                .FindByCondition(x => x.Id == childProfileId)
                .FirstOrDefaultAsync();

            if (childProfile == null)
            {
                return (null, false, false);
            }

            if (childProfile.UserId == parentUserId)
            {
                return (null, false, true);
            }

            var hasAccess = await _dbContext.UserPatients
                .AnyAsync(x => x.UserId == parentUserId && x.PatientId == childProfileId);

            if (!hasAccess)
            {
                return (null, false, false);
            }

            return (childProfile, true, false);
        }

        private async Task<bool> VerifyEmailPhoneUniqueness(string email, string phone, bool isValidationPassed, bool isCurrentUserValid, bool isChildProfileValid)
        {
            if (!isValidationPassed || !isCurrentUserValid || !isChildProfileValid) return true;

            var emailExists = await _userQueryRepository
                .FindByCondition(x => x.Email != null && x.Email.ToLower() == email.ToLower())
                .AnyAsync();

            var phoneExists = await _userQueryRepository
                .FindByCondition(x => x.Phone == phone)
                .AnyAsync();

            return !(emailExists || phoneExists);
        }

        private async Task<(bool IsSuccess, Guid NewUserId, Guid NewProfileId)> ExecuteSeparationTransaction(PatientProfile? childProfile, string newEmail, string newPhone, Guid parentUserId, bool isValidationPassed, bool isCurrentUserValid, bool isChildProfileValid, bool isEmailPhoneUnique)
        {
            if (childProfile == null || !isValidationPassed || !isCurrentUserValid || !isChildProfileValid || !isEmailPhoneUnique)
            {
                return (false, Guid.Empty, Guid.Empty);
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var tempPassword = GenerateTemporaryPassword(12);

                var newUser = await CreateNewUserAccount(childProfile, newEmail, newPhone, tempPassword);

                // Transfer profile ownership to new user
                childProfile.UserId = newUser.Id;
                childProfile.UpdatedAt = DateTime.UtcNow;
                await _patientRepository.UpdateAsync(childProfile);

                await RemoveParentAccess(parentUserId, childProfile.Id);
                await LogAuditEntry(parentUserId, childProfile.Id, newUser.Id, childProfile.Id);

                await transaction.CommitAsync();

                _ = SendSeparationEmail(newUser.Email!, tempPassword, childProfile.FullName);

                return (true, newUser.Id, childProfile.Id);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return (false, Guid.Empty, Guid.Empty);
            }
        }

        private string GenerateTemporaryPassword(int length)
        {
            const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
            const string digitChars = "0123456789";
            const string specialChars = "!@#$%^&*";
            const string allChars = upperChars + lowerChars + digitChars + specialChars;
            
            var random = new Random();
            var passwordChars = new List<char>();
            
            // Ensure at least one character from each category
            passwordChars.Add(upperChars[random.Next(upperChars.Length)]);
            passwordChars.Add(lowerChars[random.Next(lowerChars.Length)]);
            passwordChars.Add(digitChars[random.Next(digitChars.Length)]);
            passwordChars.Add(specialChars[random.Next(specialChars.Length)]);
            
            // Fill the rest with random characters from all categories
            for (int i = 4; i < length; i++)
            {
                passwordChars.Add(allChars[random.Next(allChars.Length)]);
            }
            
            // Shuffle the characters to avoid predictable pattern
            return new string(passwordChars.OrderBy(x => random.Next()).ToArray());
        }

        private async Task<User> CreateNewUserAccount(PatientProfile childProfile, string email, string phone, string tempPassword)
        {
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = childProfile.FullName,
                Email = email.ToLower().Trim(),
                Phone = phone.Trim(),
                PasswordHash = PasswordHelper.HashPassword(tempPassword),
                Role = UserRole.PATIENT,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(newUser);
            return newUser;
        }

        private async Task RemoveParentAccess(Guid parentUserId, Guid oldProfileId)
        {
            var userPatient = await _dbContext.UserPatients
                .FirstOrDefaultAsync(x => x.UserId == parentUserId && x.PatientId == oldProfileId);

            if (userPatient != null)
            {
                _dbContext.UserPatients.Remove(userPatient);
                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task LogAuditEntry(Guid parentUserId, Guid oldProfileId, Guid newUserId, Guid newProfileId)
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = parentUserId,
                Action = "SEPARATE_PATIENT_PROFILE",
                TableName = "PatientProfile",
                RecordId = oldProfileId.ToString(),
                NewValue = $"Transferred patient profile {oldProfileId} ownership from parent {parentUserId} to independent account {newUserId}",
                CreatedAt = DateTime.UtcNow,
                IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
            };

            await _auditLogRepository.CreateAsync(auditLog);
        }

        private async Task SendSeparationEmail(string email, string tempPassword, string childName)
        {
            try
            {
                var emailBody = BuildSeparationEmailBody(childName, email, tempPassword);
                await _emailService.SendEmailAsync(email, "Tách tài khoản thành công - Your New Independent Account", emailBody);
            }
            catch
            {
                // Background task email sending failure should not fail the API
            }
        }

        private string BuildSeparationEmailBody(string childName, string email, string tempPassword)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; }}
        .header h1 {{ color: #ffffff; margin: 0; font-size: 24px; }}
        .content {{ padding: 40px 30px; text-align: center; }}
        .credentials {{ background: #f8f9fa; border-left: 4px solid #667eea; padding: 20px; margin: 30px 0; text-align: left; }}
        .credentials-label {{ font-size: 12px; color: #999; text-transform: uppercase; }}
        .credentials-value {{ font-size: 16px; font-weight: bold; color: #333; margin-top: 5px; font-family: monospace; }}
        .message {{ color: #666666; font-size: 14px; line-height: 1.6; margin: 20px 0; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; font-size: 12px; color: #856404; }}
        .button {{ background: #667eea; color: #ffffff; padding: 12px 30px; border-radius: 5px; text-decoration: none; display: inline-block; margin: 20px 0; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #999999; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Tách Tài Khoản Thành Công</h1>
        </div>
        <div class='content'>
            <p class='message'>Xin chào <strong>{childName}</strong>,</p>
            
            <p class='message'>
                Cha/mẹ của bạn đã thành công chuyển quyền quản lý hồ sơ y tế của bạn sang tài khoản độc lập. 
                Bây giờ bạn có thể quản lý hồ sơ y tế của riêng mình với các thông tin đăng nhập dưới đây.
            </p>

            <div class='credentials'>
                <div>
                    <div class='credentials-label'>Email:</div>
                    <div class='credentials-value'>{email}</div>
                </div>
                <div style='margin-top: 15px;'>
                    <div class='credentials-label'>Mật khẩu tạm thời:</div>
                    <div class='credentials-value'>{tempPassword}</div>
                </div>
            </div>

            <div class='warning'>
                <strong>⚠️ Lưu ý:</strong> Vui lòng đổi mật khẩu tạm thời này ngay lần đầu tiên đăng nhập. 
                Mật khẩu này chỉ có hiệu lực một lần.
            </div>

            <p class='message'>
                Tất cả hồ sơ y tế, lịch khám, và lịch sử bệnh của bạn vẫn được bảo lưu và có thể truy cập 
                với tài khoản mới.
            </p>

            <p class='message'>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi.</p>

            <p style='color: #999; font-size: 12px; margin-top: 40px;'>
                Email này được gửi từ hệ thống Eye Clinic Support (ECS).<br>
                Vui lòng không trả lời email này.
            </p>
        </div>
        <div class='footer'>
            <p>&copy; 2026 Eye Clinic Support System. Bảo lưu mọi quyền.</p>
        </div>
    </div>
</body>
</html>
";
        }

        private ApiResponse<SeparateProfileResponse> CreateResponse(
            string? validationErrorCode,
            bool isValidationPassed,
            bool isCurrentUserValid,
            bool isChildProfileValid,
            bool isOwnProfile,
            bool isEmailPhoneUnique,
            bool isExecutionSuccess,
            Guid newUserId,
            Guid newProfileId)
        {
            var systemicFailure = FilterSystemicValidationFailures(validationErrorCode, isValidationPassed, isCurrentUserValid, isChildProfileValid, isOwnProfile, isEmailPhoneUnique, isExecutionSuccess);
            
            if (systemicFailure != null)
            {
                return systemicFailure;
            }

            return ApiResponse<SeparateProfileResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                new SeparateProfileResponse
                {
                    NewUserId = newUserId,
                    NewPatientProfileId = newProfileId,
                    Message = "Chuyển quyền quản lý hồ sơ thành công. Mật khẩu đã được gửi qua email."
                });
        }

        private ApiResponse<SeparateProfileResponse>? FilterSystemicValidationFailures(
            string? validationErrorCode,
            bool isValidationPassed,
            bool isCurrentUserValid,
            bool isChildProfileValid,
            bool isOwnProfile,
            bool isEmailPhoneUnique,
            bool isExecutionSuccess)
        {
            if (!isValidationPassed)
            {
                return ApiResponse<SeparateProfileResponse>.Fail(validationErrorCode ?? GeneralCode.APP_MESSAGE_4019.ToString());
            }
            if (!isCurrentUserValid)
            {
                return ApiResponse<SeparateProfileResponse>.Fail(GeneralCode.APP_MESSAGE_4033.ToString());
            }
            if (isOwnProfile)
            {
                return ApiResponse<SeparateProfileResponse>.Fail(GeneralCode.APP_MESSAGE_4014.ToString()); // User does not have permission
            }
            if (!isChildProfileValid)
            {
                return ApiResponse<SeparateProfileResponse>.Fail(GeneralCode.APP_MESSAGE_4010.ToString()); // Specified patient not found in the system
            }
            if (!isEmailPhoneUnique)
            {
                return ApiResponse<SeparateProfileResponse>.Fail(GeneralCode.APP_MESSAGE_4017.ToString()); // Email/Phone already exists
            }
            if (!isExecutionSuccess)
            {
                return ApiResponse<SeparateProfileResponse>.Fail(GeneralCode.APP_MESSAGE_5001.ToString()); // Database operation failed
            }
            return null;
        }
    }
}
