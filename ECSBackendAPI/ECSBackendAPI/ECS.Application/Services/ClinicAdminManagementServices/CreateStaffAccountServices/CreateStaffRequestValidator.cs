using FluentValidation;
using ECS.Domain.Enums;

namespace ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices
{
    /// <summary>
    /// Validator handling strict data validation and format rules for the create staff request.
    /// </summary>
    public class CreateStaffRequestValidator : AbstractValidator<CreateStaffRequest>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CreateStaffRequestValidator"/> with pre-defined system message codes.
        /// </summary>
        public CreateStaffRequestValidator()
        {
            RuleFor(x => x.Phone)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .Matches(@"^[0-9]{10}$")
                .WithErrorCode(GeneralCode.APP_MESSAGE_4001.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4001.ToString());

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .EmailAddress()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString());

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MinimumLength(8)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.StaffRole)
                .IsInEnum()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }
    }
}