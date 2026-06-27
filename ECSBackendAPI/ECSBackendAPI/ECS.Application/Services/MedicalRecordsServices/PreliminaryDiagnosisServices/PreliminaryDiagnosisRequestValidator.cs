using ECS.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ECS.Application.Services.MedicalRecordsServices.PreliminaryDiagnosisServices
{
    /// <summary>
    /// Validator for PreliminaryDiagnosisRequest.
    /// </summary>
    public class PreliminaryDiagnosisRequestValidator : AbstractValidator<PreliminaryDiagnosisRequest>
    {
        private readonly ILogger<PreliminaryDiagnosisRequestValidator> _logger;

        public PreliminaryDiagnosisRequestValidator(ILogger<PreliminaryDiagnosisRequestValidator> logger)
        {
            _logger = logger;
            
            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString())
                .Must(BeAValidGuid)
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.UrgencyLevel)
                .IsInEnum()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage("UrgencyLevel must be Low, Medium, High, or Emergency");

            RuleFor(x => x.PainLevel)
                .InclusiveBetween(1, 10)
                .When(x => x.PainLevel.HasValue)
                    .WithMessage("Pain level must be between 1 and 10");
        }

        private static bool BeAValidGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }
    }
}
