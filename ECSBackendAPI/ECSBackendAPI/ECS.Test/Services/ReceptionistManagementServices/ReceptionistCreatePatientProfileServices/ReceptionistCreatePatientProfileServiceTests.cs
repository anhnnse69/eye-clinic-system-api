using System.Linq.Expressions;
using System.Reflection;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreatePatientProfileServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Patient;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.ReceptionistManagementServices.ReceptionistCreatePatientProfileServices
{
    public class ReceptionistCreatePatientProfileServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext>> _patientRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>> _userRepoMock = new();
        private readonly ReceptionistCreatePatientProfileService _sut;

        public ReceptionistCreatePatientProfileServiceTests()
        {
            _sut = new ReceptionistCreatePatientProfileService(
                _patientRepoMock.Object,
                _userRepoMock.Object);
        }

        private void SetupUserRepository(List<User> users)
        {
            var mockDbSet = users.BuildMockDbSet();

            _userRepoMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<bool>()
                ))
                .Returns((Expression<Func<User, bool>> predicate, bool _) =>
                    mockDbSet.Object.Where(predicate));
        }

        [Fact]
        public async Task Process_WithExistingUser_FullData_CreatesPatientProfileAndReturnsSuccess()
        {
            var existingUser = ReceptionistCreatePatientProfileMockData.GetExistingUser();
            SetupUserRepository(new List<User> { existingUser });

            var request = ReceptionistCreatePatientProfileMockData.GetRequestWithExistingUser();

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data.Should().NotBeNull();
            result.Data!.LinkedUserId.Should().Be(existingUser.Id.ToString());
            result.Data.IsAccountAutoCreated.Should().BeFalse();
            result.Data.GeneratedPassword.Should().BeNull();

            _patientRepoMock.Verify(x => x.CreateAsync(It.Is<PatientProfile>(p =>
                p.UserId == existingUser.Id &&
                p.FullName == request.FullName &&
                p.PhoneNumber == request.PhoneNumber &&
                p.Address == request.Address &&
                p.IdentityNumber == request.IdentityNumber &&
                p.BhytNumber == "HS123456789")), Times.Once);

            _patientRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_WithExistingUser_NullOptionalFields_CreatesPatientProfileSuccessfully()
        {
            var existingUser = ReceptionistCreatePatientProfileMockData.GetExistingUser();
            SetupUserRepository(new List<User> { existingUser });

            var request = ReceptionistCreatePatientProfileMockData.GetRequestWithExistingUser();
            request.Address = null;
            request.IdentityNumber = null;
            request.BhytNumber = null;

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data.Should().NotBeNull();

            _patientRepoMock.Verify(x => x.CreateAsync(It.Is<PatientProfile>(p =>
                p.Address == null &&
                p.IdentityNumber == null &&
                p.BhytNumber == null)), Times.Once);
        }

        [Fact]
        public async Task Process_HasAccountTrue_ButSelectedUserIdIsNull_ThrowsArgumentException()
        {
            SetupUserRepository(new List<User>());

            var request = ReceptionistCreatePatientProfileMockData.GetRequestWithExistingUser();
            request.SelectedUserId = null;

            Func<Task> act = async () => await _sut.Process(request);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("APP_MESSAGE_4019");
        }

        [Fact]
        public async Task Process_HasAccountTrue_UserNotFound_ThrowsKeyNotFoundException()
        {
            SetupUserRepository(new List<User>());

            var request = ReceptionistCreatePatientProfileMockData.GetRequestWithExistingUser();
            request.SelectedUserId = ReceptionistCreatePatientProfileMockData.NonExistentUserId;

            Func<Task> act = async () => await _sut.Process(request);

            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("USER_NOT_FOUND");
        }

        [Theory]
        [InlineData("newpatient@example.com")]
        [InlineData(null)]
        public async Task Process_AutoCreateAccount_CreatesUserAndPatientProfile_ReturnsSuccessWithPassword(string? email)
        {
            SetupUserRepository(new List<User>());

            var request = ReceptionistCreatePatientProfileMockData.GetRequestWithAutoAccountCreation(email);

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data.Should().NotBeNull();
            result.Data!.IsAccountAutoCreated.Should().BeTrue();
            result.Data.LinkedUserId.Should().NotBeNullOrEmpty();
            result.Data.GeneratedPassword.Should().NotBeNullOrEmpty();
            result.Data.GeneratedPassword!.Length.Should().Be(8);

            _userRepoMock.Verify(x => x.CreateAsync(It.Is<User>(u =>
                u.FullName == request.FullName &&
                u.Phone == request.PhoneNumber &&
                u.Email == (email != null ? email.ToLower() : null))), Times.Once);

            _patientRepoMock.Verify(x => x.CreateAsync(It.Is<PatientProfile>(p =>
                p.FullName == request.FullName &&
                p.PhoneNumber == request.PhoneNumber)), Times.Once);

            _patientRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public void GenerateRandomTextPassword_WithLengthLessThan8_ResetsLengthTo8()
        {
            var methodInfo = typeof(ReceptionistCreatePatientProfileService)
                .GetMethod("GenerateRandomTextPassword", BindingFlags.NonPublic | BindingFlags.Instance);

            methodInfo.Should().NotBeNull();

            var result = (string)methodInfo!.Invoke(_sut, new object[] { 5 })!;

            result.Should().NotBeNull();
            result.Length.Should().Be(8);
        }

        [Fact]
        public void BuildResponsePayload_PatientUserIdIsNull_DoesNotAttemptToRemoveFromDictionary()
        {
            var methodInfo = typeof(ReceptionistCreatePatientProfileService)
                .GetMethod("BuildResponsePayload", BindingFlags.NonPublic | BindingFlags.Instance);

            methodInfo.Should().NotBeNull();

            var patientProfile = new PatientProfile
            {
                Id = Guid.NewGuid(),
                UserId = null,
                FullName = "Test Patient",
                CreatedAt = DateTime.UtcNow
            };

            var request = ReceptionistCreatePatientProfileMockData.GetRequestWithExistingUser();

            var result = (ReceptionistCreatePatientProfileResponse)methodInfo!.Invoke(_sut, new object[] { patientProfile, request })!;

            result.Should().NotBeNull();
            result.LinkedUserId.Should().BeNull();
            result.GeneratedPassword.Should().BeNull();
        }
    }
}