using ECS.Domain.Enums;
using FluentValidation;

namespace ECS.Application.Services.AuthServices.RegisterServices
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(150)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Email)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .EmailAddress()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .MaximumLength(150)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Phone)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(20)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches(@"^\+?[0-9]\d{1,14}$")
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Password)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MinimumLength(8)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .MaximumLength(100)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches("[A-Z]")
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches("[a-z]")
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches("[0-9]")
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches("[^a-zA-Z0-9]")
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .Equal(x => x.Password)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }
    }
}
