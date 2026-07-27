using ECS.Application.Services.AuthServices.ChangePasswordServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;

namespace ECS.Test.Services.AuthServices
{
    public class ChangePasswordServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>> _repositoryMock;
        private readonly Mock<IValidator<ChangePasswordRequest>> _validatorMock;
        private readonly ChangePasswordService _service;

        public ChangePasswordServiceTests()
        {
            _repositoryMock = new Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>>();
            _validatorMock = new Mock<IValidator<ChangePasswordRequest>>();
            _service = new ChangePasswordService(_repositoryMock.Object, _validatorMock.Object);
        }

        private void SetupRepository(User? user)
        {
            var users = user == null ? new List<User>() : new List<User> { user };
            var mockQueryable = users.BuildMockDbSet<User>();

            _repositoryMock
                .Setup(repository => repository.FindByCondition(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        private void SetupValidation(bool isValid, string errorCode = "")
        {
            var result = isValid
                ? new ValidationResult()
                : new ValidationResult(new List<ValidationFailure>
                {
                    new ValidationFailure(nameof(ChangePasswordRequest.CurrentPassword), "Invalid request")
                    {
                        ErrorCode = string.IsNullOrWhiteSpace(errorCode)
                            ? GeneralCode.APP_MESSAGE_4003.ToString()
                            : errorCode
                    }
                });

            _validatorMock
                .Setup(validator => validator.Validate(It.IsAny<ChangePasswordRequest>()))
                .Returns(result);
        }

        [Fact]
        public async Task Process_InvalidRequest_ReturnsValidationErrorAndSkipsPasswordVerification()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            var request = new ChangePasswordRequest
            {
                CurrentPassword = string.Empty,
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };
            var expectedCode = GeneralCode.APP_MESSAGE_4003.ToString();

            //Arrange 2
            SetupValidation(false, expectedCode);
            SetupRepository(null);

            //Act
            var result = await _service.Process(userId, request);

            //Assert
            result.CodeMessage.Should().Be(expectedCode);
            result.Data.Should().BeNull();
            _repositoryMock.Verify(repository => repository.UpdateAsync(It.IsAny<User>()), Times.Never);
            _repositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ValidRequestUserNotFound_ReturnsCurrentPasswordError()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "Current@123",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            //Arrange 2
            SetupValidation(true);
            SetupRepository(null);

            //Act
            var result = await _service.Process(userId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4039.ToString());
            result.Data.Should().BeNull();
            _repositoryMock.Verify(repository => repository.UpdateAsync(It.IsAny<User>()), Times.Never);
            _repositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ValidRequestWrongCurrentPassword_ReturnsCurrentPasswordError()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Stored@123"),
                IsActive = true
            };
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "Wrong@123",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            //Arrange 2
            SetupValidation(true);
            SetupRepository(user);

            //Act
            var result = await _service.Process(userId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4039.ToString());
            result.Data.Should().BeNull();
            _repositoryMock.Verify(repository => repository.UpdateAsync(It.IsAny<User>()), Times.Never);
            _repositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ValidRequestCorrectCurrentPassword_UpdatesPasswordAndReturnsSuccess()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Current@123"),
                IsActive = true
            };
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "Current@123",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            //Arrange 2
            SetupValidation(true);
            SetupRepository(user);
            _repositoryMock
                .Setup(repository => repository.UpdateAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);
            _repositoryMock
                .Setup(repository => repository.SaveChangesAsync())
                .ReturnsAsync(1);

            //Act
            var result = await _service.Process(userId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2008.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.IsSuccess.Should().BeTrue();
            result.Data.Message.Should().Be(GeneralCode.APP_MESSAGE_2008.ToString());
            BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash).Should().BeTrue();
            user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            _repositoryMock.Verify(repository => repository.UpdateAsync(user), Times.Once);
            _repositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
        }
    }
}
