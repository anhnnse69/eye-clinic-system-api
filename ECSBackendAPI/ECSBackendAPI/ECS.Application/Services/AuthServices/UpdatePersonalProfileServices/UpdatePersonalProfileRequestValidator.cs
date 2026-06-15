using ECS.Domain.Entities.Auth;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.AuthServices.UpdatePersonalProfileServices
{
    /// <summary>
    /// Validator containing core rules for validating <see cref="UpdatePersonalProfileRequest"/>.
    /// </summary>
    public class UpdatePersonalProfileRequestValidator : AbstractValidator<UpdatePersonalProfileRequest>
    {
        private readonly IRepositoryQueryBase<User, Guid, AppDbContext> _userQueryRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="UpdatePersonalProfileRequestValidator"/> with required services.
        /// </summary>
        /// <param name="userQueryRepo">Repository layer to aid uniqueness checks.</param>
        public UpdatePersonalProfileRequestValidator(IRepositoryQueryBase<User, Guid, AppDbContext> userQueryRepo)
        {
            _userQueryRepo = userQueryRepo;
            // 1. Validate User Full Name
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("APP_MESSAGE_4003")
                .MaximumLength(150).WithMessage("APP_MESSAGE_4019");
            // 2. Validate User Phone Number
            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("APP_MESSAGE_4003")
                .Matches(@"^0[0-9]{9}$").WithMessage("APP_MESSAGE_4001")
                .MustAsync(BeUniquePhone).WithMessage("APP_MESSAGE_4018");
            // 3. Validate User Email Address
            RuleFor(x => x.Email)
                .MaximumLength(150).WithMessage("APP_MESSAGE_4019")
                .EmailAddress().WithMessage("APP_MESSAGE_4019")
                .When(x => !string.IsNullOrEmpty(x.Email))
                .MustAsync(BeUniqueEmail).WithMessage("APP_MESSAGE_4017");
        }

        /// <summary>
        /// Asynchronously checks if the requested phone number is unique across other users.
        /// </summary>
        private async Task<bool> BeUniquePhone(UpdatePersonalProfileRequest request, string phone, ValidationContext<UpdatePersonalProfileRequest> context, CancellationToken cancellationToken)
        {
            // Safely extract the user ID attached from the upstream Controller context
            if (!context.RootContextData.TryGetValue("TargetUserId", out var idObj) || idObj is not Guid currentUserId)
            {
                return true;
            }
            var isPhoneDuplicate = await _userQueryRepo
                .FindByCondition(u => u.Phone == phone.Trim() && !u.Id.Equals(currentUserId), trackChanges: false)
                .AnyAsync<User>(cancellationToken);
            return !isPhoneDuplicate;
        }

        /// <summary>
        /// Asynchronously checks if the requested email is unique across other users.
        /// </summary>
        private async Task<bool> BeUniqueEmail(UpdatePersonalProfileRequest request, string? email, ValidationContext<UpdatePersonalProfileRequest> context, CancellationToken cancellationToken)
        {
            // Safely extract the user ID attached from the upstream Controller context
            if (string.IsNullOrEmpty(email) || !context.RootContextData.TryGetValue("TargetUserId", out var idObj) || idObj is not Guid currentUserId)
            {
                return true;
            }
            var isEmailDuplicate = await _userQueryRepo
                .FindByCondition(u => u.Email == email.Trim() && !u.Id.Equals(currentUserId), trackChanges: false)
                .AnyAsync<User>(cancellationToken);
            return !isEmailDuplicate;
        }
    }
}