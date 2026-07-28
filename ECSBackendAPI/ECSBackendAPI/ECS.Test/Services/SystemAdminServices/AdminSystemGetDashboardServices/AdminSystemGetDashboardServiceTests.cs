using System.Reflection;
using ECS.Application.Services.SystemAdminServices.AdminSystemGetDashboardServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECS.Test.Services.SystemAdminServices.AdminSystemGetDashboardServices
{
    /// <summary>
    /// Unit tests for <see cref="AdminSystemGetDashboardService"/>.
    /// Designed for 100% Line Coverage & 100% Branch Coverage.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class AdminSystemGetDashboardServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly AdminSystemGetDashboardService _sut;

        public AdminSystemGetDashboardServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _sut = new AdminSystemGetDashboardService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Dashboard Core Process Tests

        [Fact]
        public async Task Process_EmptyDatabase_Returns2000WithZeroMetrics()
        {
            // Arrange
            var request = SystemAdminDashboardMockData.GetDefaultRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data.Should().NotBeNull();
            result.Data!.TotalSystemAccounts.Total.Should().Be(0);
            result.Data.OperationalClinics.Active.Should().Be(0);
            result.Data.OperationalClinics.Total.Should().Be(0);
            result.Data.Appointments.Total.Should().Be(0);
            result.Data.RegisteredPatients.Should().Be(0);
            result.Data.PendingClinics.Should().BeEmpty();
            result.Data.TopServices.Should().BeEmpty();
        }

        [Fact]
        public async Task Process_WithSeededData_ReturnsAggregatedMetrics()
        {
            // Arrange
            await SystemAdminDashboardMockData.SeedDashboardDataAsync(_context);
            var request = SystemAdminDashboardMockData.GetDefaultRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            var dashboard = result.Data!;
            dashboard.OperationalClinics.Total.Should().Be(2);
            dashboard.OperationalClinics.Active.Should().Be(1);
            dashboard.RegisteredPatients.Should().Be(1);
            dashboard.PendingClinics.Should().HaveCount(2);
            dashboard.PendingClinics[0].Name.Should().Be("New Clinic A");
            dashboard.PendingClinics[1].Name.Should().Be("New Clinic B");
            dashboard.TotalSystemAccounts.Doctor.Should().Be(1);
            dashboard.TotalSystemAccounts.ClinicAdmin.Should().Be(1);
            dashboard.TotalSystemAccounts.Receptionist.Should().Be(1);
            dashboard.TotalSystemAccounts.SystemAdmin.Should().Be(1);
            dashboard.TotalSystemAccounts.Total.Should().Be(4);
            dashboard.Appointments.Total.Should().Be(3);
            dashboard.Appointments.Pending.Should().Be(1);
            dashboard.Appointments.Completed.Should().Be(1);
            dashboard.Appointments.Cancelled.Should().Be(1);
            dashboard.TopServices.Should().HaveCount(1);
            dashboard.TopServices[0].Name.Should().Be("Comprehensive Eye Exam");
            dashboard.TopServices[0].Count.Should().Be(3);
            dashboard.TopServices[0].Growth.Should().Be("+100.0%");
        }

        [Fact]
        public async Task Process_WithClinicFilter_ExcludesOtherClinicStaffAndAppointments()
        {
            // Arrange
            await SystemAdminDashboardMockData.SeedDashboardDataAsync(_context);
            var request = SystemAdminDashboardMockData.GetClinicFilteredRequest(SystemAdminClinicMockData.ClinicId);

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            var dashboard = result.Data!;
            dashboard.TotalSystemAccounts.ClinicAdmin.Should().Be(1);
            dashboard.TotalSystemAccounts.Receptionist.Should().Be(1);
            dashboard.TotalSystemAccounts.Doctor.Should().Be(0);
            dashboard.TotalSystemAccounts.Total.Should().Be(2);
            dashboard.Appointments.Total.Should().Be(3);
            dashboard.TopServices.Should().HaveCount(1);
        }

        [Fact]
        public async Task Process_WithDateRange_FiltersAppointmentsAndTopServices()
        {
            // Arrange
            await SystemAdminDashboardMockData.SeedDashboardDataAsync(_context);
            var start = DateTime.UtcNow.Date.AddDays(-7);
            var end = DateTime.UtcNow.Date;
            var request = SystemAdminDashboardMockData.GetDateFilteredRequest(start, end);

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            var dashboard = result.Data!;
            dashboard.Appointments.Total.Should().Be(2);
            dashboard.Appointments.Pending.Should().Be(1);
            dashboard.Appointments.Completed.Should().Be(1);
            dashboard.Appointments.Cancelled.Should().Be(0);
            dashboard.TopServices.Should().HaveCount(1);
            dashboard.TopServices[0].Count.Should().Be(2);
            dashboard.TopServices[0].Growth.Should().Be("+100.0%");
        }

        [Fact]
        public async Task Process_PendingClinics_ExcludesNonPendingRequests()
        {
            // Arrange
            await SystemAdminDashboardMockData.SeedDashboardDataAsync(_context);
            var request = SystemAdminDashboardMockData.GetDefaultRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data!.PendingClinics.Should().OnlyContain(p => p.Name == "New Clinic A" || p.Name == "New Clinic B");
            result.Data.PendingClinics.Should().NotContain(p => p.Name == "Approved Clinic");
        }

        #endregion

        #region Branch Coverage Edge-Case Tests

        [Fact]
        public async Task Process_OnlyStartDateProvided_FiltersCorrectly()
        {
            // Covers branch: StartDate != null && EndDate == null
            await SystemAdminDashboardMockData.SeedDashboardDataAsync(_context);
            var request = new AdminSystemGetDashboardRequest
            {
                StartDate = DateTime.UtcNow.Date.AddDays(-7),
                EndDate = null
            };

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data!.Appointments.Total.Should().Be(2);
        }

        [Fact]
        public async Task Process_OnlyEndDateProvided_FiltersCorrectly()
        {
            // Covers branch: StartDate == null && EndDate != null
            await SystemAdminDashboardMockData.SeedDashboardDataAsync(_context);
            var request = new AdminSystemGetDashboardRequest
            {
                StartDate = null,
                EndDate = DateTime.UtcNow.Date.AddDays(-15)
            };

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data!.Appointments.Total.Should().Be(1);
        }

        [Fact]
        public async Task Process_WithPreviousPeriodAppointments_CalculatesPercentageGrowth()
        {
            // Covers branch: Top Service Growth calculation when prevCount > 0
            var clinic = SystemAdminClinicMockData.GetClinicWithPublicationRequest();
            var service = new Service
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic.Id,
                ServiceName = "General Checkup",
                Price = 100000m,
                IsActive = true
            };

            var now = DateTime.UtcNow.Date;
            var start = now.AddDays(-7);
            var end = now;

            var appts = new List<Appointment>
            {
                // Previous period (-14d to -7d): 2 appointments
                new() { Id = Guid.NewGuid(), ServiceId = service.Id, AppointmentDate = now.AddDays(-10), Status = AppointmentStatus.COMPLETED },
                new() { Id = Guid.NewGuid(), ServiceId = service.Id, AppointmentDate = now.AddDays(-9), Status = AppointmentStatus.COMPLETED },
                // Current period (-7d to now): 4 appointments
                new() { Id = Guid.NewGuid(), ServiceId = service.Id, AppointmentDate = now.AddDays(-3), Status = AppointmentStatus.COMPLETED },
                new() { Id = Guid.NewGuid(), ServiceId = service.Id, AppointmentDate = now.AddDays(-2), Status = AppointmentStatus.COMPLETED },
                new() { Id = Guid.NewGuid(), ServiceId = service.Id, AppointmentDate = now.AddDays(-1), Status = AppointmentStatus.COMPLETED },
                new() { Id = Guid.NewGuid(), ServiceId = service.Id, AppointmentDate = now, Status = AppointmentStatus.COMPLETED }
            };

            _context.Clinics.Add(clinic);
            _context.Services.Add(service);
            _context.Appointments.AddRange(appts);
            await _context.SaveChangesAsync();

            var request = new AdminSystemGetDashboardRequest
            {
                StartDate = start,
                EndDate = end
            };

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data!.TopServices.Should().HaveCount(1);
            // ((4 - 2) / 2) * 100 = +100.0%
            result.Data.TopServices[0].Growth.Should().Be("+100.0%");
        }

        #endregion

        #region Private Instance Method Tests (Reflection)

        /// <summary>
        /// Direct test for private instance method NormalizeRoleName using Reflection.
        /// Ensures 100% coverage for nulls, numeric role strings ("0" - "4"),
        /// integer role values, and default string fallbacks.
        /// </summary>
        [Theory]
        [InlineData(null, "")]
        [InlineData("0", "PATIENT")]
        [InlineData(0, "PATIENT")]
        [InlineData("1", "DOCTOR")]
        [InlineData(1, "DOCTOR")]
        [InlineData("2", "CLINIC_ADMIN")]
        [InlineData(2, "CLINIC_ADMIN")]
        [InlineData("3", "RECEPTIONIST")]
        [InlineData(3, "RECEPTIONIST")]
        [InlineData("4", "SYSTEM_ADMIN")]
        [InlineData(4, "SYSTEM_ADMIN")]
        [InlineData("PATIENT", "PATIENT")]
        [InlineData("UNKNOWN_ROLE", "UNKNOWN_ROLE")]
        public void NormalizeRoleName_VariousInputs_ReturnsExpectedNormalizedName(object? inputRole, string expected)
        {
            // Arrange
            var methodInfo = typeof(AdminSystemGetDashboardService)
                .GetMethod("NormalizeRoleName", BindingFlags.NonPublic | BindingFlags.Instance);

            methodInfo.Should().NotBeNull("Method 'NormalizeRoleName' should exist in AdminSystemGetDashboardService");

            // Act - Passed _sut as instance parameter since NormalizeRoleName is an instance method
            var result = methodInfo!.Invoke(_sut, new[] { inputRole });

            // Assert
            result.Should().Be(expected);
        }

        #endregion
    }
}