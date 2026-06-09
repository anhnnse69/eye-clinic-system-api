using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Helper.Utility;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.AuthServices.RegisterServices
{
    /// <summary>
    /// Handles user registration process.
    /// </summary>
    public class RegisterService : IRegisterService
    {
        /// <summary>Repository for read-only queries.</summary>
        private readonly IRepositoryQueryBase<User, Guid, AppDbContext>
            _userQueryRepository;
        /// <summary>Repository for write operations.</summary>
        private readonly IRepositoryBaseAsync<User, Guid, AppDbContext>
            _userRepository;

        public RegisterService(
            IRepositoryQueryBase<User, Guid, AppDbContext> userQueryRepository,
            IRepositoryBaseAsync<User, Guid, AppDbContext> userRepository)
        {
            _userQueryRepository = userQueryRepository;
            _userRepository = userRepository;
        }

        /// <inheritdoc/>
        public async Task<ApiResponse<bool>> Process(RegisterRequest request)
        {
            var errorCode = await GetValidationError(request);
            var isFailed = errorCode != null;

            return isFailed
                ? ApiResponse<bool>.Fail(errorCode!)
                : await CreateUser(request);
        }

        /// <summary>
        /// Returns the first validation error code found, or null if the request is valid.
        /// </summary>
        private async Task<string?> GetValidationError(RegisterRequest request)
        {
            var passwordMismatch = request.Password != request.ConfirmPassword;
            if (passwordMismatch) return GeneralCode.APP_MESSAGE_4019.ToString();

            var emailExists = await _userQueryRepository
                .FindByCondition(x =>
                    x.Email != null &&
                    x.Email.ToLower() == request.Email.ToLower())
                .AnyAsync();
            if (emailExists) return GeneralCode.APP_MESSAGE_4017.ToString();

            var phoneExists = await _userQueryRepository
                .FindByCondition(x => x.Phone == request.Phone)
                .AnyAsync();
            if (phoneExists) return GeneralCode.APP_MESSAGE_4018.ToString();

            return null;
        }

        /// <summary>
        /// Persists the new user and returns a success response.
        /// </summary>
        private async Task<ApiResponse<bool>> CreateUser(RegisterRequest request)
        {
            var user = BuildUser(request);
            await _userRepository.CreateAsync(user);
            await _userRepository.SaveChangesAsync();
            return ApiResponse<bool>.Success(GeneralCode.APP_MESSAGE_2000.ToString(), true);
        }

        /// <summary>
        /// Builds a new <see cref="User"/> entity from the registration request.
        /// Role defaults to <see cref="UserRole.PATIENT"/>.
        /// </summary>
        /// <param name="request">The validated registration data.</param>
        /// <returns>A new <see cref="User"/> entity ready to be persisted.</returns>
        private User BuildUser(RegisterRequest request)
        {
            return new User
            {
                Id           = Guid.NewGuid(),
                FullName     = request.FullName.Trim(),
                Email        = request.Email.Trim().ToLower(),
                Phone        = request.Phone.Trim(),
                PasswordHash = PasswordHelper.HashPassword(request.Password),
                Role         = UserRole.PATIENT,
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow
            };
        }
    }
}