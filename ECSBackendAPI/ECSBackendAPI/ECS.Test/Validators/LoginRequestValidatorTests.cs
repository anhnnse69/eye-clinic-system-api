using FluentValidation.TestHelper;
using ECS.Application.Services.AuthServices.LoginServices;
using ECS.Domain.Enums;
using ECS.Test.MockData;
using Xunit;

namespace ECS.Test.Validators
{
    /// <summary>
    /// Unit tests for <see cref="LoginRequestValidator"/>.
    /// Covers all validation rules: email format, password complexity.
    /// </summary>
    public class LoginRequestValidatorTests
    {
        private readonly LoginRequestValidator _validator = new();

        // ── Valid ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// TC-VAL-LOGIN-01: Valid request → no validation errors.
        /// </summary>
        [Fact]
        public void Validate_ValidRequest_NoErrors()
        {
            var result = _validator.TestValidate(LoginRequestMockData.GetValidRequest());
            result.ShouldNotHaveAnyValidationErrors();
        }

        // ── Email ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// TC-VAL-LOGIN-02: Empty email → required field error.
        /// </summary>
        [Fact]
        public void Validate_EmptyEmail_HasErrorForEmailAddress()
        {
            var result = _validator.TestValidate(LoginRequestMockData.GetEmptyEmailRequest());
            result.ShouldHaveValidationErrorFor(x => x.EmailAddress)
                  .WithErrorMessage(GeneralCode.APP_MESSAGE_4003.ToString());
        }

        /// <summary>
        /// TC-VAL-LOGIN-03: Invalid email format → validation error.
        /// </summary>
        [Fact]
        public void Validate_InvalidEmailFormat_HasErrorForEmailAddress()
        {
            var result = _validator.TestValidate(LoginRequestMockData.GetInvalidEmailFormatRequest());
            result.ShouldHaveValidationErrorFor(x => x.EmailAddress);
        }

        /// <summary>
        /// TC-VAL-LOGIN-04: Email exceeds 150 characters → validation error.
        /// </summary>
        [Fact]
        public void Validate_EmailTooLong_HasErrorForEmailAddress()
        {
            var request = new LoginRequest
            {
                EmailAddress = new string('a', 145) + "@ECS.vn",
                Password = "Test@12345"
            };
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.EmailAddress);
        }

        // ── Password ──────────────────────────────────────────────────────────────

        /// <summary>
        /// TC-VAL-LOGIN-05: Empty password → required field error.
        /// </summary>
        [Fact]
        public void Validate_EmptyPassword_HasErrorForPassword()
        {
            var request = new LoginRequest
            {
                EmailAddress = "test@ECS.vn",
                Password = string.Empty
            };
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Password)
                  .WithErrorMessage(GeneralCode.APP_MESSAGE_4003.ToString());
        }

        /// <summary>
        /// TC-VAL-LOGIN-06: Weak password (no uppercase) → validation error.
        /// </summary>
        [Fact]
        public void Validate_PasswordNoUppercase_HasError()
        {
            var request = new LoginRequest
            {
                EmailAddress = "test@ECS.vn",
                Password = "test@12345"
            };
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        /// <summary>
        /// TC-VAL-LOGIN-07: Password too short (less than 8 chars) → validation error.
        /// </summary>
        [Fact]
        public void Validate_PasswordTooShort_HasError()
        {
            var request = new LoginRequest
            {
                EmailAddress = "test@ECS.vn",
                Password = "T@1a"
            };
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        /// <summary>
        /// TC-VAL-LOGIN-08: Password no special character → validation error.
        /// </summary>
        [Fact]
        public void Validate_PasswordNoSpecialChar_HasError()
        {
            var request = new LoginRequest
            {
                EmailAddress = "test@ECS.vn",
                Password = "TestPass1"
            };
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        /// <summary>
        /// TC-VAL-LOGIN-09: Password no digit → validation error.
        /// </summary>
        [Fact]
        public void Validate_PasswordNoDigit_HasError()
        {
            var request = new LoginRequest
            {
                EmailAddress = "test@ECS.vn",
                Password = "TestPass@abc"
            };
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        /// <summary>
        /// TC-VAL-LOGIN-10: Password exceeds 100 characters → validation error.
        /// </summary>
        [Fact]
        public void Validate_PasswordTooLong_HasError()
        {
            var request = new LoginRequest
            {
                EmailAddress = "test@ECS.vn",
                Password = "Test@1" + new string('a', 100)
            };
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }
    }
}
