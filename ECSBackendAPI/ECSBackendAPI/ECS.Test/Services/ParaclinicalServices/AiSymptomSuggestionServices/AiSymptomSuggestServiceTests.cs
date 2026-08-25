using System.Security.Claims;
using ECS.Application.Services.ParaclinicalServices.AiSymptomSuggestionServices;
using ECS.Domain.Enums;
using ECS.Infrastructure.Ai;
using ECS.Infrastructure.Persistence.MongoDb;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace ECS.Test.Services.ParaclinicalServices.AiSymptomSuggestionServices
{
    /// <summary>
    /// Unit tests for <see cref="AiSymptomSuggestService"/>.
    /// Goal: 100% line and branch coverage.
    /// </summary>
    public class AiSymptomSuggestServiceTests
    {
        private readonly Mock<IAiServiceClient> _aiMock = new();
        private readonly Mock<IMongoDbContext> _mongoMock = new();
        private readonly Mock<IMongoCollection<AiSuggestionDocument>> _mongoCollectionMock = new();
        private readonly Mock<IHttpContextAccessor> _httpMock = new();
        private readonly Mock<ILogger<AiSymptomSuggestService>> _loggerMock = new();

        private AiSymptomSuggestService _sut;

        public AiSymptomSuggestServiceTests()
        {
            _mongoMock.Setup(m => m.AiSuggestions).Returns(_mongoCollectionMock.Object);
            SetupHttpContextUser(AiSymptomSuggestMockData.ValidUserId);
            SetupMongoInsertSuccess();

            _sut = BuildSut();
        }

        private AiSymptomSuggestService BuildSut()
        {
            return new AiSymptomSuggestService(
                _aiMock.Object,
                _mongoMock.Object,
                _httpMock.Object,
                _loggerMock.Object);
        }

        private void SetupHttpContextUser(Guid? userId)
        {
            var identity = userId.HasValue
                ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "TestAuth")
                : new ClaimsIdentity();
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpMock.Setup(x => x.HttpContext).Returns(httpContext);
        }

        private void SetupMongoInsertSuccess()
        {
            _mongoCollectionMock
                .Setup(x => x.InsertOneAsync(
                    It.IsAny<AiSuggestionDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private void SetupMongoInsertThrows()
        {
            _mongoCollectionMock
                .Setup(x => x.InsertOneAsync(
                    It.IsAny<AiSuggestionDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Mongo unavailable"));
        }

        // ==================================================================
        // ================== AUTHENTICATION TESTS ==========================
        // ==================================================================

        [Fact]
        public async Task Process_UnauthenticatedUser_ReturnsFail4033()
        {
            // Arrange
            SetupHttpContextUser(null);
            var request = AiSymptomSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
            _aiMock.Verify(a => a.PredictSymptomsAsync(It.IsAny<AiSymptomPredictRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Process_NullHttpContext_ReturnsFail4033()
        {
            // Arrange
            _httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            var request = AiSymptomSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
        }

        // ==================================================================
        // ==================== PREDICT SYMPTOMS TESTS ======================
        // ==================================================================

        [Fact]
        public async Task Process_AiPredictSymptomsThrowsException_ReturnsFail5001()
        {
            // Arrange
            _aiMock
                .Setup(a => a.PredictSymptomsAsync(It.IsAny<AiSymptomPredictRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("FastAPI unreachable"));

            var request = AiSymptomSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
            result.Data.Should().BeNull();
        }

        // ==================================================================
        // ================= PERSISTENCE & MONGO TESTS ======================
        // ==================================================================

        [Fact]
        public async Task Process_MongoInsertThrows_ReturnsFail4001()
        {
            // Arrange
            var aiResp = AiSymptomSuggestMockData.GetSymptomPredictResponse();
            _aiMock.Setup(a => a.PredictSymptomsAsync(It.IsAny<AiSymptomPredictRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResp);

            SetupMongoInsertThrows();
            var request = AiSymptomSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
        }

        // ==================================================================
        // ============= NULL / FALLBACK BRANCH COVERAGE TESTS ==============
        // ==================================================================

        [Fact]
        public async Task Process_NullDatesAndNullAllProbabilities_UsesFallbackValues()
        {
            // Arrange
            var aiResp = new AiSymptomPredictResponse
            {
                TaskId = "task-null-test",
                Status = "completed",
                PredictedDisease = "Cataract",
                Confidence = 0.85,
                AllProbabilities = null,
                CreatedAt = null,
                CompletedAt = null
            };

            _aiMock.Setup(a => a.PredictSymptomsAsync(It.IsAny<AiSymptomPredictRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResp);

            AiSuggestionDocument? insertedDoc = null;
            _mongoCollectionMock
                .Setup(x => x.InsertOneAsync(
                    It.IsAny<AiSuggestionDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<AiSuggestionDocument, InsertOneOptions, CancellationToken>((doc, _, _) => insertedDoc = doc)
                .Returns(Task.CompletedTask);

            var request = AiSymptomSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            insertedDoc.Should().NotBeNull();
            insertedDoc!.ModelName.Should().Be("Symptom_Eye_Disease_19class");
            insertedDoc.AllProbabilities.ElementCount.Should().Be(0);
        }

        // ==================================================================
        // ===================== HAPPY PATH TESTS ===========================
        // ==================================================================

        [Fact]
        public async Task Process_HappyPath_ReturnsSuccessWithFullResponseData()
        {
            // Arrange
            var probs = new Dictionary<string, double> { { "Acute_Glaucoma", 0.92 }, { "Conjunctivitis", 0.08 } };
            var aiResp = AiSymptomSuggestMockData.GetSymptomPredictResponse(
                taskId: "task-sym-999",
                status: "completed",
                predictedDisease: "Acute_Glaucoma",
                confidence: 0.92,
                riskLevel: "HIGH",
                allProbabilities: probs);

            _aiMock.Setup(a => a.PredictSymptomsAsync(It.IsAny<AiSymptomPredictRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResp);

            var request = AiSymptomSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();

            var data = result.Data!;
            data.IsSuccess.Should().BeTrue();
            data.TaskId.Should().Be("task-sym-999");
            data.Status.Should().Be("completed");
            data.PredictedDisease.Should().Be("Acute_Glaucoma");
            data.Confidence.Should().Be(0.92);
            data.RiskLevel.Should().Be("HIGH");
            data.AllProbabilities.Should().BeEquivalentTo(probs);
            data.SuggestionId.Should().NotBeNullOrEmpty();

            _mongoCollectionMock.Verify(
                x => x.InsertOneAsync(It.IsAny<AiSuggestionDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
