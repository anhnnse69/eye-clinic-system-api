using FluentValidation;
using ECS.Domain.Enums;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDemographicsServices
{
    public class ViewPatientDemographicsRequestValidator : AbstractValidator<ViewPatientDemographicsRequest>
    {
        public ViewPatientDemographicsRequestValidator()
        {
            RuleFor(x => x.PatientProfileId)
                .MaximumLength(50)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.RecordType)
                .MaximumLength(30)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Must(type => string.IsNullOrWhiteSpace(type) || Enum.TryParse<RecordType>(type, true, out _))
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.SearchTerm)
                .MaximumLength(200)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .LessThanOrEqualTo(100)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }
    }
}
