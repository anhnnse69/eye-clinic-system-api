using ECS.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicDeleteMedicineServices
{
    /// <summary>
    /// Validator providing business constraint verification rules for <see cref="DeleteMedicineCatalogRequest"/>.
    /// </summary>
    public class DeleteMedicineCatalogRequestValidator : AbstractValidator<DeleteMedicineCatalogRequest>
    {
        /// <summary>
        /// Initializes rules ensuring input boundaries are structurally coherent.
        /// </summary>
        public DeleteMedicineCatalogRequestValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString());
        }
    }
}
