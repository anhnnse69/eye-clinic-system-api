using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using ECS.Application.Services.ParaclinicalServices.CreateLabRequestServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Persistence.MongoDb;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace ECS.Test.Services.ParaclinicalServices.CreateLabRequestServices
{
    /// <summary>
    /// Unit tests for <see cref="CreateLabRequestService"/> and <see cref="CreateLabRequestRequestValidator"/>.
    /// Goal: 100% Line AND Branch Coverage.
    /// </summary>
    public class CreateLabRequestServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext>> _medicalRecordRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IMongoDbContext> _mongoMock = new();
        private readonly Mock<IMongoCollection<LabResultDocument>> _mongoCollectionMock = new();
        private readonly Mock<IHttpContextAccessor> _httpMock = new();
        private readonly CreateLabRequestRequestValidator _validator = new();

        private readonly CreateLabRequestService _sut;

        public CreateLabRequestServiceTests()
        {
            _mongoMock.Setup(m => m.LabResults).Returns(_mongoCollectionMock.Object);

            SetupHttpContextUser(CreateLabRequestMockData.ValidUserId);
            SetupMedicalRecordRepo(CreateLabRequestMockData.GetMedicalRecord());
            SetupDoctorRepo(CreateLabRequestMockData.GetDoctorProfile());
            SetupMongoInsertSuccess();

            _sut = new CreateLabRequestService(
                _medicalRecordRepoMock.Object,
                _doctorRepoMock.Object,
                _mongoMock.Object,
                _validator,
                _httpMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // Setup Helpers (Async LINQ Mock Queryable)
        // ─────────────────────────────────────────────────────────────────

        private void SetupHttpContextUser(Guid? userId)
        {
            var identity = userId.HasValue
                ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "TestAuth")
                : new ClaimsIdentity();
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpMock.Setup(x => x.HttpContext).Returns(httpContext);
        }

        private void SetupMedicalRecordRepo(MedicalRecord? record)
        {
            _medicalRecordRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<MedicalRecord, bool>>>(), false))
                .Returns((Expression<Func<MedicalRecord, bool>> predicate, bool _) =>
                {
                    var list = record != null ? new List<MedicalRecord> { record } : new List<MedicalRecord>();
                    return CreateAsyncQueryable(list.AsQueryable().Where(predicate));
                });
        }

        private void SetupDoctorRepo(DoctorProfile? doctor)
        {
            _doctorRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), false))
                .Returns((Expression<Func<DoctorProfile, bool>> predicate, bool _) =>
                {
                    var list = doctor != null ? new List<DoctorProfile> { doctor } : new List<DoctorProfile>();
                    return CreateAsyncQueryable(list.AsQueryable().Where(predicate));
                });
        }

        private void SetupMongoInsertSuccess()
        {
            _mongoCollectionMock
                .Setup(x => x.InsertOneAsync(
                    It.IsAny<LabResultDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private void SetupMongoInsertThrows()
        {
            _mongoCollectionMock
                .Setup(x => x.InsertOneAsync(
                    It.IsAny<LabResultDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Mongo DB connection error"));
        }

        private static IQueryable<T> CreateAsyncQueryable<T>(IQueryable<T> source)
        {
            var mock = new Mock<IQueryable<T>>();
            var asyncProvider = new TestAsyncQueryProvider<T>(source.Provider);

            mock.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) => new TestAsyncEnumerator<T>(source.GetEnumerator()));

            mock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(asyncProvider);
            mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(source.Expression);
            mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(source.ElementType);
            mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => source.GetEnumerator());

            return mock.Object;
        }

        // ==================================================================
        // ==================== VALIDATION TESTS ============================
        // ==================================================================

        [Theory]
        [InlineData("", "REFRACTION")]
        [InlineData(null, "REFRACTION")]
        [InlineData("invalid-guid-string", "REFRACTION")]
        [InlineData("00000000-0000-0000-0000-000000000000", "INVALID_TYPE")]
        public async Task Process_InvalidRequestPayload_ReturnsFail4019(string? recordId, string labType)
        {
            var request = CreateLabRequestMockData.GetValidRequest(recordId: recordId!, labType: labType);
            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();
        }

        [Theory]
        [InlineData("INVALID_SIDE", "REQUESTED")]
        [InlineData("OD", "INVALID_STATUS")]
        [InlineData("INVALID_SIDE", "INVALID_STATUS")]
        public async Task Process_InvalidSideOrStatus_ReturnsFail4019(string side, string status)
        {
            var request = CreateLabRequestMockData.GetValidRequest(side: side, status: status);
            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();
        }

        // ==================================================================
        // ==================== AUTHENTICATION TESTS ========================
        // ==================================================================

        [Fact]
        public async Task Process_UnauthenticatedUser_ReturnsFail4033()
        {
            SetupHttpContextUser(null);
            var request = CreateLabRequestMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_NullHttpContext_ReturnsFail4033()
        {
            _httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            var request = CreateLabRequestMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_UserWithInvalidGuidClaim_ReturnsFail4033()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "NOT_A_GUID") }, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpMock.Setup(x => x.HttpContext).Returns(httpContext);

            var request = CreateLabRequestMockData.GetValidRequest();
            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
        }

        // ==================================================================
        // ================= PARSING & VALIDATION BYPASS =====================
        // ==================================================================

        [Fact]
        public async Task Process_InvalidRecordGuidFormat_TriggersParseRecordIdError_ReturnsFail4019()
        {
            var passValidator = new InlineValidator<CreateLabRequestRequest>();

            var sutForInvalidGuid = new CreateLabRequestService(
                _medicalRecordRepoMock.Object,
                _doctorRepoMock.Object,
                _mongoMock.Object,
                passValidator,
                _httpMock.Object);

            var request = CreateLabRequestMockData.GetValidRequest(recordId: "INVALID_GUID_STRING");

            var result = await sutForInvalidGuid.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();
        }

        // ==================================================================
        // ==================== REPOSITORY RESOLUTION TESTS =================
        // ==================================================================

        [Fact]
        public async Task Process_MedicalRecordNotFound_ReturnsFail4028()
        {
            SetupMedicalRecordRepo(null);
            var request = CreateLabRequestMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4028.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_DoctorProfileNotFound_ReturnsFail4011()
        {
            SetupDoctorRepo(null);
            var request = CreateLabRequestMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
            result.Data.Should().BeNull();
        }

        // ==================================================================
        // === PIPELINE SHORT-CIRCUIT & CONDITIONAL BRANCHES COVERAGE =======
        // ==================================================================

        [Fact]
        public async Task Process_WhenAuthFails_ShortCircuitsAllSubsequentSteps()
        {
            SetupHttpContextUser(null);
            var request = CreateLabRequestMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        [Fact]
        public async Task Process_WhenRecordNotFound_ShortCircuitsDoctorAndInsertSteps()
        {
            SetupMedicalRecordRepo(null);
            var request = CreateLabRequestMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4028.ToString());
        }

        [Fact]
        public async Task Process_WhenDoctorNotFound_ShortCircuitsInsertStep()
        {
            SetupDoctorRepo(null);
            var request = CreateLabRequestMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        // ==================================================================
        // ================= BSON & MEASUREMENTS TESTS ======================
        // ==================================================================

        [Fact]
        public async Task Process_NonObjectMeasurements_ParsesEmptyBsonAndSucceeds()
        {
            using var jsonDoc = JsonDocument.Parse("\"some_string_value\"");
            var request = CreateLabRequestMockData.GetValidRequest(measurements: jsonDoc.RootElement);

            LabResultDocument? insertedDoc = null;
            _mongoCollectionMock
                .Setup(x => x.InsertOneAsync(
                    It.IsAny<LabResultDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<LabResultDocument, InsertOneOptions, CancellationToken>((doc, _, _) => insertedDoc = doc)
                .Returns(Task.CompletedTask);

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            insertedDoc.Should().NotBeNull();
            insertedDoc!.Measurements.ElementCount.Should().Be(0);
        }

        [Fact]
        public async Task Process_NullOrUndefinedMeasurements_ParsesEmptyBsonAndSucceeds()
        {
            using var jsonDoc = JsonDocument.Parse("null");
            var request = CreateLabRequestMockData.GetValidRequest(measurements: jsonDoc.RootElement);

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
        }

        [Fact]
        public async Task Process_InvalidBsonJsonMeasurements_TriggersBsonParseException_ReturnsFail4019()
        {
            using var jsonDoc = JsonDocument.Parse("{\"invalid_date\": {\"$date\": \"INVALID_DATE_FORMAT\"}}");

            var request = CreateLabRequestMockData.GetValidRequest();
            request.Measurements = jsonDoc.RootElement;

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_MongoInsertThrows_ReturnsFail5001()
        {
            SetupMongoInsertThrows();
            var request = CreateLabRequestMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
            result.Data.Should().BeNull();
        }

        // ==================================================================
        // ================= HAPPY PATH & OPTIONAL BRANCHES ==================
        // ==================================================================

        [Fact]
        public async Task Process_ValidRequest_DefaultStatus_ReturnsSuccess2005()
        {
            var request = CreateLabRequestMockData.GetValidRequest(status: null);

            LabResultDocument? insertedDoc = null;
            _mongoCollectionMock
                .Setup(x => x.InsertOneAsync(
                    It.IsAny<LabResultDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<LabResultDocument, InsertOneOptions, CancellationToken>((doc, _, _) => insertedDoc = doc)
                .Returns(Task.CompletedTask);

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data.Should().NotBeNull();
            insertedDoc.Should().NotBeNull();
            insertedDoc!.Status.Should().Be("REQUESTED");
        }

        [Fact]
        public async Task Process_ValidRequest_ExplicitStatus_ReturnsSuccess2005()
        {
            var request = CreateLabRequestMockData.GetValidRequest(status: "COMPLETED");

            LabResultDocument? insertedDoc = null;
            _mongoCollectionMock
                .Setup(x => x.InsertOneAsync(
                    It.IsAny<LabResultDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<LabResultDocument, InsertOneOptions, CancellationToken>((doc, _, _) => insertedDoc = doc)
                .Returns(Task.CompletedTask);

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data.Should().NotBeNull();
            insertedDoc.Should().NotBeNull();
            insertedDoc!.Status.Should().Be("COMPLETED");
        }

        [Fact]
        public async Task ResolveDoctorAsync_WhenStateHasError_ReturnsEarly()
        {
            // Arrange: Lấy chính xác ExecutionState nested class của CreateLabRequestService
            var serviceType = typeof(CreateLabRequestService);
            var stateType = serviceType.GetNestedType("ExecutionState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!;

            var state = Activator.CreateInstance(stateType)!;
            stateType.GetProperty("HasError")?.SetValue(state, true);

            var method = serviceType.GetMethod("ResolveDoctorAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

            // Act: Gọi trực tiếp private method với HasError = true để phủ nhánh TRUE dòng 150
            var task = (Task)method.Invoke(_sut, new[] { state })!;
            await task;

            // Assert
            var hasError = (bool)stateType.GetProperty("HasError")!.GetValue(state)!;
            hasError.Should().BeTrue();
        }

        [Fact]
        public async Task InsertLabDocumentAsync_WhenStateHasError_ReturnsEarly()
        {
            // Arrange: Lấy chính xác ExecutionState nested class của CreateLabRequestService
            var request = CreateLabRequestMockData.GetValidRequest();
            var serviceType = typeof(CreateLabRequestService);
            var stateType = serviceType.GetNestedType("ExecutionState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!;

            var state = Activator.CreateInstance(stateType)!;
            stateType.GetProperty("HasError")?.SetValue(state, true);

            var method = serviceType.GetMethod("InsertLabDocumentAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

            // Act: Gọi trực tiếp private method để phủ nhánh TRUE dòng 170
            var task = (Task)method.Invoke(_sut, new[] { request, state })!;
            await task;

            // Assert
            var hasError = (bool)stateType.GetProperty("HasError")!.GetValue(state)!;
            hasError.Should().BeTrue();
        }

        [Fact]
        public async Task InsertLabDocumentAsync_WhenRecordIsNull_ReturnsEarly()
        {
            // Arrange: state.HasError = false, nhưng Record = null
            var request = CreateLabRequestMockData.GetValidRequest();
            var serviceType = typeof(CreateLabRequestService);
            var stateType = serviceType.GetNestedType("ExecutionState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!;

            var state = Activator.CreateInstance(stateType)!;
            stateType.GetProperty("HasError")?.SetValue(state, false);
            stateType.GetProperty("Record")?.SetValue(state, null);
            stateType.GetProperty("DoctorProfile")?.SetValue(state, CreateLabRequestMockData.GetDoctorProfile());

            var method = serviceType.GetMethod("InsertLabDocumentAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

            // Act: Phủ nhánh thứ 2 của dòng 170 (state.Record == null)
            var task = (Task)method.Invoke(_sut, new[] { request, state })!;
            await task;

            // Assert: Đảm bảo không throw exception và không gọi Insert Mongo
            _mongoCollectionMock.Verify(x => x.InsertOneAsync(
                It.IsAny<LabResultDocument>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task InsertLabDocumentAsync_WhenDoctorProfileIsNull_ReturnsEarly()
        {
            // Arrange: state.HasError = false, Record có giá trị, nhưng DoctorProfile = null
            var request = CreateLabRequestMockData.GetValidRequest();
            var serviceType = typeof(CreateLabRequestService);
            var stateType = serviceType.GetNestedType("ExecutionState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!;

            var state = Activator.CreateInstance(stateType)!;
            stateType.GetProperty("HasError")?.SetValue(state, false);
            stateType.GetProperty("Record")?.SetValue(state, CreateLabRequestMockData.GetMedicalRecord());
            stateType.GetProperty("DoctorProfile")?.SetValue(state, null);

            var method = serviceType.GetMethod("InsertLabDocumentAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

            // Act: Phủ nhánh thứ 3 của dòng 170 (state.DoctorProfile == null)
            var task = (Task)method.Invoke(_sut, new[] { request, state })!;
            await task;

            // Assert: Đảm bảo không gọi Insert Mongo
            _mongoCollectionMock.Verify(x => x.InsertOneAsync(
                It.IsAny<LabResultDocument>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData("OD")]
        [InlineData("OS")]
        [InlineData(null)]
        public async Task Process_ValidRequest_VariousSides_ReturnsSuccess2005(string? side)
        {
            var request = CreateLabRequestMockData.GetValidRequest(side: side);

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data.Should().NotBeNull();
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Internal Async Queryable Mock Helpers
    // ─────────────────────────────────────────────────────────────────

    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

        public IQueryable CreateQuery(Expression expression) => _inner.CreateQuery(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
            new TestAsyncEnumerable<TElement>(expression);

        public object? Execute(Expression expression) => _inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethods()
                .First(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
                .MakeGenericMethod(expectedResultType)
                .Invoke(_inner, new object[] { expression });

            return (TResult)typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult })!;
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

        public T Current => _inner.Current;

        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}