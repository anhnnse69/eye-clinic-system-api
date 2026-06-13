using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemUpdateClinicServices
{
    /// <summary>
    /// Validator containing core rules for validating <see cref="UpdateClinicRequest"/>.
    /// </summary>
    public class UpdateClinicRequestValidator : AbstractValidator<UpdateClinicRequest>
    {
        private readonly IRepositoryBaseAsync<Clinic, Guid, AppDbContext> _repository;

        /// <summary>
        /// Initializes a new instance of <see cref="UpdateClinicRequestValidator"/> with required services.
        /// </summary>
        /// <param name="repository">Repository layer to aid uniqueness checks.</param>
        public UpdateClinicRequestValidator(IRepositoryBaseAsync<Clinic, Guid, AppDbContext> repository)
        {
            _repository = repository;

            // 1. Validate Clinic Name
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(255).WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
            // 2. Validate Clinic Address
            RuleFor(x => x.Address)
                .NotEmpty().WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(500).WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
            // 3. Validate Clinic Phone Number
            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .Matches(@"^0[0-9]{9}$").WithMessage(GeneralCode.APP_MESSAGE_4001.ToString())
                .MustAsync((request, phone, context, cancellation) => BeUniquePhone(phone, context, cancellation))
                .WithMessage(GeneralCode.APP_MESSAGE_4018.ToString());
            // 4. Validate Clinic Email
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .EmailAddress().WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .MaximumLength(150).WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .MustAsync((request, email, context, cancellation) => BeUniqueEmail(email, context, cancellation))
                .WithMessage(GeneralCode.APP_MESSAGE_4017.ToString());
        }

        /// <summary>
        /// Asynchronously checks if the requested email is unique across other clinics.
        /// </summary>
        private async Task<bool> BeUniqueEmail(string email, ValidationContext<UpdateClinicRequest> context, CancellationToken cancellationToken)
        {
            // Safely extract the clinic ID attached from the upstream Controller context
            if (!context.RootContextData.TryGetValue("ClinicId", out var idObj) || idObj is not Guid currentClinicId)
            {
                return true;
            }
            var isEmailDuplicate = await _repository
                .FindByCondition(c => c.Email == email.Trim() && !c.Id.Equals(currentClinicId))
                .AnyAsync(cancellationToken);
            return !isEmailDuplicate;
        }

        /// <summary>
        /// Asynchronously checks if the requested phone number is unique across other clinics.
        /// </summary>
        private async Task<bool> BeUniquePhone(string phone, ValidationContext<UpdateClinicRequest> context, CancellationToken cancellationToken)
        {
            // Safely extract the clinic ID attached from the upstream Controller context
            if (!context.RootContextData.TryGetValue("ClinicId", out var idObj) || idObj is not Guid currentClinicId)
            {
                return true;
            }
            var isPhoneDuplicate = await _repository
                .FindByCondition(c => c.Phone == phone.Trim() && !c.Id.Equals(currentClinicId))
                .AnyAsync(cancellationToken);
            return !isPhoneDuplicate;
        }
    }
}