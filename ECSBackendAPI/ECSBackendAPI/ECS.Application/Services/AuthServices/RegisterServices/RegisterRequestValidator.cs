using ECS.Domain.Enums;
using FluentValidation;

namespace ECS.Application.Services.AuthServices.RegisterServices
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(150).WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .EmailAddress().WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .MaximumLength(150).WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(20).WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches(@"^\+?[0-9]\d{1,14}$").WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MinimumLength(8).WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .MaximumLength(100).WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches("[A-Z]").WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches("[a-z]").WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches("[0-9]").WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .Matches("[^a-zA-Z0-9]").WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .Equal(x => x.Password).WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }
    }
}
