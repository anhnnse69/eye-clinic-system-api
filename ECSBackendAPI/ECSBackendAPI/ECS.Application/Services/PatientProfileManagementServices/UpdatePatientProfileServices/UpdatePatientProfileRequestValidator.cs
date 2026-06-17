using ECS.Domain.Enums;
using FluentValidation;

namespace ECS.Application.Services.PatientProfileManagementServices.UpdatePatientProfileServices
{
    /// <summary>
    /// Validator handling strict data validation and format rules for the update patient profile request.
    /// </summary>
    public class UpdatePatientProfileRequestValidator : AbstractValidator<UpdatePatientProfileRequest>
    {
        public UpdatePatientProfileRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString());

            RuleFor(x => x.Gender)
                .IsInEnum()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Dob)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .LessThan(DateTime.UtcNow)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4002.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4002.ToString());

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^[0-9]{10}$")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.IdentityNumber)
                .Matches(@"^[0-9]{9}$|^[0-9]{12}$")
                .When(x => !string.IsNullOrEmpty(x.IdentityNumber))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Relationship)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString());
        }
    }
}
