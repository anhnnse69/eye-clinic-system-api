using System.Text.Json;
using ECS.Domain.Enums;
using FluentValidation;

namespace ECS.Application.Services.MedicalRecordsServices.CreateMedicalRecordServices
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

            RuleFor(x => x.PatientId)
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

            RuleFor(x => x.FormData)
                .Must(fd => fd.ValueKind == JsonValueKind.Object)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage("FormData must be a JSON object");
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
