using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.DeleteClinicFeedbackServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace ECS.Test.Services.ClinicAdminManagementServices.DeleteClinicFeedbackServices
{
    /// <summary>
    /// Unit tests for <see cref="DeleteClinicFeedbackService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line and 100% branch coverage using 4-step layout (Arrange 1, Arrange 2, Act, Assert).
    /// </summary>
    public class DeleteClinicFeedbackServiceTests
    {
        private static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ValidClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid OtherClinicId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid ValidFeedbackId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        private readonly Mock<IRepositoryBaseAsync<Feedback, Guid, AppDbContext>> _feedbackRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly DeleteClinicFeedbackService _sut;

        public DeleteClinicFeedbackServiceTests()
        {
            _sut = new DeleteClinicFeedbackService(
                _feedbackRepoMock.Object,
                _staffClinicRepoMock.Object,
                _httpContextAccessorMock.Object);

            SetupStaffClinic(null);
            SetupFeedbackRepo(null);
        }

        private void SetupHttpContextNull()
        {
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        }

        private void SetupHttpContextUserNull()
        {
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(x => x.User).Returns((ClaimsPrincipal?)null);
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);
        }

        private void SetupHttpContextClaimMissing()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(x => x.User).Returns(claimsPrincipal);
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);
        }

        private void SetupHttpContext(string userIdValue)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userIdValue) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(x => x.User).Returns(claimsPrincipal);
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);
        }

        private void SetupStaffClinic(StaffClinic? staffClinic)
        {
            var list = staffClinic != null ? new List<StaffClinic> { staffClinic } : new List<StaffClinic>();
            var mockQueryable = list.BuildMockDbSet();

            _staffClinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        private void SetupFeedbackRepo(Feedback? feedback)
        {
            var list = feedback != null ? new List<Feedback> { feedback } : new List<Feedback>();
            var mockQueryable = list.BuildMockDbSet();

            _feedbackRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Feedback, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        [Fact]
        public async Task Process_HttpContextNull_ReturnsFailUserInvalid()
        {
            //Arrange 1
            var request = new DeleteClinicFeedbackRequest
            {
                FeedbackId = ValidFeedbackId.ToString()
            };
            var expectedMessage = GeneralCode.APP_MESSAGE_4033.ToString();

            //Arrange 2
            SetupHttpContextNull();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(expectedMessage);
            result.Data.Should().BeFalse();
            _feedbackRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Feedback>()), Times.Never);
            _feedbackRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_HttpContextUserNull_ReturnsFailUserInvalid()
        {
            //Arrange 1
            var request = new DeleteClinicFeedbackRequest
            {
                FeedbackId = ValidFeedbackId.ToString()
            };
            var expectedMessage = GeneralCode.APP_MESSAGE_4033.ToString();

            //Arrange 2
            SetupHttpContextUserNull();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(expectedMessage);
            result.Data.Should().BeFalse();
            _feedbackRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Feedback>()), Times.Never);
            _feedbackRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_UserIdClaimMissing_ReturnsFailUserInvalid()
        {
            //Arrange 1
            var request = new DeleteClinicFeedbackRequest
            {
                FeedbackId = ValidFeedbackId.ToString()
            };
            var expectedMessage = GeneralCode.APP_MESSAGE_4033.ToString();

            //Arrange 2
            SetupHttpContextClaimMissing();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(expectedMessage);
            result.Data.Should().BeFalse();
            _feedbackRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Feedback>()), Times.Never);
            _feedbackRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_UserIdClaimInvalidGuidString_ReturnsFailUserInvalid()
        {
            //Arrange 1
            var request = new DeleteClinicFeedbackRequest
            {
                FeedbackId = ValidFeedbackId.ToString()
            };
            var expectedMessage = GeneralCode.APP_MESSAGE_4033.ToString();

            //Arrange 2
            SetupHttpContext("invalid-guid-string");

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(expectedMessage);
            result.Data.Should().BeFalse();
            _feedbackRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Feedback>()), Times.Never);
            _feedbackRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ClinicNotFound_ReturnsFailClinicNotFound()
        {
            //Arrange 1
            var request = new DeleteClinicFeedbackRequest
            {
                FeedbackId = ValidFeedbackId.ToString()
            };
            var expectedMessage = GeneralCode.APP_MESSAGE_4034.ToString();

            //Arrange 2
            SetupHttpContext(ValidUserId.ToString());
            SetupStaffClinic(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(expectedMessage);
            result.Data.Should().BeFalse();
            _feedbackRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Feedback>()), Times.Never);
            _feedbackRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_InvalidFeedbackIdFormat_ReturnsFailFeedbackNotFound()
        {
            //Arrange 1
            var request = new DeleteClinicFeedbackRequest
            {
                FeedbackId = "invalid-guid-format"
            };
            var staffClinic = new StaffClinic
            {
                UserId = ValidUserId,
                ClinicId = ValidClinicId,
                IsActive = true
            };
            var expectedMessage = GeneralCode.APP_MESSAGE_4035.ToString();

            //Arrange 2
            SetupHttpContext(ValidUserId.ToString());
            SetupStaffClinic(staffClinic);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(expectedMessage);
            result.Data.Should().BeFalse();
            _feedbackRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Feedback>()), Times.Never);
            _feedbackRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_FeedbackNotFound_ReturnsFailFeedbackNotFound()
        {
            //Arrange 1
            var request = new DeleteClinicFeedbackRequest
            {
                FeedbackId = ValidFeedbackId.ToString()
            };
            var staffClinic = new StaffClinic
            {
                UserId = ValidUserId,
                ClinicId = ValidClinicId,
                IsActive = true
            };
            var expectedMessage = GeneralCode.APP_MESSAGE_4035.ToString();

            //Arrange 2
            SetupHttpContext(ValidUserId.ToString());
            SetupStaffClinic(staffClinic);
            SetupFeedbackRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(expectedMessage);
            result.Data.Should().BeFalse();
            _feedbackRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Feedback>()), Times.Never);
            _feedbackRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_FeedbackBelongsToAnotherClinic_ReturnsFailUnauthorized()
        {
            //Arrange 1
            var request = new DeleteClinicFeedbackRequest
            {
                FeedbackId = ValidFeedbackId.ToString()
            };
            var staffClinic = new StaffClinic
            {
                UserId = ValidUserId,
                ClinicId = ValidClinicId,
                IsActive = true
            };
            var feedback = new Feedback
            {
                Id = ValidFeedbackId,
                ClinicId = OtherClinicId,
                IsPublic = true
            };
            var expectedMessage = GeneralCode.APP_MESSAGE_4036.ToString();

            //Arrange 2
            SetupHttpContext(ValidUserId.ToString());
            SetupStaffClinic(staffClinic);
            SetupFeedbackRepo(feedback);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(expectedMessage);
            result.Data.Should().BeFalse();
            _feedbackRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Feedback>()), Times.Never);
            _feedbackRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ValidFeedbackAndClinic_ReturnsSuccessAndSoftDeletes()
        {
            //Arrange 1
            var request = new DeleteClinicFeedbackRequest
            {
                FeedbackId = ValidFeedbackId.ToString()
            };
            var staffClinic = new StaffClinic
            {
                UserId = ValidUserId,
                ClinicId = ValidClinicId,
                IsActive = true
            };
            var feedback = new Feedback
            {
                Id = ValidFeedbackId,
                ClinicId = ValidClinicId,
                IsPublic = true
            };
            var expectedMessage = GeneralCode.APP_MESSAGE_4037.ToString();

            //Arrange 2
            SetupHttpContext(ValidUserId.ToString());
            SetupStaffClinic(staffClinic);
            SetupFeedbackRepo(feedback);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(expectedMessage);
            result.Data.Should().BeTrue();
            feedback.IsPublic.Should().BeFalse();
            _feedbackRepoMock.Verify(x => x.UpdateAsync(feedback), Times.Once);
            _feedbackRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }
    }
}
