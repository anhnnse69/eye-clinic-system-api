using FluentValidation;
using ECS.Domain.Enums;

namespace ECS.Application.Services.AuthServices.LoginServices
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.EmailAddress)
                .NotEmpty().WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .EmailAddress().WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .MaximumLength(150).WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MinimumLength(8).WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .MaximumLength(100).WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches("[A-Z]").WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches("[a-z]").WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches("[0-9]").WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches("[^a-zA-Z0-9]").WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }
    }
}