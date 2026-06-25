using FluentValidation;
using ECS.Domain.Enums;

namespace ECS.Application.Services.MedicalRecordsServices.UpdateMedicalRecordServices
{
    /// <summary>
    /// Validator for UpdateMedicalRecordRequest.
    /// </summary>
    public class UpdateMedicalRecordRequestValidator : AbstractValidator<UpdateMedicalRecordRequest>
    {
        public UpdateMedicalRecordRequestValidator()
        {
            RuleFor(x => x.RecordType)
                .Must(BeAValidRecordTypeOrEmpty)
                .When(x => !string.IsNullOrEmpty(x.RecordType))
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.DiagnosisMain)
                .NotEmpty()
                    .When(x => x.DiagnosisMain != null)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage("Primary diagnosis cannot be cleared when updating medical record");
        }

        private static bool BeAValidRecordTypeOrEmpty(string value)
        {
            if (string.IsNullOrEmpty(value)) return true;
            return Enum.TryParse<RecordType>(value, true, out _);
        }
    }
}
