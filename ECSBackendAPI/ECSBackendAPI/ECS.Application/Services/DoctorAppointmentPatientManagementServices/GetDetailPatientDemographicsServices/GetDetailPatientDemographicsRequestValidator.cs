using FluentValidation;
using ECS.Domain.Enums;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetDetailPatientDemographicsServices
{
    public class GetDetailPatientDemographicsRequestValidator : AbstractValidator<GetDetailPatientDemographicsRequest>
    {
        public GetDetailPatientDemographicsRequestValidator()
        {
            RuleFor(x => x.PatientId)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString());
        }
    }
}
