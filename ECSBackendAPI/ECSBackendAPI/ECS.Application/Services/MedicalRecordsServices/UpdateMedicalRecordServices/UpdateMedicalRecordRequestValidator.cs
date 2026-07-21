using System.Text.Json;
using ECS.Domain.Enums;
using FluentValidation;

namespace ECS.Application.Services.MedicalRecordsServices.UpdateMedicalRecordServices
{
    /// <summary>
    /// Validator for UpdateMedicalRecordRequest.
    /// </summary>
    public class UpdateMedicalRecordRequestValidator : AbstractValidator<UpdateMedicalRecordRequest>
    {
        public UpdateMedicalRecordRequestValidator()
        {
            RuleFor(x => x.FormData)
                .Must(fd => fd.ValueKind == JsonValueKind.Object)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage("FormData must be a JSON object");
        }
    }
}
