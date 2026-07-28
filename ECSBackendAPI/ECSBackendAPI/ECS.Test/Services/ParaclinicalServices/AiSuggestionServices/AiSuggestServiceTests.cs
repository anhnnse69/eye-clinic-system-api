using System.Security.Claims;
using ECS.Application.Services.ParaclinicalServices.AiSuggestionServices;
using ECS.Domain.Enums;
using ECS.Infrastructure.Ai;
using ECS.Infrastructure.Persistence.MongoDb;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace ECS.Test.Services.ParaclinicalServices.AiSuggestionServices
{
    /// <summary>
    /// Unit tests for <see cref="AiSuggestService"/>.
    /// Goal: 100% line AND branch coverage.
    /// </summary>
    public class AiSuggestServiceTests
    {
        private readonly Mock<IAiServiceClient> _aiMock = new();
        private readonly Mock<IMongoDbContext> _mongoMock = new();
        private readonly Mock<IMongoCollection<AiSuggestionDocument>> _mongoCollectionMock = new();
        private readonly Mock<IHttpContextAccessor> _httpMock = new();
        private readonly Mock<ILogger<AiSuggestService>> _loggerMock = new();

        private AiServiceOptions _options = AiSuggestMockData.GetAiOptions();
        private AiSuggestService _sut;

        public AiSuggestServiceTests()
        {
            _mongoMock.Setup(m => m.AiSuggestions).Returns(_mongoCollectionMock.Object);
            SetupHttpContextUser(AiSuggestMockData.ValidUserId);
            SetupMongoInsertSuccess();

            _sut = BuildSut();
        }

        private AiSuggestService BuildSut()
        {
            var optionsWrapper = Options.Create(_options);
            return new AiSuggestService(
                _aiMock.Object,
                _mongoMock.Object,
                optionsWrapper,
                _httpMock.Object,
                _loggerMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // Setup Helpers
        // ─────────────────────────────────────────────────────────────────

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

        /// <summary>
        /// TC-01: No NameIdentifier claim on HttpContext.User -> APP_MESSAGE_4033.
        /// </summary>
        [Fact]
        public async Task Process_UnauthenticatedUser_ReturnsFail4033()
        {
            // Arrange
            SetupHttpContextUser(null);
            var request = AiSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
            _aiMock.Verify(a => a.SubmitPredictionAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// TC-02: HttpContext itself is null -> APP_MESSAGE_4033.
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_ReturnsFail4033()
        {
            // Arrange
            _httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            var request = AiSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
        }

        // ==================================================================
        // ==================== SUBMIT TO AI TESTS ==========================
        // ==================================================================

        /// <summary>
        /// TC-03: AI service SubmitPredictionAsync throws exception -> APP_MESSAGE_5001.
        /// </summary>
        [Fact]
        public async Task Process_SubmitToAiThrowsException_ReturnsFail5001()
        {
            // Arrange
            _aiMock
                .Setup(a => a.SubmitPredictionAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("FastAPI unreachable"));

            var request = AiSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
            result.Data.Should().BeNull();
        }

        // ==================================================================
        // =================== POLLING & TERMINAL TESTS =====================
        // ==================================================================

        /// <summary>
        /// TC-04: Task is initially in non-terminal status ('pending') and becomes 'completed' on second poll attempt.
        /// </summary>
        [Fact]
        public async Task Process_PollsUntilCompleted_ReturnsSuccess()
        {
            // Arrange
            var initialTask = AiSuggestMockData.GetPredictTask(status: "pending");
            var completedTask = AiSuggestMockData.GetPredictTask(status: "completed");

            _aiMock.Setup(a => a.SubmitPredictionAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(initialTask);

            _aiMock.Setup(a => a.GetTaskAsync(initialTask.TaskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(completedTask);

            var request = AiSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.IsSuccess.Should().BeTrue();
            result.Data.Status.Should().Be("completed");
            _aiMock.Verify(a => a.GetTaskAsync(initialTask.TaskId, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// TC-05: Task reaches terminal status 'failed' -> Process finishes polling and builds response.
        /// </summary>
        [Fact]
        public async Task Process_TaskFailed_ReturnsResponseWithIsSuccessFalse()
        {
            // Arrange
            var failedTask = AiSuggestMockData.GetPredictTask(status: "FAILED", errorCode: "ERR_MODEL", errorMessage: "Inference failed");

            _aiMock.Setup(a => a.SubmitPredictionAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(failedTask);

            var request = AiSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.IsSuccess.Should().BeFalse();
            result.Data.Status.Should().Be("FAILED");
            result.Data.ErrorCode.Should().Be("ERR_MODEL");
            result.Data.ErrorMessage.Should().Be("Inference failed");

            // Since it's already terminal upon submission, GetTaskAsync shouldn't be called
            _aiMock.Verify(a => a.GetTaskAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// TC-06: Task remains non-terminal and reaches MaxPollAttempts limit -> Polling stops.
        /// </summary>
        [Fact]
        public async Task Process_PollExceedsMaxAttempts_StopsPolling()
        {
            // Arrange
            _options = AiSuggestMockData.GetAiOptions(maxPollAttempts: 2);
            _sut = BuildSut();

            var pendingTask = AiSuggestMockData.GetPredictTask(status: "processing");

            _aiMock.Setup(a => a.SubmitPredictionAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pendingTask);

            _aiMock.Setup(a => a.GetTaskAsync(pendingTask.TaskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pendingTask);

            var request = AiSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.IsSuccess.Should().BeFalse();
            result.Data.Status.Should().Be("processing");

            // GetTaskAsync should be called exactly MaxPollAttempts times (2)
            _aiMock.Verify(a => a.GetTaskAsync(pendingTask.TaskId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        /// <summary>
        /// TC-07: Exception thrown during GetTaskAsync polling -> Exception logged, loop continues until max attempts.
        /// </summary>
        [Fact]
        public async Task Process_PollThrowsException_SwallowsAndContinuesPolling()
        {
            // Arrange
            _options = AiSuggestMockData.GetAiOptions(maxPollAttempts: 2);
            _sut = BuildSut();

            var pendingTask = AiSuggestMockData.GetPredictTask(status: "pending");

            _aiMock.Setup(a => a.SubmitPredictionAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pendingTask);

            _aiMock.Setup(a => a.GetTaskAsync(pendingTask.TaskId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Network timeout during poll"));

            var request = AiSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            _aiMock.Verify(a => a.GetTaskAsync(pendingTask.TaskId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        // ==================================================================
        // ================= PERSISTENCE & MONGO TESTS ======================
        // ==================================================================

        /// <summary>
        /// TC-08: Mongo InsertOneAsync throws exception -> Logged as error, non-fatal, state.Document remains null -> Returns fail 4001.
        /// </summary>
        [Fact]
        public async Task Process_MongoInsertThrows_NonFatalButBuildResponseReturnsFail4001()
        {
            // Arrange
            var task = AiSuggestMockData.GetPredictTask(status: "completed");
            _aiMock.Setup(a => a.SubmitPredictionAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            SetupMongoInsertThrows();
            var request = AiSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            // BuildResponse checks `state.Document == null` and returns Fail(APP_MESSAGE_4001)
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
        }

        // ==================================================================
        // ============= NULL / FALLBACK BRANCH COVERAGE TESTS ==============
        // ==================================================================
        /// <summary>
        /// TC-09: Task ModelVersion is null -> Fallback to "unknown".
        /// Task CreatedAt is null -> Fallback to DateTime.UtcNow.
        /// Task AllProbabilities is null -> Returns empty BsonDocument.
        /// </summary>
        [Fact]
        public async Task Process_TaskModelVersionAndCreatedAtAndAllProbabilitiesNull_UsesFallbackValues()
        {
            // Arrange: Tạo task trực tiếp với các thuộc tính null để test nhánh fallback
            var taskWithNulls = new AiPredictTask
            {
                TaskId = "task-null-test",
                Status = "completed",
                ModelVersion = null,
                CreatedAt = null,
                AllProbabilities = null
            };

            _aiMock.Setup(a => a.SubmitPredictionAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(taskWithNulls);

            AiSuggestionDocument? insertedDoc = null;
            _mongoCollectionMock
                .Setup(x => x.InsertOneAsync(
                    It.IsAny<AiSuggestionDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<AiSuggestionDocument, InsertOneOptions, CancellationToken>((doc, _, _) => insertedDoc = doc)
                .Returns(Task.CompletedTask);

            var request = AiSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            insertedDoc.Should().NotBeNull();
            insertedDoc!.ModelVersion.Should().Be("unknown");
            insertedDoc.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            insertedDoc.AllProbabilities.Should().NotBeNull();
            insertedDoc.AllProbabilities.ElementCount.Should().Be(0);
        }

        /// <summary>
        /// TC-10: BuildResponse called when ErrorCode is null in ExecutionState -> Returns default error code APP_MESSAGE_4001.
        /// </summary>
        [Fact]
        public async Task Process_HasErrorTrueWithNullErrorCode_ReturnsDefaultFail4001()
        {
            // Arrange
            // Force state.HasError = true without ErrorCode by causing submit failure without setting ErrorCode directly,
            // or testing via Submit failure which sets 5001.
            // Here we verify BuildResponse fallback path when HasError = true and ErrorCode = null.
            _aiMock.Setup(a => a.SubmitPredictionAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AiPredictTask)null!); // Will cause task to be null in state

            var request = AiSuggestMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
        }

        // ==================================================================
        // ===================== HAPPY PATH TESTS ===========================
        // ==================================================================

        /// <summary>
        /// TC-11: Full Happy Path -> Returns success with all response fields populated correctly.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_ReturnsSuccessWithFullResponseData()
        {
            // Arrange
            var probs = new Dictionary<string, double> { { "DME", 0.88 }, { "NORMAL", 0.12 } };
            var task = AiSuggestMockData.GetPredictTask(
                taskId: "task-999",
                status: "completed",
                predictedClass: "DME",
                confidence: 0.88,
                allProbabilities: probs,
                modelVersion: "v2.1.0",
                processingTimeMs: 250);

            _aiMock.Setup(a => a.SubmitPredictionAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            var request = AiSuggestMockData.GetValidRequest(recordId: "rec-001", labResultId: "lab-002");

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();

            var data = result.Data!;
            data.IsSuccess.Should().BeTrue();
            data.TaskId.Should().Be("task-999");
            data.Status.Should().Be("completed");
            data.PredictedClass.Should().Be("DME");
            data.Confidence.Should().Be(0.88);
            data.ModelVersion.Should().Be("v2.1.0");
            data.ProcessingTimeMs.Should().Be(250);
            data.AllProbabilities.Should().BeEquivalentTo(probs);
            data.SuggestionId.Should().NotBeNullOrEmpty();

            _mongoCollectionMock.Verify(
                x => x.InsertOneAsync(It.IsAny<AiSuggestionDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}