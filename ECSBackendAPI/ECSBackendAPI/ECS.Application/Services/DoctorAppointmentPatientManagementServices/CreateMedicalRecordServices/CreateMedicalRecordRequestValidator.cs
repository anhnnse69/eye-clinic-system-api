using FluentValidation;
using ECS.Domain.Enums;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreateMedicalRecordServices
{
    /// <summary>
    /// Validator for CreateMedicalRecordRequest.
    /// </summary>
    public class CreateMedicalRecordRequestValidator : AbstractValidator<CreateMedicalRecordRequest>
    {
        public CreateMedicalRecordRequestValidator()
        {
            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .Must(BeAValidGuid)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.RecordType)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .Must(BeAValidRecordType)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }

        private static bool BeAValidGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }

        private static bool BeAValidRecordType(string value)
        {
            return Enum.TryParse<RecordType>(value, true, out _);
        }
    }
}
