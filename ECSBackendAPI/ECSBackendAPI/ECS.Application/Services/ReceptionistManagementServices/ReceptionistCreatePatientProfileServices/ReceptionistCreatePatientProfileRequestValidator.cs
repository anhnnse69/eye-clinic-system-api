using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Patient;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreatePatientProfileServices
{
    /// <summary>
    /// Validator containing core rules for validating <see cref="ReceptionistCreatePatientProfileRequest"/>.
    /// </summary>
    public class ReceptionistCreatePatientProfileRequestValidator : AbstractValidator<ReceptionistCreatePatientProfileRequest>
    {
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientQueryRepo;
        private readonly IRepositoryQueryBase<User, Guid, AppDbContext> _userQueryRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistCreatePatientProfileRequestValidator"/> with data streaming repositories.
        /// </summary>
        /// <param name="patientQueryRepo">Query repository targeting unique evaluation over existing patient records.</param>
        /// <param name="userQueryRepo">Query repository facilitating isolation checks across application identity accounts.</param>
        public ReceptionistCreatePatientProfileRequestValidator(
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientQueryRepo,
            IRepositoryQueryBase<User, Guid, AppDbContext> userQueryRepo)
        {
            _patientQueryRepo = patientQueryRepo;
            _userQueryRepo = userQueryRepo;
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("APP_MESSAGE_4003")
                .MaximumLength(150).WithMessage("APP_MESSAGE_4019");
            RuleFor(x => x.Gender)
                .NotEmpty().WithMessage("APP_MESSAGE_4003")
                .Must(g => g.ToUpper() == "MALE" || g.ToUpper() == "FEMALE" || g.ToUpper() == "OTHER")
                .WithMessage("APP_MESSAGE_4019");
            RuleFor(x => x.Dob)
                .NotEmpty().WithMessage("APP_MESSAGE_4003")
                .Must(d => DateTime.TryParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                .WithMessage("APP_MESSAGE_4019");
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("APP_MESSAGE_4003")
                .Matches(@"^0[0-9]{9}$").WithMessage("APP_MESSAGE_4001")
                .MustAsync(BeUniquePatientPhoneNumber).WithMessage("PATIENT_PHONE_EXISTS");
            // Enforce presence and architectural format for email sequences during auto-account generation
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("EMAIL_REQUIRED_FOR_NEW_ACCOUNT")
                .EmailAddress().WithMessage("APP_MESSAGE_4019")
                .MaximumLength(150).WithMessage("APP_MESSAGE_4019")
                .When(x => x.IsHasAccount == false);
            // Enforce layout constraints without structural obligation when connecting against preexisting records
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("APP_MESSAGE_4019")
                .MaximumLength(150).WithMessage("APP_MESSAGE_4019")
                .When(x => x.IsHasAccount == true && !string.IsNullOrEmpty(x.Email));
            RuleFor(x => x.IdentityNumber)
                .Matches(@"^[0-9]{12}$").WithMessage("APP_MESSAGE_4019")
                .MustAsync(BeUniqueIdentityNumber).WithMessage("APP_MESSAGE_4018")
                .When(x => !string.IsNullOrEmpty(x.IdentityNumber));
            RuleFor(x => x.BhytNumber)
                .MaximumLength(20).WithMessage("APP_MESSAGE_4019");
            // Conditional Rules regarding UI flow states
            RuleFor(x => x.SelectedUserId)
                .NotEmpty().WithMessage("ACCOUNT_LINKING_REQUIRED")
                .When(x => x.IsHasAccount == true);
            // Assert system-wide digital routing isolation across structural storage arrays
            RuleFor(x => x.Email)
                .MustAsync(BeUniqueUserEmail).WithMessage("USER_EMAIL_EXISTS")
                .When(x => x.IsHasAccount == false);
        }

        /// <summary>
        /// Asynchronously tracks potential duplication blocks for phone parameters across patient spaces.
        /// </summary>
        private async Task<bool> BeUniquePatientPhoneNumber(string phoneNo, CancellationToken token)
        {
            var exists = await _patientQueryRepo.FindByCondition(p => p.PhoneNumber == phoneNo.Trim(), trackChanges: false).AnyAsync(token);
            return !exists;
        }

        /// <summary>
        /// Asynchronously targets national identity token tracking frames evaluating domain collision markers.
        /// </summary>
        private async Task<bool> BeUniqueIdentityNumber(string? identityNo, CancellationToken token)
        {
            if (string.IsNullOrEmpty(identityNo)) return true;
            var exists = await _patientQueryRepo.FindByCondition(p => p.IdentityNumber == identityNo.Trim(), trackChanges: false).AnyAsync(token);
            return !exists;
        }

        /// <summary>
        /// Asynchronously screens core user account aggregates confirming structural mail boundary isolation.
        /// </summary>
        private async Task<bool> BeUniqueUserEmail(string? email, CancellationToken token)
        {
            if (string.IsNullOrEmpty(email)) return true;
            var exists = await _userQueryRepo.FindByCondition(u => u.Email == email.Trim(), trackChanges: false).AnyAsync(token);
            return !exists;
        }
    }
}