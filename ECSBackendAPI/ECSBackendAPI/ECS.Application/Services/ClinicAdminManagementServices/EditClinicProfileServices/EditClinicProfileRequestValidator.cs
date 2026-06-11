using FluentValidation;
using ECS.Domain.Enums;

namespace ECS.Application.Services.ClinicAdminManagementServices.EditClinicProfileServices
{
    /// <summary>
    /// Validates clinic profile update request payload.
    /// </summary>
    public class EditClinicProfileRequestValidator : AbstractValidator<EditClinicProfileRequest>
    {
        /// <summary>
        /// Initializes validation rules for clinic profile update requests.
        /// </summary>
        public EditClinicProfileRequestValidator()
        {
            RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
            .MaximumLength(200)
            .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(500)
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Phone)
                .NotEmpty()
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(20)
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Email)
                .EmailAddress()
                .When(x => !string.IsNullOrWhiteSpace(x.Email))
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString())
                .MaximumLength(150)
                .When(x => !string.IsNullOrWhiteSpace(x.Email))
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.LogoUrl)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.LogoUrl))
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrWhiteSpace(x.Description))
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }
    }
}
