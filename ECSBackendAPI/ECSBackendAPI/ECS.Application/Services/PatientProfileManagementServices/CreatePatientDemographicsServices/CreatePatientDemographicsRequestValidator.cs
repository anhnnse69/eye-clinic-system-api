using ECS.Domain.Enums;
using FluentValidation;

namespace ECS.Application.Services.PatientProfileManagementServices.CreatePatientDemographicsServices
{
    /// <summary>
    /// Validator handling strict data validation and format rules for the create patient demographics request.
    /// </summary>
    public class CreatePatientDemographicsRequestValidator : AbstractValidator<CreatePatientDemographicsRequest>
    {
        public CreatePatientDemographicsRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .MaximumLength(100)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Gender)
                .IsInEnum()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Dob)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .LessThan(DateTime.UtcNow)
                .WithErrorCode(GeneralCode.APP_MESSAGE_4002.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4002.ToString());

            RuleFor(x => x.IdentityNumber)
                .Matches(@"^[0-9]{9}$|^[0-9]{12}$")
                .When(x => !string.IsNullOrEmpty(x.IdentityNumber))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^[0-9]{10}$")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Address)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.Address))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.BhytNumber)
                .MaximumLength(20)
                .When(x => !string.IsNullOrEmpty(x.BhytNumber))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.BloodType)
                .MaximumLength(5)
                .When(x => !string.IsNullOrEmpty(x.BloodType))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Allergies)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.Allergies))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.MedicalHistory)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrEmpty(x.MedicalHistory))
                .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Relationship)
                .NotEmpty()
                .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString());
        }
    }
}
