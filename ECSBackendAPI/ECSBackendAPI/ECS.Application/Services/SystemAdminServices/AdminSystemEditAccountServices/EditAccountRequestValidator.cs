using ECS.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemEditAccountServices
{
    /// <summary>
    /// Implements strict validation protocol schemas rule maps targeting the edit account payload criteria.
    /// </summary>
    public class EditAccountRequestValidator : AbstractValidator<EditAccountRequest>
    {
        /// <summary>
        /// Initializes rules checking constraints targeting essential request properties.
        /// </summary>
        public EditAccountRequestValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString());

            RuleFor(x => x.Phone)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .Matches(@"^\+?[0-9]{10,12}$")
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Email)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.Email))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(150)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Role)
                .IsInEnum()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4022.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4022.ToString());
        }
    }
}
