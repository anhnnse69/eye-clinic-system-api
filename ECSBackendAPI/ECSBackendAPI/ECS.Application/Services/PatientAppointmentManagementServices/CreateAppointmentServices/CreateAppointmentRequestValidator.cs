using ECS.Domain.Enums;
using FluentValidation;

namespace ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentServices
{
    /// <summary>
    /// Validator for create appointment request.
    /// Validates all required fields and business rules for appointment booking
    /// </summary>
    public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
    {
        public CreateAppointmentRequestValidator()
        {
            // 1. PatientId validation
            RuleFor(x => x.PatientId)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage("Patient ID is required")
                .Must(BeAValidGuid)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage("Patient ID must be a valid GUID");

            // 2. DoctorId validation
            RuleFor(x => x.DoctorId)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage("Doctor ID is required")
                .Must(BeAValidGuid)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage("Doctor ID must be a valid GUID");

            // 3. SlotId validation
            RuleFor(x => x.SlotId)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage("Time slot ID is required")
                .Must(BeAValidGuid)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage("Time slot ID must be a valid GUID");

            // 4. ServiceId validation (optional)
            RuleFor(x => x.ServiceId)
                .Must(x => x == null || Guid.TryParse(x.ToString(), out _))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage("Service ID must be a valid GUID")
                .When(x => x.ServiceId.HasValue);

            // 5. Symptoms validation (optional)
            RuleFor(x => x.Symptoms)
                .MaximumLength(2000)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage("Symptoms cannot exceed 2000 characters")
                .When(x => !string.IsNullOrEmpty(x.Symptoms));

            // 7. Custom validation rule - kiểm tra các ID không được trùng nhau
            RuleFor(x => x)
                .Must(x => x.PatientId != x.DoctorId)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4020.ToString())
                .WithMessage("Patient and Doctor cannot be the same person")
                .When(x => x.PatientId != Guid.Empty && x.DoctorId != Guid.Empty);
        }

        private static bool BeAValidGuid(Guid value)
        {
            return value != Guid.Empty;
        }

        private static bool BeAValidGuid(Guid? value)
        {
            return !value.HasValue || value.Value != Guid.Empty;
        }
    }
}