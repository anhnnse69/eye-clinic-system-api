using ECS.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicEditMedicineServices
{
    /// <summary>
    /// Validator checking constraints on structural updating parameters request payload properties.
    /// </summary>
    public class UpdateMedicineCatalogRequestValidator : AbstractValidator<UpdateMedicineCatalogRequest>
    {
        public UpdateMedicineCatalogRequestValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString());

            RuleFor(x => x.MedicineName)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(200)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }
    }
}
