using FluentValidation;
using ECS.Domain.Enums;

namespace ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordDetailServices
{
    /// <summary>
    /// Validator for GetMedicalRecordDetailRequest
    /// </summary>
    public class GetMedicalRecordDetailRequestValidator : AbstractValidator<GetMedicalRecordDetailRequest>
    {
        public GetMedicalRecordDetailRequestValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString());
        }
    }
}
