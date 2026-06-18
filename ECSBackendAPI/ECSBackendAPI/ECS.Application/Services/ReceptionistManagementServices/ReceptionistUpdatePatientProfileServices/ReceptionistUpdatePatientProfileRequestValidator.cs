using ECS.Domain.Entities.Patient;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistUpdatePatientProfileServices
{
    /// <summary>
    /// Validator containing core rules for validating <see cref="ReceptionistUpdatePatientProfileRequest"/>.
    /// </summary>
    public class ReceptionistUpdatePatientProfileRequestValidator : AbstractValidator<ReceptionistUpdatePatientProfileRequest>
    {
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientQueryRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistUpdatePatientProfileRequestValidator"/> with required services.
        /// </summary>
        /// <param name="patientQueryRepo">Repository layer to aid uniqueness checks against patient profile domains.</param>
        public ReceptionistUpdatePatientProfileRequestValidator(IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientQueryRepo)
        {
            _patientQueryRepo = patientQueryRepo;
            // 1. Validate Patient Full Name
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("APP_MESSAGE_4003")
                .MaximumLength(150).WithMessage("APP_MESSAGE_4019");
            // 2. Validate Presentation Gender Representation Code
            RuleFor(x => x.Gender)
                .NotEmpty().WithMessage("APP_MESSAGE_4003")
                .Must(g => g.ToUpper() == "MALE" || g.ToUpper() == "FEMALE" || g.ToUpper() == "OTHER")
                .WithMessage("APP_MESSAGE_4019");
            // 3. Validate Patient Date of Birth Strict Formatting
            RuleFor(x => x.Dob)
                .NotEmpty().WithMessage("APP_MESSAGE_4003")
                .Must(d => DateTime.TryParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                .WithMessage("APP_MESSAGE_4019");
            // 4. Validate Patient Telephone Sequence and Uniqueness
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("APP_MESSAGE_4003")
                .Matches(@"^0[0-9]{9}$").WithMessage("APP_MESSAGE_4001")
                .MustAsync(BeUniquePhoneNumber).WithMessage("PATIENT_PHONE_EXISTS");
            // 5. Evaluate Structural Email Syntax Limits Without Uniqueness Check
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("APP_MESSAGE_4019")
                .MaximumLength(150).WithMessage("APP_MESSAGE_4019")
                .When(x => !string.IsNullOrEmpty(x.Email));
            // 6. Validate National Identity Card Layout Metrics and Isolation Boundary
            RuleFor(x => x.IdentityNumber)
                .Matches(@"^[0-9]{12}$").WithMessage("APP_MESSAGE_4019")
                .MustAsync(BeUniqueIdentityNumber).WithMessage("APP_MESSAGE_4018")
                .When(x => !string.IsNullOrEmpty(x.IdentityNumber));
            // 7. Insurance Baseline BHYT Structural Content Restrictions
            RuleFor(x => x.BhytNumber)
                .MaximumLength(20).WithMessage("APP_MESSAGE_4019");
        }

        /// <summary>
        /// Asynchronously checks if the requested phone number is unique across other patient profile contexts.
        /// </summary>
        /// <param name="request">The root request payload package instance context.</param>
        /// <param name="phoneNo">The raw incoming target phone number string to parse.</param>
        /// <param name="context">The contextual collection containing evaluation pipeline properties metadata.</param>
        /// <param name="cancellationToken">The architectural token monitoring execution termination metrics.</param>
        /// <returns>True if the phone number context does not cause duplicate collusions; otherwise, false.</returns>
        private async Task<bool> BeUniquePhoneNumber(
            ReceptionistUpdatePatientProfileRequest request,
            string? phoneNo,
            ValidationContext<ReceptionistUpdatePatientProfileRequest> context,
            CancellationToken cancellationToken)
        {
            // Safely extract the target patient ID attached from the upstream Controller context
            if (string.IsNullOrEmpty(phoneNo) ||
                !context.RootContextData.TryGetValue("TargetPatientId", out var idObj) ||
                idObj is not Guid currentPatientId)
            {
                return true;
            }
            var isDuplicate = await _patientQueryRepo
                .FindByCondition(p => p.PhoneNumber == phoneNo.Trim() && !p.Id.Equals(currentPatientId), trackChanges: false)
                .AnyAsync(cancellationToken);
            return !isDuplicate;
        }

        /// <summary>
        /// Asynchronously checks if the requested National Identity Number is unique across other patient profile contexts.
        /// </summary>
        /// <param name="request">The root request payload package instance context.</param>
        /// <param name="identityNo">The raw incoming national identity registration layout string to parse.</param>
        /// <param name="context">The contextual collection containing evaluation pipeline properties metadata.</param>
        /// <param name="cancellationToken">The architectural token monitoring execution termination metrics.</param>
        /// <returns>True if the identifier context does not cause duplicate collusions; otherwise, false.</returns>
        private async Task<bool> BeUniqueIdentityNumber(
            ReceptionistUpdatePatientProfileRequest request,
            string? identityNo,
            ValidationContext<ReceptionistUpdatePatientProfileRequest> context,
            CancellationToken cancellationToken)
        {
            // Safely extract the target patient ID attached from the upstream Controller context
            if (string.IsNullOrEmpty(identityNo) ||
                !context.RootContextData.TryGetValue("TargetPatientId", out var idObj) ||
                idObj is not Guid currentPatientId)
            {
                return true;
            }
            var isDuplicate = await _patientQueryRepo
                .FindByCondition(p => p.IdentityNumber == identityNo.Trim() && !p.Id.Equals(currentPatientId), trackChanges: false)
                .AnyAsync(cancellationToken);
            return !isDuplicate;
        }
    }
}