using ECS.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.EditServiceServices
{
    /// <summary>
    /// Validator checking constraints and rules layout for editing a clinic service.
    /// </summary>
    public class EditServiceRequestValidator : AbstractValidator<EditServiceRequest>
    {
        /// <summary>
        /// Initializes validation expressions for incoming service modification payloads.
        /// </summary>
        public EditServiceRequestValidator()
        {
            RuleFor(x => x.ServiceId)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString());

            RuleFor(x => x.ServiceName)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(200)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0)
                    .When(x => x.Price.HasValue)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.DurationMinutes)
                .GreaterThan(0)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }
    }
}
