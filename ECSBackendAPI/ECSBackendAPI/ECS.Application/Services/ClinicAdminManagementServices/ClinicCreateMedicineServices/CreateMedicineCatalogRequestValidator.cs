using ECS.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicCreateMedicineServices
{
    /// <summary>
    /// Validator engine ensuring structure safety bounds and constraints for medicine creation requests.
    /// </summary>
    public class CreateMedicineCatalogRequestValidator : AbstractValidator<CreateMedicineCatalogRequest>
    {
        /// <summary>
        /// Initializes validation rules mapping directly to domain integrity constraints.
        /// </summary>
        public CreateMedicineCatalogRequestValidator()
        {
            RuleFor(x => x.MedicineName)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(200)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Unit)
                .MaximumLength(50)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.DosageForm)
                .MaximumLength(100)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }
    }
}
