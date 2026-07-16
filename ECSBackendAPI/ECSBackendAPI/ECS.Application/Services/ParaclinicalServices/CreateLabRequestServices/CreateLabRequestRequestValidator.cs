using ECS.Domain.Enums;
using FluentValidation;

namespace ECS.Application.Services.ParaclinicalServices.CreateLabRequestServices
{
    public class CreateLabRequestRequestValidator : AbstractValidator<CreateLabRequestRequest>
    {
        private static readonly HashSet<string> AllowedLabTypes =
            new(StringComparer.OrdinalIgnoreCase) { "OCT", "VISUAL_FIELD", "ULTRASOUND", "GENERAL_LAB" };

        private static readonly HashSet<string> AllowedSides =
            new(StringComparer.OrdinalIgnoreCase) { "OD", "OS", "BOTH" };

        public CreateLabRequestRequestValidator()
        {
            RuleFor(x => x.RecordId)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .Must(v => Guid.TryParse(v, out _))
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString())
                    .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.LabType)
                .NotEmpty()
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4003.ToString())
                .Must(v => BeValidLabType(v ?? string.Empty))
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Side)
                .Must(v => BeValidSide(v ?? string.Empty))
                    .When(x => !string.IsNullOrWhiteSpace(x.Side))
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString());

            RuleFor(x => x.Status)
                .Must(v => BeValidStatus(v ?? string.Empty))
                    .When(x => !string.IsNullOrWhiteSpace(x.Status))
                    .WithErrorCode(GeneralCode.APP_MESSAGE_4019.ToString());
        }

        private static bool BeValidLabType(string value) =>
            AllowedLabTypes.Contains(value ?? string.Empty);

        private static bool BeValidSide(string value) =>
            AllowedSides.Contains(value ?? string.Empty);

        private static bool BeValidStatus(string value)
        {
            var statuses = new[] { "REQUESTED", "IN_PROGRESS", "COMPLETED", "CANCELLED" };
            return statuses.Contains(value, StringComparer.OrdinalIgnoreCase);
        }
    }
}