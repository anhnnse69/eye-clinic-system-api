using ECS.Domain.Enums;
using FluentValidation;

namespace ECS.Application.Services.AuthServices.RegisterClinicApplicationServices
{
    public class RegisterClinicApplicationRequestValidator
        : AbstractValidator<RegisterClinicApplicationRequest>
    {
        public RegisterClinicApplicationRequestValidator()
        {
            RuleFor(x => x.ClinicName)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(255)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString());
            RuleFor(x => x.ClinicAddress)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(500)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString());
            RuleFor(x => x.ContactName)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(150)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString());
            RuleFor(x => x.ContactPhone)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(20)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches(@"^\+?[0-9]\d{1,14}$")
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString());
            RuleFor(x => x.ContactEmail)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .EmailAddress()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .MaximumLength(150)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString());
            RuleFor(x => x.BusinessLicenseUrl)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(1000)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString());
        }
    }
}