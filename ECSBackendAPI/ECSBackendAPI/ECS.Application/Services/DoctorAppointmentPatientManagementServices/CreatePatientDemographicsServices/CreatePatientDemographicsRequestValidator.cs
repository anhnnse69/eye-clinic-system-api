using ECS.Domain.Enums;
using FluentValidation;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreatePatientDemographicsServices
{
    /// <summary>
    /// Validator for patient medical demographics creation request.
    /// Based on UC36 - Create Patient Demographics
    /// Medical Demographics: All fields are optional (patient info already created by Patient/Receptionist)
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
    }
}
