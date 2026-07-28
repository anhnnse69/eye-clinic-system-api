using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Application.Services.ParaclinicalServices.GetLabResultsServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Persistence.MongoDb;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace ECS.Test.Services.ParaclinicalServices.GetLabResultsServices
{
    /// <summary>
    /// Unit tests for <see cref="GetLabResultsService"/>.
    /// Target: 100% Line AND Branch Coverage.
    /// </summary>
    public class GetLabResultsServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext>> _recordRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>> _patientRepoMock = new();
        private readonly Mock<IMongoDbContext> _mongoMock = new();
        private readonly Mock<IMongoCollection<LabResultDocument>> _mongoCollectionMock = new();
        private readonly Mock<IAsyncCursor<LabResultDocument>> _mongoCursorMock = new();
        private readonly Mock<IHttpContextAccessor> _httpMock = new();

        private readonly GetLabResultsService _sut;

        public GetLabResultsServiceTests()
        {
            _mongoMock.Setup(m => m.LabResults).Returns(_mongoCollectionMock.Object);

            // Default Happy Path Setup (Doctor role owning the record)
            SetupHttpContextUser(GetLabResultsMockData.ValidDoctorUserId, nameof(UserRole.DOCTOR));
            SetupMedicalRecordRepo(GetLabResultsMockData.GetMedicalRecord());
            SetupDoctorRepo(GetLabResultsMockData.GetDoctorProfile());
            SetupPatientRepo(GetLabResultsMockData.GetPatientProfile());
            SetupMongoQueryResults(GetLabResultsMockData.GetSampleMongoDocs());

            _sut = new GetLabResultsService(
                _recordRepoMock.Object,
                _doctorRepoMock.Object,
                _patientRepoMock.Object,
                _mongoMock.Object,
                _httpMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // MOCK HELPERS
        // ─────────────────────────────────────────────────────────────────

        private void SetupHttpContextUser(Guid? userId, string? role)
        {
            if (!userId.HasValue)
            {
                _httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
                return;
            }

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.Value.ToString()) };
            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpMock.Setup(x => x.HttpContext).Returns(httpContext);
        }

        private void SetupMedicalRecordRepo(MedicalRecord? record)
        {
            _recordRepoMock
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

        private void SetupPatientRepo(PatientProfile? patient)
        {
            _patientRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), false))
                .Returns((Expression<Func<PatientProfile, bool>> predicate, bool _) =>
                {
                    var list = patient != null ? new List<PatientProfile> { patient } : new List<PatientProfile>();
                    return CreateAsyncQueryable(list.AsQueryable().Where(predicate));
                });
        }

        private void SetupMongoQueryResults(List<LabResultDocument> results)
        {
            _mongoCursorMock.Setup(_ => _.Current).Returns(results);
            _mongoCursorMock
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mongoCollectionMock
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<LabResultDocument>>(),
                    It.IsAny<FindOptions<LabResultDocument, LabResultDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mongoCursorMock.Object);
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
        // 1. VALIDATION TESTS
        // ==================================================================

        [Theory]
        [InlineData("invalid-guid")]
        [InlineData("")]
        [InlineData(null)]
        public async Task Process_InvalidRecordId_ReturnsFail4019(string? recordId)
        {
            // Tạo request trực tiếp thay vì thông qua helper có gán mặc định
            var request = new GetLabResultsRequest
            {
                RecordId = recordId!,
                LabType = null,
                Side = null
            };

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();
        }

        // ==================================================================
        // 2. AUTHENTICATION & USER INFO TESTS
        // ==================================================================

        [Fact]
        public async Task Process_NullHttpContext_ReturnsFail4033()
        {
            SetupHttpContextUser(null, null);
            var request = GetLabResultsMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        [Fact]
        public async Task Process_InvalidUserClaimGuid_ReturnsFail4033()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "NOT_A_GUID") }, "TestAuth");
            _httpMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext { User = new ClaimsPrincipal(identity) });

            var request = GetLabResultsMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        // ==================================================================
        // 3. AUTHORIZATION & ROLE ACCESS TESTS
        // ==================================================================

        [Fact]
        public async Task Process_RecordNotFound_ReturnsFail4028()
        {
            SetupMedicalRecordRepo(null);
            var request = GetLabResultsMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4028.ToString());
        }

        [Fact]
        public async Task Process_DoctorNotFound_ReturnsFail4033()
        {
            SetupDoctorRepo(null); // Doctor Profile active không tìm thấy
            var request = GetLabResultsMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        [Fact]
        public async Task Process_DoctorNotAssignedToRecord_ReturnsFail4014()
        {
            var otherDoctorId = Guid.NewGuid();
            SetupDoctorRepo(GetLabResultsMockData.GetDoctorProfile(doctorId: otherDoctorId));
            var request = GetLabResultsMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
        }

        [Fact]
        public async Task Process_PatientUser_AssignedToRecord_ReturnsSuccess2000()
        {
            SetupHttpContextUser(GetLabResultsMockData.ValidPatientUserId, nameof(UserRole.PATIENT));
            var request = GetLabResultsMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Process_PatientNotFound_ReturnsFail4014()
        {
            SetupHttpContextUser(GetLabResultsMockData.ValidPatientUserId, nameof(UserRole.PATIENT));
            SetupPatientRepo(null);
            var request = GetLabResultsMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
        }

        [Fact]
        public async Task Process_PatientNotOwnerOfRecord_ReturnsFail4014()
        {
            SetupHttpContextUser(GetLabResultsMockData.ValidPatientUserId, nameof(UserRole.PATIENT));
            SetupPatientRepo(GetLabResultsMockData.GetPatientProfile(patientId: Guid.NewGuid()));
            var request = GetLabResultsMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
        }

        [Theory]
        [InlineData(nameof(UserRole.CLINIC_ADMIN))]
        [InlineData(nameof(UserRole.RECEPTIONIST))]
        [InlineData(nameof(UserRole.SYSTEM_ADMIN))]
        public async Task Process_StaffRoles_ReturnsSuccess2000(string staffRole)
        {
            SetupHttpContextUser(GetLabResultsMockData.ValidStaffUserId, staffRole);
            var request = GetLabResultsMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
        }

        [Fact]
        public async Task Process_UnknownRole_ReturnsFail4033()
        {
            SetupHttpContextUser(Guid.NewGuid(), "UNKNOWN_ROLE");
            var request = GetLabResultsMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        // ==================================================================
        // 4. MONGO FILTERING & HAPPY PATH TESTS
        // ==================================================================

        [Fact]
        public async Task Process_WithLabTypeAndSideFilters_AppliesFiltersAndReturnsSuccess()
        {
            var request = GetLabResultsMockData.GetValidRequest(labType: "oct", side: "od");

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Count.Should().Be(2);
            result.Data.Results[0].AiPrediction.Should().NotBeNull();
            result.Data.Results[1].AiPrediction.Should().BeNull(); // Cover null AiPrediction branch
        }

        // ==================================================================
        // 5. REFLECTION TESTS FOR DEAD BRANCH COVERAGE (100% COVERAGE)
        // ==================================================================

        [Fact]
        public async Task ResolveAccessAsync_WhenStateHasError_ReturnsEarly()
        {
            var serviceType = typeof(GetLabResultsService);
            var stateType = serviceType.GetNestedType("ExecutionState", System.Reflection.BindingFlags.NonPublic)!;

            var state = Activator.CreateInstance(stateType)!;
            stateType.GetProperty("HasError")?.SetValue(state, true);

            var method = serviceType.GetMethod("ResolveAccessAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

            var task = (Task)method.Invoke(_sut, new[] { state, GetLabResultsMockData.ValidRecordId.ToString() })!;
            await task;

            ((bool)stateType.GetProperty("HasError")!.GetValue(state)!).Should().BeTrue();
        }

        [Fact]
        public async Task LoadLabResultsAsync_WhenStateHasError_ReturnsEarly()
        {
            var request = GetLabResultsMockData.GetValidRequest();
            var serviceType = typeof(GetLabResultsService);
            var stateType = serviceType.GetNestedType("ExecutionState", System.Reflection.BindingFlags.NonPublic)!;

            var state = Activator.CreateInstance(stateType)!;
            stateType.GetProperty("HasError")?.SetValue(state, true);

            var method = serviceType.GetMethod("LoadLabResultsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

            var task = (Task)method.Invoke(_sut, new[] { state, request })!;
            await task;

            ((bool)stateType.GetProperty("HasError")!.GetValue(state)!).Should().BeTrue();
        }

        [Fact]
        public async Task LoadLabResultsAsync_WhenRecordIsNull_ReturnsEarly()
        {
            var request = GetLabResultsMockData.GetValidRequest();
            var serviceType = typeof(GetLabResultsService);
            var stateType = serviceType.GetNestedType("ExecutionState", System.Reflection.BindingFlags.NonPublic)!;

            var state = Activator.CreateInstance(stateType)!;
            stateType.GetProperty("HasError")?.SetValue(state, false);
            stateType.GetProperty("Record")?.SetValue(state, null);

            var method = serviceType.GetMethod("LoadLabResultsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

            var task = (Task)method.Invoke(_sut, new[] { state, request })!;
            await task;

            _mongoCollectionMock.Verify(x => x.FindAsync(
                It.IsAny<FilterDefinition<LabResultDocument>>(),
                It.IsAny<FindOptions<LabResultDocument, LabResultDocument>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateResponse_WhenErrorCodeIsNull_UsesDefault4001()
        {
            var serviceType = typeof(GetLabResultsService);
            var stateType = serviceType.GetNestedType("ExecutionState", System.Reflection.BindingFlags.NonPublic)!;

            var state = Activator.CreateInstance(stateType)!;
            stateType.GetProperty("HasError")?.SetValue(state, true);
            stateType.GetProperty("ErrorCode")?.SetValue(state, null); // ErrorCode = null

            var method = serviceType.GetMethod("CreateResponse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

            var result = (ApiResponse<GetLabResultsResponse>)method.Invoke(_sut, new[] { state })!;

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
        }
        // ==================================================================
        // 6. ADDITIONAL BRANCH COVERAGE TESTS (100% BRANCH TARGET)
        // ==================================================================

        [Fact]
        public async Task SetupHttpContextUser_WhenRoleIsNull_DoesNotAddRoleClaim()
        {
            // Cover branch: if (!string.IsNullOrEmpty(role)) -> branch FALSE (role = null)
            SetupHttpContextUser(Guid.NewGuid(), role: null);
            var request = GetLabResultsMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        [Fact]
        public async Task Process_WhenLabTypeAndSideAreNull_ExecutesWithoutFilters()
        {
            // Cover branches trong Mongo query builder:
            // - labType filter -> FALSE branch (labType = null)
            // - side filter -> FALSE branch (side = null)
            var request = GetLabResultsMockData.GetValidRequest(labType: null, side: null);

            var result = await _sut.Process(request);

            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // INTERNAL ASYNC QUERYABLE HELPERS
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