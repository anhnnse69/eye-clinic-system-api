using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.PatientAppointmentManagementServices.CreateAppointmentServices
{
    public class CreateAppointmentServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<Appointment, Guid, AppDbContext>> _appointmentRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext>> _slotRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<Service, Guid, AppDbContext>> _serviceRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>> _patientRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly Mock<IDbContextTransaction> _transactionMock = new();
        private readonly CreateAppointmentRequestValidator _validator = new();

        private CreateAppointmentService CreateSut(AppDbContext? context = null)
        {
            context ??= CreateContext();
            return new CreateAppointmentService(
                _appointmentRepoMock.Object,
                _slotRepoMock.Object,
                _doctorRepoMock.Object,
                _serviceRepoMock.Object,
                _patientRepoMock.Object,
                _validator,
                context,
                _httpContextAccessorMock.Object);
        }

        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private void SetupHttpContext(string? userIdString)
        {
            if (userIdString == null)
            {
                _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null!);
                return;
            }

            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(userIdString))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdString));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        }

        private void SetupDoctors(IEnumerable<DoctorProfile> doctors)
        {
            _doctorRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<DoctorProfile, bool>> expression, bool trackChanges) =>
                {
                    var filteredList = doctors.AsQueryable().Where(expression).ToList();
                    return filteredList.BuildMockDbSet().Object;
                });
        }

        private void SetupServices(IEnumerable<Service> services)
        {
            _serviceRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<Service, bool>> expression, bool trackChanges) =>
                {
                    var filteredList = services.AsQueryable().Where(expression).ToList();
                    return filteredList.BuildMockDbSet().Object;
                });
        }

        private void SetupSlots(IEnumerable<TimeSlot> slots)
        {
            _slotRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<TimeSlot, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<TimeSlot, bool>> expression, bool trackChanges) =>
                {
                    var filteredList = slots.AsQueryable().Where(expression).ToList();
                    return filteredList.BuildMockDbSet().Object;
                });
        }

        private void SetupAppointments(IEnumerable<Appointment> appointments)
        {
            _appointmentRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<Appointment, bool>> expression, bool trackChanges) =>
                {
                    var filteredList = appointments.AsQueryable().Where(expression).ToList();
                    return filteredList.BuildMockDbSet().Object;
                });
        }

        private void SetupPatientAccess(AppDbContext context, bool hasAccess)
        {
            if (hasAccess)
            {
                context.UserPatients.Add(new UserPatient
                {
                    UserId = CreateAppointmentMockData.ValidUserId,
                    PatientId = CreateAppointmentMockData.ValidPatientId
                });
            }

            context.PatientProfiles.Add(new PatientProfile
            {
                Id = CreateAppointmentMockData.ValidPatientId,
                UserId = hasAccess ? CreateAppointmentMockData.ValidUserId : Guid.NewGuid(),
                FullName = "Bệnh nhân test",
                Gender = Gender.MALE,
                Dob = DateTime.Now.AddYears(-30)
            });

            context.SaveChanges();

            _patientRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<PatientProfile, bool>> expression, bool trackChanges) =>
                {
                    var filteredList = context.PatientProfiles.AsQueryable().Where(expression).ToList();
                    return filteredList.BuildMockDbSet().Object;
                });
        }

        private void SetupTransaction(bool shouldThrow = false)
        {
            _appointmentRepoMock
                .Setup(r => r.BeginTransactionAsync())
                .ReturnsAsync(_transactionMock.Object);

            if (shouldThrow)
            {
                _appointmentRepoMock
                    .Setup(r => r.CreateAsync(It.IsAny<Appointment>()))
                    .ThrowsAsync(new Exception("Database Save Exception"));
            }
        }

        private async Task InvokePrivateAsync(CreateAppointmentService sut, string methodName, params object[] args)
        {
            var method = typeof(CreateAppointmentService).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();

            var result = method!.Invoke(sut, args);
            if (result is Task task)
            {
                await task;
            }
        }

        private object? InvokePrivate(CreateAppointmentService sut, string methodName, params object[] args)
        {
            var method = typeof(CreateAppointmentService).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();
            return method!.Invoke(sut, args);
        }

        [Fact]
        public async Task Process_InvalidRequest_ReturnsFail4003()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupHttpContext(CreateAppointmentMockData.ValidUserId.ToString());

            var request = CreateAppointmentMockData.GetValidRequest();
            request.PatientId = Guid.Empty;

            var result = await sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4003.ToString());
        }

        [Fact]
        public async Task Process_InvalidUserId_ReturnsFail4033()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupHttpContext("invalid-guid");

            var request = CreateAppointmentMockData.GetValidRequest();
            var result = await sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        [Fact]
        public async Task Process_MissingUserClaim_ReturnsFail4033()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupHttpContext(string.Empty);

            var request = CreateAppointmentMockData.GetValidRequest();
            var result = await sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        [Fact]
        public async Task Process_InactiveDoctor_ReturnsFail4011()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupHttpContext(CreateAppointmentMockData.ValidUserId.ToString());

            var request = CreateAppointmentMockData.GetValidRequest();
            SetupDoctors(new[] { CreateAppointmentMockData.GetDoctorProfile(isDoctorActive: false, isClinicActive: true) });

            var result = await sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        [Fact]
        public async Task Process_InvalidService_ReturnsFail4044()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupHttpContext(CreateAppointmentMockData.ValidUserId.ToString());

            var request = CreateAppointmentMockData.GetValidRequest();
            SetupDoctors(new[] { CreateAppointmentMockData.GetDoctorProfile() });
            SetupServices(Array.Empty<Service>());
            SetupPatientAccess(context, hasAccess: true);

            var result = await sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4044.ToString());
        }

        [Fact]
        public async Task Process_PatientNotAccessible_ReturnsFail4014()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupHttpContext(CreateAppointmentMockData.ValidUserId.ToString());
            var request = CreateAppointmentMockData.GetValidRequest();

            SetupDoctors(new[] { CreateAppointmentMockData.GetDoctorProfile() });
            SetupServices(new[] { CreateAppointmentMockData.GetService() });
            SetupPatientAccess(context, hasAccess: false);

            var result = await sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
        }

        [Fact]
        public async Task Process_SlotDoctorMismatch_ReturnsFail4006()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupHttpContext(CreateAppointmentMockData.ValidUserId.ToString());
            var request = CreateAppointmentMockData.GetValidRequest();

            SetupDoctors(new[] { CreateAppointmentMockData.GetDoctorProfile() });
            SetupServices(new[] { CreateAppointmentMockData.GetService() });
            SetupPatientAccess(context, hasAccess: true);

            var mismatchSlot = CreateAppointmentMockData.GetTimeSlot(doctorId: Guid.NewGuid());
            SetupSlots(new[] { mismatchSlot });

            var result = await sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4006.ToString());
        }

        [Fact]
        public async Task Process_SlotInPast_ReturnsFail4005()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupHttpContext(CreateAppointmentMockData.ValidUserId.ToString());
            var request = CreateAppointmentMockData.GetValidRequest();

            SetupDoctors(new[] { CreateAppointmentMockData.GetDoctorProfile() });
            SetupServices(new[] { CreateAppointmentMockData.GetService() });
            SetupPatientAccess(context, hasAccess: true);

            var pastSlot = CreateAppointmentMockData.GetTimeSlot(startTime: DateTime.Now.AddHours(-2));
            SetupSlots(new[] { pastSlot });

            var result = await sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4005.ToString());
        }

        [Theory]
        [InlineData(SlotStatus.BOOKED, 0, 5)]
        [InlineData(SlotStatus.AVAILABLE, 5, 5)]
        public async Task Process_SlotUnavailableOrFull_ReturnsFail4007(SlotStatus status, int current, int max)
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupHttpContext(CreateAppointmentMockData.ValidUserId.ToString());
            var request = CreateAppointmentMockData.GetValidRequest();

            SetupDoctors(new[] { CreateAppointmentMockData.GetDoctorProfile() });
            SetupServices(new[] { CreateAppointmentMockData.GetService() });
            SetupPatientAccess(context, hasAccess: true);

            var slot = CreateAppointmentMockData.GetTimeSlot(status: status, currentPatients: current, maxPatients: max);
            SetupSlots(new[] { slot });

            var result = await sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4007.ToString());
        }

        [Fact]
        public async Task Process_DuplicateAppointment_ReturnsFail4015()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupHttpContext(CreateAppointmentMockData.ValidUserId.ToString());
            var request = CreateAppointmentMockData.GetValidRequest();

            SetupDoctors(new[] { CreateAppointmentMockData.GetDoctorProfile() });
            SetupServices(new[] { CreateAppointmentMockData.GetService() });
            SetupPatientAccess(context, hasAccess: true);
            SetupSlots(new[] { CreateAppointmentMockData.GetTimeSlot() });
            SetupAppointments(new[]
            {
                new Appointment
                {
                    PatientId = request.PatientId,
                    SlotId = request.SlotId,
                    Status = AppointmentStatus.PENDING
                }
            });

            var result = await sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4015.ToString());
        }

        [Fact]
        public async Task Process_TransactionException_ReturnsFail5001()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupHttpContext(CreateAppointmentMockData.ValidUserId.ToString());
            var request = CreateAppointmentMockData.GetValidRequest();

            SetupDoctors(new[] { CreateAppointmentMockData.GetDoctorProfile() });
            SetupServices(new[] { CreateAppointmentMockData.GetService() });
            SetupPatientAccess(context, hasAccess: true);
            SetupSlots(new[] { CreateAppointmentMockData.GetTimeSlot() });
            SetupAppointments(Enumerable.Empty<Appointment>());
            SetupTransaction(shouldThrow: true);

            var result = await sut.Process(request);

            _transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
        }

        [Fact]
        public async Task Process_SuccessWithService_ReturnsSuccessResponse()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupHttpContext(CreateAppointmentMockData.ValidUserId.ToString());
            var request = CreateAppointmentMockData.GetValidRequest();

            SetupDoctors(new[] { CreateAppointmentMockData.GetDoctorProfile() });
            SetupServices(new[] { CreateAppointmentMockData.GetService() });
            SetupPatientAccess(context, hasAccess: true);

            var slot = CreateAppointmentMockData.GetTimeSlot(currentPatients: 0, maxPatients: 1);
            SetupSlots(new[] { slot });
            SetupAppointments(Enumerable.Empty<Appointment>());
            SetupTransaction();

            var result = await sut.Process(request);

            _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
            slot.Status.Should().Be(SlotStatus.BOOKED);
            result.Data.Should().NotBeNull();
            result.Data.ServiceName.Should().Be("Khám mắt tổng quát");
        }

        [Fact]
        public async Task Process_SuccessWithoutService_UsesDefaultServiceName()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupHttpContext(CreateAppointmentMockData.ValidUserId.ToString());

            var request = CreateAppointmentMockData.GetValidRequest();
            request.ServiceId = null;

            SetupDoctors(new[] { CreateAppointmentMockData.GetDoctorProfile() });
            SetupPatientAccess(context, hasAccess: true);

            var slot = CreateAppointmentMockData.GetTimeSlot(currentPatients: 0, maxPatients: 5);
            SetupSlots(new[] { slot });
            SetupAppointments(Enumerable.Empty<Appointment>());
            SetupTransaction();

            var result = await sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
            result.Data.ServiceName.Should().Be("Khám tổng quát");
            slot.Status.Should().Be(SlotStatus.AVAILABLE);
        }

        [Fact]
        public async Task RetrieveActiveDoctor_InvalidDoctor_SetsErrorState()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupDoctors(new[] { CreateAppointmentMockData.GetDoctorProfile(isDoctorActive: false, isClinicActive: true) });

            var state = CreateExecutionState();
            await InvokePrivateAsync(sut, "RetrieveActiveDoctor", CreateAppointmentMockData.ValidDoctorId, state);

            state.GetType().GetProperty("HasError")!.GetValue(state).Should().Be(true);
            state.GetType().GetProperty("IsDoctorValid")!.GetValue(state).Should().Be(false);
            state.GetType().GetProperty("ErrorCode")!.GetValue(state).Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        [Fact]
        public async Task RetrieveActiveService_WithMissingClinicId_ReturnsWithoutSettingError()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupServices(Array.Empty<Service>());

            var state = CreateExecutionState();
            state.GetType().GetProperty("DoctorProfile")!.SetValue(state, null);

            await InvokePrivateAsync(sut, "RetrieveActiveService", CreateAppointmentMockData.ValidServiceId, state);

            state.GetType().GetProperty("IsServiceValid")!.GetValue(state).Should().Be(true);
            state.GetType().GetProperty("ErrorCode")!.GetValue(state).Should().BeNull();
            state.GetType().GetProperty("Service")!.GetValue(state).Should().BeNull();
        }

        [Fact]
        public async Task RetrieveActiveService_InvalidService_SetsErrorState()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupServices(Array.Empty<Service>());

            var state = CreateExecutionState();
            state.GetType().GetProperty("DoctorProfile")!.SetValue(state, CreateAppointmentMockData.GetDoctorProfile());

            await InvokePrivateAsync(sut, "RetrieveActiveService", CreateAppointmentMockData.ValidServiceId, state);

            state.GetType().GetProperty("HasError")!.GetValue(state).Should().Be(true);
            state.GetType().GetProperty("IsServiceValid")!.GetValue(state).Should().Be(false);
            state.GetType().GetProperty("ErrorCode")!.GetValue(state).Should().Be(GeneralCode.APP_MESSAGE_4044.ToString());
        }

        [Fact]
        public async Task EnsurePatientProfileAccess_NotAccessible_SetsErrorState()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupPatientAccess(context, hasAccess: false);

            var state = CreateExecutionState();
            state.GetType().GetProperty("ActiveUserId")!.SetValue(state, CreateAppointmentMockData.ValidUserId);

            await InvokePrivateAsync(sut, "EnsurePatientProfileAccess", CreateAppointmentMockData.ValidPatientId, state);

            state.GetType().GetProperty("HasError")!.GetValue(state).Should().Be(true);
            state.GetType().GetProperty("IsPatientAccessible")!.GetValue(state).Should().Be(false);
            state.GetType().GetProperty("ErrorCode")!.GetValue(state).Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
        }

        [Fact]
        public async Task CheckDuplicateAppointment_DuplicateFound_SetsErrorState()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupAppointments(new[]
            {
                new Appointment
                {
                    PatientId = CreateAppointmentMockData.ValidPatientId,
                    SlotId = CreateAppointmentMockData.ValidSlotId,
                    Status = AppointmentStatus.PENDING
                }
            });

            var state = CreateExecutionState();
            await InvokePrivateAsync(sut, "CheckDuplicateAppointment", CreateAppointmentMockData.ValidPatientId, CreateAppointmentMockData.ValidSlotId, state);

            state.GetType().GetProperty("HasError")!.GetValue(state).Should().Be(true);
            state.GetType().GetProperty("IsDuplicateValid")!.GetValue(state).Should().Be(false);
            state.GetType().GetProperty("ErrorCode")!.GetValue(state).Should().Be(GeneralCode.APP_MESSAGE_4015.ToString());
        }

        [Fact]
        public void CreateResponse_WhenErrorHasNoCode_UsesDefaultFallback()
        {
            var context = CreateContext();
            var sut = CreateSut(context);
            var state = CreateExecutionState();
            state.GetType().GetProperty("HasError")!.SetValue(state, true);
            state.GetType().GetProperty("ErrorCode")!.SetValue(state, null);

            var response = (ApiResponse<CreateAppointmentResponse>)InvokePrivate(sut, "CreateResponse", state)!;

            response.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        private static object CreateExecutionState()
        {
            var stateType = typeof(CreateAppointmentService).GetNestedType("ExecutionState", BindingFlags.NonPublic);
            stateType.Should().NotBeNull();
            return Activator.CreateInstance(stateType!)!;
        }
    }
}