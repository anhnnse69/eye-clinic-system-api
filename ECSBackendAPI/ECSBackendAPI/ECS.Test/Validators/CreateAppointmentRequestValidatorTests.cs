using ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentServices;
using ECS.Test.MockData;
using FluentValidation.TestHelper;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Test.Validators
{
    /// <summary>
    /// Unit tests for <see cref="CreateAppointmentRequestValidator"/>.
    /// Covers all validation rules for CreateAppointmentRequest.
    /// </summary>
    public class CreateAppointmentRequestValidatorTests
    {
        private readonly CreateAppointmentRequestValidator _validator = new();

        [Fact]
        public void Validate_ValidRequest_NoErrors()
        {
            var request = CreateAppointmentMockData.GetValidRequest();
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyPatientId_HasError()
        {
            var request = CreateAppointmentMockData.GetValidRequest();
            request.PatientId = Guid.Empty;

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.PatientId);
        }

        [Fact]
        public void Validate_EmptyDoctorId_HasError()
        {
            var request = CreateAppointmentMockData.GetValidRequest();
            request.DoctorId = Guid.Empty;

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.DoctorId);
        }

        [Fact]
        public void Validate_EmptySlotId_HasError()
        {
            var request = CreateAppointmentMockData.GetValidRequest();
            request.SlotId = Guid.Empty;

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.SlotId);
        }

        [Fact]
        public void Validate_NullServiceId_NoError()
        {
            var request = CreateAppointmentMockData.GetValidRequest();
            request.ServiceId = null;

            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_SymptomsExceedsMaxLength_HasError()
        {
            var request = CreateAppointmentMockData.GetValidRequest();
            request.Symptoms = new string('a', 2001);

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Symptoms);
        }

        [Fact]
        public void Validate_PatientAndDoctorAreSame_HasError()
        {
            var sameGuid = Guid.NewGuid();
            var request = CreateAppointmentMockData.GetValidRequest();
            request.PatientId = sameGuid;
            request.DoctorId = sameGuid;

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x);
        }
    }
}
