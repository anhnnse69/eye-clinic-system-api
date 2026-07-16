using ECS.Domain.Enums;
using FluentValidation;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreatePatientDemographicsServices
{
    /// <summary>
    /// Validator for patient medical demographics creation request.
    /// Based on UC36 - Create Patient Demographics
    /// Administrative fields are optional but validated if provided.
    /// Medical fields are all optional.
    /// </summary>
    public class CreatePatientDemographicsRequestValidator : AbstractValidator<CreatePatientDemographicsRequest>
    {
        public CreatePatientDemographicsRequestValidator()
        {
            // PatientProfileId validation (required)
            RuleFor(x => x.PatientProfileId)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .Must(BeAValidGuid)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // === Administrative Fields (Optional, but validated if provided) ===
            
            // FullName validation (optional)
            RuleFor(x => x.FullName)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.FullName))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // DateOfBirth validation (optional, must be valid date)
            RuleFor(x => x.DateOfBirth)
                .Must(BeAValidDateOrEmpty)
                .When(x => !string.IsNullOrEmpty(x.DateOfBirth))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // Gender validation (optional, must be MALE or FEMALE)
            RuleFor(x => x.Gender)
                .Must(BeAValidGenderOrEmpty)
                .When(x => !string.IsNullOrEmpty(x.Gender))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // PhoneNumber validation (optional)
            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20)
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // IdentityNumber validation (optional)
            RuleFor(x => x.IdentityNumber)
                .MaximumLength(20)
                .When(x => !string.IsNullOrEmpty(x.IdentityNumber))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // BhytNumber validation (optional)
            RuleFor(x => x.BhytNumber)
                .MaximumLength(20)
                .When(x => !string.IsNullOrEmpty(x.BhytNumber))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // Address validation (optional)
            RuleFor(x => x.Address)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.Address))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // === Medical Fields (Optional) ===

            // BloodType validation (optional)
            RuleFor(x => x.BloodType)
                .MaximumLength(10)
                .When(x => !string.IsNullOrEmpty(x.BloodType))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // Allergies validation (optional)
            RuleFor(x => x.Allergies)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.Allergies))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // MedicalHistory validation (optional)
            RuleFor(x => x.MedicalHistory)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrEmpty(x.MedicalHistory))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // FamilyHistory validation (optional)
            RuleFor(x => x.FamilyHistory)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrEmpty(x.FamilyHistory))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // LifestyleFactors validation (optional)
            RuleFor(x => x.LifestyleFactors)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.LifestyleFactors))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // CurrentEyeMedications validation (optional)
            RuleFor(x => x.CurrentEyeMedications)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.CurrentEyeMedications))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // PreviousEyeSurgery validation (optional)
            RuleFor(x => x.PreviousEyeSurgery)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.PreviousEyeSurgery))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            // EyeVisionHistory validation (optional)
            RuleFor(x => x.EyeVisionHistory)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.EyeVisionHistory))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }

        private static bool BeAValidGuid(string? value)
        {
            return string.IsNullOrEmpty(value) || Guid.TryParse(value, out _);
        }

        private static bool BeAValidDateOrEmpty(string? value)
        {
            if (string.IsNullOrEmpty(value)) return true;
            return DateTime.TryParse(value, out _);
        }

        private static bool BeAValidGenderOrEmpty(string? value)
        {
            if (string.IsNullOrEmpty(value)) return true;
            return Enum.TryParse<Gender>(value, true, out _);
        }
    }
}
