using ECS.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.CreateServiceServices
{
    /// <summary>
    /// Validator rule configurations for checking incoming <see cref="CreateServiceRequest"/> parameters.
    /// </summary>
    public class CreateServiceRequestValidator : AbstractValidator<CreateServiceRequest>
    {
        /// <summary>
        /// Initializes validation profiles mapping domain constraints against request properties.
        /// </summary>
        public CreateServiceRequestValidator()
        {
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
