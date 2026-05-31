using FluentAssertions;
using ECS.API.Controllers.AuthController;
using ECS.Application.Common.Response;
using ECS.Application.Services.AuthServices.LoginServices;
using ECS.Domain.Enums;
using ECS.Test.MockData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ECS.Test.Controllers.AuthControllers
{
    /// <summary>
    /// Unit tests for <see cref="LoginController"/>.
    /// Covers: HTTP status codes, response structure from controller layer.
    /// </summary>
    public class LoginControllerTests
    {
        private readonly Mock<ILoginService> _loginServiceMock;
        private readonly LoginController _controller;

        public LoginControllerTests()
        {
            _loginServiceMock = new Mock<ILoginService>();
            _controller = new LoginController(_loginServiceMock.Object);
        }

        // ── Test Cases ────────────────────────────────────────────────────────────

        /// <summary>
        /// TC-CTRL-LOGIN-01: Valid request → service returns success → controller returns 200 OK.
        /// </summary>
        [Fact]
        public async Task Login_ValidRequest_Returns200Ok()
        {
            // Arrange
            var request = LoginRequestMockData.GetValidRequest();
            var serviceResponse = ApiResponse<LoginResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                new LoginResponse("fake.jwt.token")
            );

            _loginServiceMock.Setup(s => s.Proccess(It.IsAny<LoginRequest>()))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        /// <summary>
        /// TC-CTRL-LOGIN-02: Valid request → response body contains token.
        /// </summary>
        [Fact]
        public async Task Login_ValidRequest_ResponseBodyContainsToken()
        {
            // Arrange
            var request = LoginRequestMockData.GetValidRequest();
            const string expectedToken = "eyJhbGci.payload.signature";
            var serviceResponse = ApiResponse<LoginResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                new LoginResponse(expectedToken)
            );

            _loginServiceMock.Setup(s => s.Proccess(It.IsAny<LoginRequest>()))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var body = okResult.Value.Should().BeOfType<ApiResponse<LoginResponse>>().Subject;
            body.Data!.Token.Should().Be(expectedToken);
            body.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
        }

        /// <summary>
        /// TC-CTRL-LOGIN-03: Wrong password → service returns fail → controller returns 400 BadRequest.
        /// </summary>
        [Fact]
        public async Task Login_WrongPassword_Returns400BadRequest()
        {
            // Arrange
            var request = LoginRequestMockData.GetWrongPasswordRequest();
            var serviceResponse = ApiResponse<LoginResponse>.Fail(
                GeneralCode.APP_MESSAGE_4016.ToString()
            );

            _loginServiceMock.Setup(s => s.Proccess(It.IsAny<LoginRequest>()))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        /// <summary>
        /// TC-CTRL-LOGIN-04: User not found → service returns fail → response body has 4016 code.
        /// </summary>
        [Fact]
        public async Task Login_UserNotFound_ResponseBodyHas4016Code()
        {
            // Arrange
            var request = LoginRequestMockData.GetNotFoundEmailRequest();
            var serviceResponse = ApiResponse<LoginResponse>.Fail(
                GeneralCode.APP_MESSAGE_4016.ToString()
            );

            _loginServiceMock.Setup(s => s.Proccess(It.IsAny<LoginRequest>()))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var body = badResult.Value.Should().BeOfType<ApiResponse<LoginResponse>>().Subject;
            body.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4016.ToString());
            body.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-CTRL-LOGIN-05: Controller calls login service exactly once per request.
        /// </summary>
        [Fact]
        public async Task Login_AnyRequest_CallsServiceExactlyOnce()
        {
            // Arrange
            var request = LoginRequestMockData.GetValidRequest();
            _loginServiceMock.Setup(s => s.Proccess(It.IsAny<LoginRequest>()))
                .ReturnsAsync(ApiResponse<LoginResponse>.Fail(GeneralCode.APP_MESSAGE_4016.ToString()));

            // Act
            await _controller.Login(request);

            // Assert
            _loginServiceMock.Verify(s => s.Proccess(It.IsAny<LoginRequest>()), Times.Once);
        }

        /// <summary>
        /// TC-CTRL-LOGIN-06: Success response → Meta is null (no pagination for login).
        /// </summary>
        [Fact]
        public async Task Login_ValidRequest_MetaIsNull()
        {
            // Arrange
            var request = LoginRequestMockData.GetValidRequest();
            var serviceResponse = ApiResponse<LoginResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                new LoginResponse("token")
            );

            _loginServiceMock.Setup(s => s.Proccess(It.IsAny<LoginRequest>()))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var body = okResult.Value.Should().BeOfType<ApiResponse<LoginResponse>>().Subject;
            body.Meta.Should().BeNull();
        }
    }
}
