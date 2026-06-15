using FluentValidation;

namespace ECS.Application.Services.AuthServices.ViewAccountInfoServices
{
    public class ViewAccountInfoRequestValidator : AbstractValidator<ViewAccountInfoRequest>
    {
        public ViewAccountInfoRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                    .WithErrorCode(Domain.Enums.GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(Domain.Enums.GeneralCode.APP_MESSAGE_4003.ToString());
        }
    }
}
