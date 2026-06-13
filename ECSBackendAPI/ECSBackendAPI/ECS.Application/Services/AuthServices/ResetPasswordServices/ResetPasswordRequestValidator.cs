using ECS.Domain.Enums;
using FluentValidation;

namespace ECS.Application.Services.AuthServices.ResetPasswordServices
{
    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.ResetToken)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString());

            RuleFor(x => x.Otp)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .Length(6)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4031.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4031.ToString())
                .Matches(@"^\d{6}$")
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4031.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4031.ToString());

            RuleFor(x => x.NewPassword)
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
                .Equal(x => x.NewPassword)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }
    }
}
