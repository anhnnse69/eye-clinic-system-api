using FluentValidation;
using ECS.Domain.Enums;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CompleteQueueServices
{
    /// <summary>
    /// Validator for CompleteQueueRequest.
    /// </summary>
    public class CompleteQueueRequestValidator : AbstractValidator<CompleteQueueRequest>
    {
        public CompleteQueueRequestValidator()
        {
            RuleFor(x => x.QueueId)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .Must(BeAValidGuid)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }

        private static bool BeAValidGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }
    }
}
