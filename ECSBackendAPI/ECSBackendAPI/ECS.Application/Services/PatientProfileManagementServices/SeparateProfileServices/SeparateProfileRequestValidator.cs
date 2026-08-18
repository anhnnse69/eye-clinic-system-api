using FluentValidation;

namespace ECS.Application.Services.PatientProfileManagementServices.SeparateProfileServices
{
    /// <summary>
    /// Validator for SeparateProfileRequest
    /// </summary>
    public class SeparateProfileRequestValidator : AbstractValidator<SeparateProfileRequest>
    {
        public SeparateProfileRequestValidator()
        {
            RuleFor(x => x.ChildPatientProfileId)
                .NotEmpty()
                .WithErrorCode("INVALID_PROFILE_ID")
                .WithMessage("Child patient profile ID must not be empty");

            RuleFor(x => x.NewEmail)
                .NotEmpty()
                .WithErrorCode("EMPTY_EMAIL")
                .WithMessage("New email address is required")
                .EmailAddress()
                .WithErrorCode("INVALID_EMAIL_FORMAT")
                .WithMessage("New email address is not in valid format");

            RuleFor(x => x.NewPhone)
                .NotEmpty()
                .WithErrorCode("EMPTY_PHONE")
                .WithMessage("New phone number is required")
                .Matches(@"^\d{10,}$")
                .WithErrorCode("INVALID_PHONE_FORMAT")
                .WithMessage("Phone number must be at least 10 digits");
        }
    }
}
