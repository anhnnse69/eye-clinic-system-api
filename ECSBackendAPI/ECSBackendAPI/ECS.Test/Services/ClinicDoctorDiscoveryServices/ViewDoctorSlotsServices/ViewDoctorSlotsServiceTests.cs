using System.Linq.Expressions;
using System.Reflection;
using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicDoctorDiscoveryService.ViewDoctorSlotsServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.ClinicDoctorDiscoveryServices.ViewDoctorSlotsServices
{
    /// <summary>
    /// Unit tests for <see cref="ViewDoctorSlotsService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on <c>ViewDoctorSlotsService.cs</c>.
    /// </summary>
    public class ViewDoctorSlotsServiceTests
    {
        private static readonly Guid DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid DoctorUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid ClinicId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid SpecialtyId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext>> _scheduleRepoMock = new();
        private readonly ViewDoctorSlotsService _sut;

        public ViewDoctorSlotsServiceTests()
        {
            _sut = new ViewDoctorSlotsService(
                _doctorRepoMock.Object,
                _scheduleRepoMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // Reflection helpers
        // ─────────────────────────────────────────────────────────────────

        private static object? InvokePrivate(object target, string methodName, params object[] args)
        {
            var mi = target.GetType().GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            mi.Should().NotBeNull($"method '{methodName}' must exist on {target.GetType().Name}");
            return mi!.Invoke(target, args);
        }

        private static async Task<T> InvokePrivateAsync<T>(object target, string methodName, params object[] args)
        {
            var raw = InvokePrivate(target, methodName, args);
            raw.Should().NotBeNull();
            var task = (Task<T>)raw!;
            return await task;
        }

        private static async Task<object?> InvokePrivateAsyncDynamic(object target, string methodName, params object[] args)
        {
            var raw = InvokePrivate(target, methodName, args);
            raw.Should().NotBeNull();
            var task = (Task)raw!;
            await task;
            // We need a non-generic Task<T>.GetAwaiter().GetResult() approach via reflection.
            var resultProp = task.GetType().GetProperty("Result");
            return resultProp!.GetValue(task);
        }

        // ─────────────────────────────────────────────────────────────────
        // Repository helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupDoctorRepo(IEnumerable<DoctorProfile> doctors)
        {
            var list = doctors.ToList();
            var queryable = list.BuildMockDbSet<DoctorProfile>();
            _doctorRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<DoctorProfile, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<DoctorProfile, object>>[]>()))
                .Returns(queryable.Object);
            _doctorRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<DoctorProfile, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupEmptyDoctorRepo()
            => SetupDoctorRepo(Array.Empty<DoctorProfile>());

        private void SetupScheduleRepo(IEnumerable<DoctorSchedule> schedules)
        {
            var list = schedules.ToList();
            var queryable = list.BuildMockDbSet<DoctorSchedule>();
            _scheduleRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<DoctorSchedule, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<DoctorSchedule, object>>[]>()))
                .Returns(queryable.Object);
            _scheduleRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<DoctorSchedule, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupEmptyScheduleRepo()
            => SetupScheduleRepo(Array.Empty<DoctorSchedule>());

        // ─────────────────────────────────────────────────────────────────
        // Data factories
        // ─────────────────────────────────────────────────────────────────

        private static Clinic MakeClinic(
            Guid id = default,
            string name = "Saigon Eye Clinic",
            string address = "1 Cong Hoa Street") => new()
        {
            Id = id == default ? ClinicId : id,
            Name = name,
            Address = address,
            Phone = "0900000000",
            IsActive = true
        };

        private static User MakeUser(Guid id, string fullName) => new()
        {
            Id = id,
            Phone = "0901111111",
            PasswordHash = "hashed",
            FullName = fullName,
            Role = UserRole.DOCTOR,
            IsActive = true,
            AvatarUrl = "https://example.com/avatar.png"
        };

        private static Specialty MakeSpecialty(string name = "Ophthalmology") => new()
        {
            Id = SpecialtyId,
            Name = name,
            IsActive = true
        };

        private static DoctorProfile MakeDoctor(
            Guid id = default,
            Specialty? specialty = null,
            string? title = "Senior Ophthalmologist",
            int experienceYears = 10,
            string? bio = "Highly experienced",
            decimal? ratingAvg = 4.8m,
            int? reviewCount = 100) => new()
        {
            Id = id == default ? DoctorId : id,
            UserId = DoctorUserId,
            ClinicId = ClinicId,
            SpecialtyId = specialty?.Id,
            Title = title,
            ExperienceYears = experienceYears,
            Bio = bio,
            IsActive = true,
            RatingAvg = ratingAvg,
            ReviewCount = reviewCount,
            User = MakeUser(DoctorUserId, "Dr. Nguyen Van A"),
            Specialty = specialty,
            Clinic = MakeClinic()
        };

        private static TimeSlot MakeSlot(
            Guid id,
            DateTime startTime,
            DateTime endTime,
            SlotStatus status = SlotStatus.AVAILABLE,
            int maxPatients = 1,
            int currentPatients = 0) => new()
        {
            Id = id,
            ScheduleId = Guid.NewGuid(),
            StartTime = startTime,
            EndTime = endTime,
            MaxPatients = maxPatients,
            CurrentPatients = currentPatients,
            Status = status
        };

        private static DoctorSchedule MakeSchedule(
            Guid id,
            DateTime workDate,
            ShiftType shiftType = ShiftType.MORNING,
            params TimeSlot[] slots) => new()
        {
            Id = id,
            DoctorId = DoctorId,
            WorkDate = workDate,
            ShiftType = shiftType,
            IsDeleted = false,
            TimeSlots = slots.ToList()
        };

        // ─────────────────────────────────────────────────────────────────
        // ==================================================================
        // ====================== Process(...) tests ========================
        // ==================================================================

        /// <summary>
        /// TC-VDS-01: Doctor repo returns empty → GetDoctorOrThrowAsync null-coalescing
        /// branch throws KeyNotFoundException(APP_MESSAGE_4011). Schedule repo is
        /// never invoked.
        /// </summary>
        [Fact]
        public async Task Process_DoctorNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1

            //Arrange 2
            SetupEmptyDoctorRepo();
            SetupEmptyScheduleRepo();

            //Act
            Func<Task> act = async () => await _sut.Process(DoctorId);

            //Assert
            var ex = await act.Should().ThrowAsync<KeyNotFoundException>();
            ex.Which.Message.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());

            _scheduleRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<DoctorSchedule, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<DoctorSchedule, object>>[]>()),
                Times.Never);
            _scheduleRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<DoctorSchedule, bool>>>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-VDS-02: Doctor exists, schedule repo empty → success response with empty
        /// ScheduleDays list and all doctor scalar fields populated.
        /// </summary>
        [Fact]
        public async Task Process_EmptySchedule_Returns2000WithEmptyList()
        {
            //Arrange 1
            var doctor = MakeDoctor(specialty: MakeSpecialty());

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupEmptyScheduleRepo();

            //Act
            var result = await _sut.Process(DoctorId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.DoctorId.Should().Be(DoctorId);
            result.Data!.FullName.Should().Be("Dr. Nguyen Van A");
            result.Data!.Specialty.Should().Be("Ophthalmology");
            result.Data!.ClinicName.Should().Be("Saigon Eye Clinic");
            result.Data!.ClinicAddress.Should().Be("1 Cong Hoa Street");
            result.Data!.ExperienceYears.Should().Be(10);
            result.Data!.RatingAvg.Should().Be(4.8m);
            result.Data!.ReviewCount.Should().Be(100);
            result.Data!.ScheduleDays.Should().BeEmpty();
        }

        /// <summary>
        /// TC-VDS-03: Doctor + 1 schedule with 1 fully available slot → ScheduleDays[0]
        /// contains the slot with correct mapping.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_FullDoctorProfile_WithSlots()
        {
            //Arrange 1
            var doctor = MakeDoctor(specialty: MakeSpecialty());
            var futureStart = DateTime.UtcNow.AddDays(1).Date.AddHours(9);
            var futureEnd = futureStart.AddHours(1);
            var slot = MakeSlot(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                futureStart,
                futureEnd,
                status: SlotStatus.AVAILABLE,
                maxPatients: 3,
                currentPatients: 1);
            var schedule = MakeSchedule(
                Guid.Parse("66666666-6666-6666-6666-666666666666"),
                futureStart.Date,
                ShiftType.MORNING,
                slot);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupScheduleRepo(new[] { schedule });

            //Act
            var result = await _sut.Process(DoctorId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.ScheduleDays.Should().HaveCount(1);
            var day = result.Data!.ScheduleDays[0];
            day.ScheduleId.Should().Be(Guid.Parse("66666666-6666-6666-6666-666666666666"));
            day.WorkDate.Should().Be(DateOnly.FromDateTime(futureStart.Date));
            day.ShiftType.Should().Be("MORNING");
            day.Slots.Should().HaveCount(1);
            var slotOut = day.Slots[0];
            slotOut.SlotId.Should().Be(Guid.Parse("55555555-5555-5555-5555-555555555555"));
            slotOut.StartTime.Should().Be(futureStart);
            slotOut.EndTime.Should().Be(futureEnd);
            slotOut.MaxPatients.Should().Be(3);
            slotOut.CurrentPatients.Should().Be(1);
            slotOut.Remaining.Should().Be(2);
            slotOut.Status.Should().Be("AVAILABLE");
        }

        /// <summary>
        /// TC-VDS-04: Schedule with only past/booked/full slots → filtered out by
        /// Where(d => d.Slots.Count > 0). Response.ScheduleDays is empty.
        /// </summary>
        [Fact]
        public async Task Process_ScheduleWithNoAvailableSlots_IsFilteredOut()
        {
            //Arrange 1
            var doctor = MakeDoctor(specialty: MakeSpecialty());
            var pastStart = DateTime.UtcNow.AddDays(-1).Date.AddHours(9);
            var pastEnd = pastStart.AddHours(1);
            var slot = MakeSlot(
                Guid.NewGuid(),
                pastStart,
                pastEnd,
                status: SlotStatus.AVAILABLE,
                maxPatients: 1,
                currentPatients: 0);
            var schedule = MakeSchedule(Guid.NewGuid(), pastStart.Date, ShiftType.MORNING, slot);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupScheduleRepo(new[] { schedule });

            //Act
            var result = await _sut.Process(DoctorId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.ScheduleDays.Should().BeEmpty();
        }

        /// <summary>
        /// TC-VDS-05: Doctor with Specialty = null → response.Specialty == null
        /// (true branch of `doctor.Specialty?.Name`).
        /// </summary>
        [Fact]
        public async Task Process_DoctorSpecialtyIsNull_BuildsNullSpecialty()
        {
            //Arrange 1
            var doctor = MakeDoctor(specialty: null);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupEmptyScheduleRepo();

            //Act
            var result = await _sut.Process(DoctorId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Specialty.Should().BeNull();
        }

        /// <summary>
        /// TC-VDS-06: Two schedules with different WorkDate → result ordered ascending.
        /// </summary>
        [Fact]
        public async Task Process_Ordering_DaysByWorkDate()
        {
            //Arrange 1
            var doctor = MakeDoctor(specialty: MakeSpecialty());
            var day1 = DateTime.UtcNow.AddDays(5).Date;
            var day2 = DateTime.UtcNow.AddDays(2).Date;
            var slot1 = MakeSlot(
                Guid.NewGuid(),
                day1.AddHours(9),
                day1.AddHours(10),
                status: SlotStatus.AVAILABLE,
                maxPatients: 1,
                currentPatients: 0);
            var slot2 = MakeSlot(
                Guid.NewGuid(),
                day2.AddHours(14),
                day2.AddHours(15),
                status: SlotStatus.AVAILABLE,
                maxPatients: 1,
                currentPatients: 0);
            var schedule1 = MakeSchedule(Guid.NewGuid(), day1, ShiftType.MORNING, slot1);
            var schedule2 = MakeSchedule(Guid.NewGuid(), day2, ShiftType.AFTERNOON, slot2);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupScheduleRepo(new[] { schedule1, schedule2 });

            //Act
            var result = await _sut.Process(DoctorId);

            //Assert
            result.Data!.ScheduleDays.Should().HaveCount(2);
            // OrderBy(WorkDate) ascending → earliest first
            var first = result.Data!.ScheduleDays[0].WorkDate;
            var second = result.Data!.ScheduleDays[1].WorkDate;
            first.DayNumber.Should().BeLessThan(second.DayNumber);
        }

        // ==================================================================
        // =========== GetDoctorOrThrowAsync(...) — private ==================
        // ==================================================================

        /// <summary>
        /// TC-VDS-07: Direct reflection call. Seed doctor → returns it.
        /// </summary>
        [Fact]
        public async Task GetDoctorOrThrowAsync_DoctorFound_ReturnsDoctor()
        {
            //Arrange 1
            var doctor = MakeDoctor(specialty: MakeSpecialty());

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });

            //Act
            var result = await InvokePrivateAsync<DoctorProfile>(_sut, "GetDoctorOrThrowAsync", DoctorId);

            //Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(DoctorId);
        }

        /// <summary>
        /// TC-VDS-08: Empty seed → throws KeyNotFoundException(APP_MESSAGE_4011).
        /// </summary>
        [Fact]
        public async Task GetDoctorOrThrowAsync_DoctorNotFound_ThrowsKeyNotFound()
        {
            //Arrange 1

            //Arrange 2
            SetupEmptyDoctorRepo();

            //Act
            Func<Task> act = async () =>
                await InvokePrivateAsync<DoctorProfile>(_sut, "GetDoctorOrThrowAsync", DoctorId);

            //Assert
            var ex = await act.Should().ThrowAsync<KeyNotFoundException>();
            ex.Which.Message.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        // ==================================================================
        // ============ FetchDoctorAsync(...) — private =====================
        // ==================================================================

        /// <summary>
        /// TC-VDS-09: Direct reflection call. Seed doctor → returns DoctorFetchResult with
        /// IsFound=true and the seeded doctor.
        /// </summary>
        [Fact]
        public async Task FetchDoctorAsync_DoctorFound_ReturnsFoundResult()
        {
            //Arrange 1
            var doctor = MakeDoctor(specialty: MakeSpecialty());

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });

            //Act
            var raw = await InvokePrivateAsyncDynamic(_sut, "FetchDoctorAsync", DoctorId);

            //Assert
            var isFound = (bool)raw!.GetType().GetProperty("IsFound")!.GetValue(raw)!;
            var fetchedDoctor = raw.GetType().GetProperty("Doctor")!.GetValue(raw);
            isFound.Should().BeTrue();
            fetchedDoctor.Should().NotBeNull();
            ((DoctorProfile)fetchedDoctor!).Id.Should().Be(DoctorId);
        }

        /// <summary>
        /// TC-VDS-10: Empty seed → DoctorFetchResult with IsFound=false and Doctor=null.
        /// </summary>
        [Fact]
        public async Task FetchDoctorAsync_DoctorNotFound_ReturnsNotFoundResult()
        {
            //Arrange 1

            //Arrange 2
            SetupEmptyDoctorRepo();

            //Act
            var raw = await InvokePrivateAsyncDynamic(_sut, "FetchDoctorAsync", DoctorId);

            //Assert
            var isFound = (bool)raw!.GetType().GetProperty("IsFound")!.GetValue(raw)!;
            var fetchedDoctor = raw.GetType().GetProperty("Doctor")!.GetValue(raw);
            isFound.Should().BeFalse();
            fetchedDoctor.Should().BeNull();
        }

        // ==================================================================
        // ============ FetchScheduleDaysAsync(...) — private ===============
        // ==================================================================

        /// <summary>
        /// TC-VDS-11: Direct reflection call. Empty seed → empty list.
        /// </summary>
        [Fact]
        public async Task FetchScheduleDaysAsync_NoSchedules_ReturnsEmpty()
        {
            //Arrange 1

            //Arrange 2
            SetupEmptyScheduleRepo();

            //Act
            var result = await InvokePrivateAsync<List<DoctorScheduleDay>>(
                _sut, "FetchScheduleDaysAsync", DoctorId);

            //Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// TC-VDS-12: Schedules with only past slots → all filtered out.
        /// </summary>
        [Fact]
        public async Task FetchScheduleDaysAsync_AllSchedulesHaveNoAvailableSlots_ReturnsEmpty()
        {
            //Arrange 1
            var pastStart = DateTime.UtcNow.AddDays(-1).Date.AddHours(9);
            var pastEnd = pastStart.AddHours(1);
            var pastSlot = MakeSlot(Guid.NewGuid(), pastStart, pastEnd, status: SlotStatus.AVAILABLE);
            var schedule = MakeSchedule(Guid.NewGuid(), pastStart.Date, ShiftType.MORNING, pastSlot);

            //Arrange 2
            SetupScheduleRepo(new[] { schedule });

            //Act
            var result = await InvokePrivateAsync<List<DoctorScheduleDay>>(
                _sut, "FetchScheduleDaysAsync", DoctorId);

            //Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// TC-VDS-13: Mixed schedules — only days with at least one available slot remain.
        /// </summary>
        [Fact]
        public async Task FetchScheduleDaysAsync_ReturnsOnlyDaysWithAvailableSlots()
        {
            //Arrange 1
            var futureDay = DateTime.UtcNow.AddDays(3).Date;
            var pastDay = DateTime.UtcNow.AddDays(-1).Date;

            var futureSlot = MakeSlot(
                Guid.NewGuid(),
                futureDay.AddHours(9),
                futureDay.AddHours(10),
                status: SlotStatus.AVAILABLE,
                maxPatients: 1,
                currentPatients: 0);
            var futureSchedule = MakeSchedule(Guid.NewGuid(), futureDay, ShiftType.MORNING, futureSlot);

            var pastSlot = MakeSlot(
                Guid.NewGuid(),
                pastDay.AddHours(9),
                pastDay.AddHours(10),
                status: SlotStatus.AVAILABLE,
                maxPatients: 1,
                currentPatients: 0);
            var pastSchedule = MakeSchedule(Guid.NewGuid(), pastDay, ShiftType.MORNING, pastSlot);

            //Arrange 2
            SetupScheduleRepo(new[] { futureSchedule, pastSchedule });

            //Act
            var result = await InvokePrivateAsync<List<DoctorScheduleDay>>(
                _sut, "FetchScheduleDaysAsync", DoctorId);

            //Assert
            result.Should().HaveCount(1);
            result[0].WorkDate.Should().Be(DateOnly.FromDateTime(futureDay));
            result[0].Slots.Should().HaveCount(1);
        }

        // ==================================================================
        // ============== MapScheduleDay(...) — private static ==============
        // ==================================================================

        /// <summary>
        /// TC-VDS-14: All 3 conditions met → slot is kept, every DoctorTimeSlot field
        /// is mapped correctly.
        /// </summary>
        [Fact]
        public void MapScheduleDay_AllConditionsMet_KeepsSlot()
        {
            //Arrange 1
            var futureStart = DateTime.UtcNow.AddDays(1).Date.AddHours(9);
            var futureEnd = futureStart.AddHours(1);
            var slot = new TimeSlot
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                ScheduleId = Guid.NewGuid(),
                StartTime = futureStart,
                EndTime = futureEnd,
                MaxPatients = 3,
                CurrentPatients = 1,
                Status = SlotStatus.AVAILABLE
            };
            var schedule = new DoctorSchedule
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                DoctorId = DoctorId,
                WorkDate = futureStart.Date,
                ShiftType = ShiftType.MORNING,
                TimeSlots = new List<TimeSlot> { slot }
            };
            var nowUtc = DateTime.UtcNow;

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "MapScheduleDay", schedule, nowUtc);
            var day = raw.Should().BeAssignableTo<DoctorScheduleDay>().Subject;

            //Assert
            day.ScheduleId.Should().Be(Guid.Parse("88888888-8888-8888-8888-888888888888"));
            day.WorkDate.Should().Be(DateOnly.FromDateTime(futureStart.Date));
            day.ShiftType.Should().Be("MORNING");
            day.Slots.Should().HaveCount(1);
            var slotOut = day.Slots[0];
            slotOut.SlotId.Should().Be(Guid.Parse("77777777-7777-7777-7777-777777777777"));
            slotOut.StartTime.Should().Be(futureStart);
            slotOut.EndTime.Should().Be(futureEnd);
            slotOut.MaxPatients.Should().Be(3);
            slotOut.CurrentPatients.Should().Be(1);
            slotOut.Remaining.Should().Be(2);
            slotOut.Status.Should().Be("AVAILABLE");
        }

        /// <summary>
        /// TC-VDS-15: Slot with Status = BOOKED → filtered out.
        /// </summary>
        [Fact]
        public void MapScheduleDay_SlotStatusNotAvailable_FiltersOut()
        {
            //Arrange 1
            var futureStart = DateTime.UtcNow.AddDays(1).Date.AddHours(9);
            var slot = new TimeSlot
            {
                Id = Guid.NewGuid(),
                StartTime = futureStart,
                EndTime = futureStart.AddHours(1),
                MaxPatients = 1,
                CurrentPatients = 0,
                Status = SlotStatus.BOOKED
            };
            var schedule = new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                DoctorId = DoctorId,
                WorkDate = futureStart.Date,
                ShiftType = ShiftType.MORNING,
                TimeSlots = new List<TimeSlot> { slot }
            };

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "MapScheduleDay", schedule, DateTime.UtcNow);
            var day = raw.Should().BeAssignableTo<DoctorScheduleDay>().Subject;

            //Assert
            day.Slots.Should().BeEmpty();
        }

        /// <summary>
        /// TC-VDS-16: Slot with CurrentPatients == MaxPatients → filtered out.
        /// </summary>
        [Fact]
        public void MapScheduleDay_SlotCurrentPatientsAtMax_FiltersOut()
        {
            //Arrange 1
            var futureStart = DateTime.UtcNow.AddDays(1).Date.AddHours(9);
            var slot = new TimeSlot
            {
                Id = Guid.NewGuid(),
                StartTime = futureStart,
                EndTime = futureStart.AddHours(1),
                MaxPatients = 2,
                CurrentPatients = 2,
                Status = SlotStatus.AVAILABLE
            };
            var schedule = new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                DoctorId = DoctorId,
                WorkDate = futureStart.Date,
                ShiftType = ShiftType.MORNING,
                TimeSlots = new List<TimeSlot> { slot }
            };

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "MapScheduleDay", schedule, DateTime.UtcNow);
            var day = raw.Should().BeAssignableTo<DoctorScheduleDay>().Subject;

            //Assert
            day.Slots.Should().BeEmpty();
        }

        /// <summary>
        /// TC-VDS-17: Slot with StartTime < nowUtc → filtered out.
        /// </summary>
        [Fact]
        public void MapScheduleDay_SlotStartTimeInPast_FiltersOut()
        {
            //Arrange 1
            var pastStart = DateTime.UtcNow.AddHours(-1);
            var slot = new TimeSlot
            {
                Id = Guid.NewGuid(),
                StartTime = pastStart,
                EndTime = pastStart.AddHours(1),
                MaxPatients = 1,
                CurrentPatients = 0,
                Status = SlotStatus.AVAILABLE
            };
            var schedule = new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                DoctorId = DoctorId,
                WorkDate = DateOnly.FromDateTime(pastStart.Date).ToDateTime(TimeOnly.MinValue),
                ShiftType = ShiftType.MORNING,
                TimeSlots = new List<TimeSlot> { slot }
            };

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "MapScheduleDay", schedule, DateTime.UtcNow);
            var day = raw.Should().BeAssignableTo<DoctorScheduleDay>().Subject;

            //Assert
            day.Slots.Should().BeEmpty();
        }

        /// <summary>
        /// TC-VDS-18: Two available slots with unsorted StartTime → ordered ascending.
        /// </summary>
        [Fact]
        public void MapScheduleDay_MultipleSlots_OrdersByStartTime()
        {
            //Arrange 1
            var futureDay = DateTime.UtcNow.AddDays(1).Date;
            var slotLater = new TimeSlot
            {
                Id = Guid.NewGuid(),
                StartTime = futureDay.AddHours(14),
                EndTime = futureDay.AddHours(15),
                MaxPatients = 1,
                CurrentPatients = 0,
                Status = SlotStatus.AVAILABLE
            };
            var slotEarlier = new TimeSlot
            {
                Id = Guid.NewGuid(),
                StartTime = futureDay.AddHours(9),
                EndTime = futureDay.AddHours(10),
                MaxPatients = 1,
                CurrentPatients = 0,
                Status = SlotStatus.AVAILABLE
            };
            var schedule = new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                DoctorId = DoctorId,
                WorkDate = futureDay,
                ShiftType = ShiftType.MORNING,
                TimeSlots = new List<TimeSlot> { slotLater, slotEarlier }
            };

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "MapScheduleDay", schedule, DateTime.UtcNow);
            var day = raw.Should().BeAssignableTo<DoctorScheduleDay>().Subject;

            //Assert
            day.Slots.Should().HaveCount(2);
            day.Slots[0].StartTime.Should().BeBefore(day.Slots[1].StartTime);
        }

        // ==================================================================
        // ============== BuildResponse(...) — private static ===============
        // ==================================================================

        /// <summary>
        /// TC-VDS-19: Direct reflection call. Doctor with Specialty → Specialty populated.
        /// </summary>
        [Fact]
        public void BuildResponse_WithSpecialty_PopulatesSpecialtyName()
        {
            //Arrange 1
            var doctor = MakeDoctor(specialty: MakeSpecialty("Ophthalmology"));

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "BuildResponse", doctor, new List<DoctorScheduleDay>())!;
            var response = raw.Should().BeAssignableTo<ViewDoctorSlotsResponse>().Subject;

            //Assert
            response.Specialty.Should().Be("Ophthalmology");
        }

        /// <summary>
        /// TC-VDS-20: Direct reflection call. Doctor with Specialty = null → Specialty is null.
        /// </summary>
        [Fact]
        public void BuildResponse_NullSpecialty_PopulatesNullSpecialty()
        {
            //Arrange 1
            var doctor = MakeDoctor(specialty: null);

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "BuildResponse", doctor, new List<DoctorScheduleDay>())!;
            var response = raw.Should().BeAssignableTo<ViewDoctorSlotsResponse>().Subject;

            //Assert
            response.Specialty.Should().BeNull();
        }

        /// <summary>
        /// TC-VDS-21: Direct reflection call. Asserts every field of ViewDoctorSlotsResponse
        /// is copied from the doctor + schedules.
        /// </summary>
        [Fact]
        public void BuildResponse_AssignsEveryField()
        {
            //Arrange 1
            var doctor = MakeDoctor(
                specialty: MakeSpecialty("Ophthalmology"),
                title: "Senior",
                experienceYears: 12,
                bio: "Bio",
                ratingAvg: 4.7m,
                reviewCount: 80);
            var days = new List<DoctorScheduleDay>
            {
                new() { ScheduleId = Guid.NewGuid(), WorkDate = new DateOnly(2026, 7, 26), ShiftType = "MORNING" }
            };

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "BuildResponse", doctor, days)!;
            var response = raw.Should().BeAssignableTo<ViewDoctorSlotsResponse>().Subject;

            //Assert
            response.DoctorId.Should().Be(DoctorId);
            response.FullName.Should().Be("Dr. Nguyen Van A");
            response.AvatarUrl.Should().Be("https://example.com/avatar.png");
            response.Title.Should().Be("Senior");
            response.Specialty.Should().Be("Ophthalmology");
            response.ClinicName.Should().Be("Saigon Eye Clinic");
            response.ClinicAddress.Should().Be("1 Cong Hoa Street");
            response.ExperienceYears.Should().Be(12);
            response.Bio.Should().Be("Bio");
            response.RatingAvg.Should().Be(4.7m);
            response.ReviewCount.Should().Be(80);
            response.ScheduleDays.Should().BeSameAs(days);
        }

        // ==================================================================
        // ========== CreateSuccessResponse(...) — private instance =========
        // ==================================================================

        /// <summary>
        /// TC-VDS-22: Direct reflection call. Wraps the response in ApiResponse<T>.Success
        /// with APP_MESSAGE_2000 and no Meta. Note: CreateSuccessResponse is an instance
        /// method on the service.
        /// </summary>
        [Fact]
        public void CreateSuccessResponse_WrapsWithCodeMessage2000()
        {
            //Arrange 1
            var payload = new ViewDoctorSlotsResponse
            {
                DoctorId = DoctorId,
                FullName = "Dr. Test",
                ClinicName = "Clinic",
                ClinicAddress = "Address"
            };

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CreateSuccessResponse", payload);
            var result = raw.Should().BeAssignableTo<ApiResponse<ViewDoctorSlotsResponse>>().Subject;

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeSameAs(payload);
            result.Meta.Should().BeNull();
        }
    }
}