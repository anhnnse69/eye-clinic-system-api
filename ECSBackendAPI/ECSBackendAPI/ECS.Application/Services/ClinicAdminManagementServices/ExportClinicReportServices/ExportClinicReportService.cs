using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicAdminManagementServices.ExportClinicReportServices
{
    /// <summary>
    /// Compiles complete clinic operational report data for export.
    /// </summary>
    public class ExportClinicReportService : IExportClinicReportService
    {
        private readonly IRepositoryQueryBase<Clinic, Guid, AppDbContext> _clinicRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> _medicalRecordRepository;
        private readonly IRepositoryQueryBase<Service, Guid, AppDbContext> _serviceRepository;
        private readonly IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext> _roomRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExportClinicReportService(
            IRepositoryQueryBase<Clinic, Guid, AppDbContext> clinicRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> medicalRecordRepository,
            IRepositoryQueryBase<Service, Guid, AppDbContext> serviceRepository,
            IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext> roomRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _clinicRepository = clinicRepository;
            _staffClinicRepository = staffClinicRepository;
            _medicalRecordRepository = medicalRecordRepository;
            _serviceRepository = serviceRepository;
            _roomRepository = roomRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResponse<ExportClinicReportResponse>> Process()
        {
            bool isUserValid = true;
            var userId = RetrieveUserId(ref isUserValid);
            if (!isUserValid)
            {
                return ApiResponse<ExportClinicReportResponse>.Fail(GeneralCode.APP_MESSAGE_4001.ToString());
            }

            var clinicId = await RetrieveClinicId(userId);
            if (!clinicId.HasValue)
            {
                return ApiResponse<ExportClinicReportResponse>.Fail(GeneralCode.APP_MESSAGE_4020.ToString());
            }

            var clinic = await _clinicRepository
                .FindByCondition(c => c.Id == clinicId.Value, trackChanges: false)
                .FirstOrDefaultAsync();

            if (clinic == null)
            {
                return ApiResponse<ExportClinicReportResponse>.Fail(GeneralCode.APP_MESSAGE_4020.ToString());
            }

            var staffs = await _staffClinicRepository
                .FindByCondition(s => s.ClinicId == clinicId.Value, trackChanges: false)
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var medicalRecords = await _medicalRecordRepository
                .FindByCondition(m => m.Doctor.ClinicId == clinicId.Value, trackChanges: false)
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .ThenInclude(d => d.User)
                .Include(m => m.Appointment)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var services = await _serviceRepository
                .FindByCondition(s => s.ClinicId == clinicId.Value, trackChanges: false)
                .OrderBy(s => s.ServiceName)
                .ToListAsync();

            var rooms = await _roomRepository
                .FindByCondition(r => r.ClinicId == clinicId.Value, trackChanges: false)
                .OrderBy(r => r.RoomName)
                .ToListAsync();

            var response = new ExportClinicReportResponse
            {
                ClinicInfo = new ClinicProfileReportDto
                {
                    Id = clinic.Id,
                    Name = clinic.Name,
                    Address = clinic.Address,
                    Phone = clinic.Phone,
                    Email = clinic.Email,
                    OpenTime = clinic.OpenTime,
                    CloseTime = clinic.CloseTime,
                    RatingAvg = clinic.RatingAvg,
                    ReviewCount = clinic.ReviewCount,
                    IsActive = clinic.IsActive,
                    IsPublished = clinic.IsPublished,
                    CreatedAt = clinic.CreatedAt,
                    TotalStaffs = staffs.Count,
                    TotalMedicalRecords = medicalRecords.Count,
                    TotalServices = services.Count,
                    TotalRooms = rooms.Count
                },
                Staffs = staffs.Select(s => new ClinicStaffReportDto
                {
                    UserId = s.UserId,
                    FullName = s.User?.FullName ?? string.Empty,
                    Email = s.User?.Email ?? string.Empty,
                    Phone = s.User?.Phone ?? string.Empty,
                    Role = s.Role switch
                    {
                        StaffRole.DOCTOR => "Bác sĩ",
                        StaffRole.RECEPTIONIST => "Tiếp tân",
                        StaffRole.CLINIC_ADMIN => "Quản lý phòng khám",
                        _ => s.Role.ToString()
                    },
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt
                }).ToList(),
                MedicalRecords = medicalRecords.Select(m => new ClinicMedicalRecordReportDto
                {
                    RecordId = m.Id,
                    AppointmentId = m.AppointmentId,
                    PatientName = m.Patient?.FullName ?? string.Empty,
                    PatientPhone = m.Patient?.PhoneNumber ?? string.Empty,
                    PatientGender = m.Patient?.Gender switch
                    {
                        Gender.MALE => "Nam",
                        Gender.FEMALE => "Nữ",
                        _ => "Khác"
                    },
                    PatientDob = m.Patient != null ? m.Patient.Dob.ToString("dd/MM/yyyy") : string.Empty,
                    DoctorName = m.Doctor?.User?.FullName ?? string.Empty,
                    RecordType = m.RecordType.ToString(),
                    ChiefComplaint = m.ChiefComplaint,
                    Summary = m.Summary,
                    Notes = m.Notes,
                    Status = m.Status.ToString(),
                    FinalizedAt = m.FinalizedAt,
                    CreatedAt = m.CreatedAt
                }).ToList(),
                Services = services.Select(s => new ClinicServiceReportDto
                {
                    ServiceId = s.Id,
                    ServiceName = s.ServiceName,
                    Price = s.Price ?? 0,
                    DurationMinutes = s.DurationMinutes,
                    IsActive = s.IsActive
                }).ToList(),
                Rooms = rooms.Select(r => new ClinicRoomReportDto
                {
                    RoomId = r.Id,
                    RoomName = r.RoomName,
                    RoomType = r.RoomType,
                    IsActive = r.IsActive
                }).ToList()
            };

            return ApiResponse<ExportClinicReportResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }

        private Guid RetrieveUserId(ref bool isUserValid)
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                isUserValid = false;
                return Guid.Empty;
            }
            return userId;
        }

        private async Task<Guid?> RetrieveClinicId(Guid userId)
        {
            var staffClinic = await _staffClinicRepository
                .FindByCondition(x => x.UserId == userId && x.IsActive)
                .FirstOrDefaultAsync();

            return staffClinic?.ClinicId;
        }
    }
}
