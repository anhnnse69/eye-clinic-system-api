using ECS.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemDeleteAccountServices
{
    /// <summary>
    /// Independent decoupled validation rules execution block mapping for account deletion requests.
    /// </summary>
    public class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequest>
    {
        /// <summary>
        /// Initializes validation constraints for the incoming delete account request entity.
        /// </summary>
        public DeleteAccountRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString());
        }
    }
}
