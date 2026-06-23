using ECS.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicCreateRoomSevices
{
    /// <summary>
    /// Validator contract ensuring integrity constraints for the room creation request object.
    /// </summary>
    public class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateRoomRequestValidator"/> class.
        /// </summary>
        public CreateRoomRequestValidator()
        {
            RuleFor(x => x.RoomName)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(100)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.RoomType)
                .MaximumLength(50)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }
    }
}
