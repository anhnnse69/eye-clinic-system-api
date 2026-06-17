using ECS.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.DeactivateServiceServices
{
    /// <summary>
    /// Validator checking parameter rules for deactivating a clinic service request.
    /// </summary>
    public class DeactivateServiceRequestValidator : AbstractValidator<DeactivateServiceRequest>
    {
        /// <summary>
        /// Initializes validation constraint boundaries mapping specific structural system properties.
        /// </summary>
        public DeactivateServiceRequestValidator()
        {
            RuleFor(x => x.ServiceId)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString());
        }
    }
}
