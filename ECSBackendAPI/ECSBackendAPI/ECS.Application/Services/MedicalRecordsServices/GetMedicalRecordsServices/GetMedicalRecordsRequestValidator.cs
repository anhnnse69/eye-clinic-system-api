using FluentValidation;
using ECS.Domain.Enums;
using ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordsServices;

namespace ECS.Application.Services.MedicalRecordsServices
{
    /// <summary>
    /// Validator for GetMedicalRecordsRequest
    /// </summary>
    public class GetMedicalRecordsRequestValidator : AbstractValidator<GetMedicalRecordsRequest>
    {
        public GetMedicalRecordsRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                    .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                    .WithMessage(GeneralCode.APP_MESSAGE_4005.ToString());

            RuleFor(x => x.EndDate)
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
                    .When(x => x.EndDate.HasValue)
                    .WithMessage(GeneralCode.APP_MESSAGE_4005.ToString());
        }
    }
}
