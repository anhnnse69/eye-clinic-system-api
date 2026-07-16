using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.MedicalRecordsServices.UpdateMedicalRecordServices
{
    /// <summary>
    /// Service implementation for updating medical records.
    /// **Refactored (2026-07-14)**: This is the legacy v1 update endpoint. The detailed
    /// form data (eye exam, subspecialty, prescriptions, etc.) used to be split across
    /// ~20 navigation collections on <see cref="MedicalRecord"/>. After the Cloudinary
    /// refactor those entities were dropped; the form data now lives as JSON in a single
    /// payload on Cloudinary (<see cref="MedicalRecord.RecordDataUrl"/>).
    ///
    /// For now this legacy endpoint only updates the core metadata columns of
    /// <see cref="MedicalRecord"/> (RecordType, Notes, Status). New updates should use the
    /// v2 endpoint that uploads a new JSON payload to Cloudinary.
    /// </summary>
    public class UpdateMedicalRecordService : IUpdateMedicalRecordService
    {
        private readonly IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext> _medicalRecordRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IValidator<UpdateMedicalRecordRequest> _validator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public UpdateMedicalRecordService(
            IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext> medicalRecordRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IValidator<UpdateMedicalRecordRequest> validator,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider)
        {
            _medicalRecordRepository = medicalRecordRepository;
            _doctorRepository = doctorRepository;
            _validator = validator;
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        public async Task<ApiResponse<UpdateMedicalRecordResponse>> Process(Guid recordId, UpdateMedicalRecordRequest request)
        {
            var state = new ExecutionState();

            try
            {
                // Validation (now no-op for v1 since most fields were removed from entity)
                var validation = await _validator.ValidateAsync(request);
                if (!validation.IsValid)
                {
                    state.HasError = true;
                    state.ErrorCode = GeneralCode.APP_MESSAGE_4019.ToString();
                    return CreateResponse(state);
                }

                var record = await FetchMedicalRecordAsync(recordId);
                if (record == null)
                {
                    state.HasError = true;
                    state.ErrorCode = GeneralCode.APP_MESSAGE_4004.ToString();
                    return CreateResponse(state);
                }
                state.MedicalRecord = record;

                // Only metadata fields are still on the entity — update those.
                if (!string.IsNullOrEmpty(request.RecordType))
                {
                    if (Enum.TryParse<RecordType>(request.RecordType, true, out var rt))
                    {
                        record.RecordType = rt;
                    }
                }
                if (request.Notes != null) record.Notes = request.Notes;
                if (request.Summary != null) record.Summary = request.Summary;
                record.UpdatedAt = DateTime.UtcNow;

                state.IsExecutionSuccess = true;
                await PersistChangesAsync(state);
            }
            catch (Exception)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }

            return CreateResponse(state);
        }

        /// <summary>
        /// Fetches the medical record (only core navigation: Patient, Doctor, Appointment)
        /// since all eye exam/subspecialty navigation collections have been dropped.
        /// </summary>
        private async Task<MedicalRecord?> FetchMedicalRecordAsync(Guid recordId)
        {
            return await _medicalRecordRepository
                .FindByCondition(r => r.Id == recordId)
                .Include(r => r.Appointment)
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .Include(r => r.DocumentAccessPermissions)
                .FirstOrDefaultAsync();
        }

        private async Task PersistChangesAsync(ExecutionState state)
        {
            if (state.HasError || state.MedicalRecord == null) return;

            try
            {
                await _medicalRecordRepository.SaveChangesAsync();
                state.IsExecutionSuccess = true;
            }
            catch (DbUpdateConcurrencyException)
            {
                state.HasConcurrencyError = true;
                state.HasError = true;
                state.IsExecutionSuccess = false;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
            catch (Exception)
            {
                state.HasError = true;
                state.IsExecutionSuccess = false;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
        }

        private static ApiResponse<UpdateMedicalRecordResponse> CreateResponse(ExecutionState state)
        {
            if (state.HasConcurrencyError)
            {
                return ApiResponse<UpdateMedicalRecordResponse>.Fail(GeneralCode.APP_MESSAGE_5001.ToString());
            }

            if (state.HasError)
            {
                return ApiResponse<UpdateMedicalRecordResponse>.Fail(state.ErrorCode ?? GeneralCode.APP_MESSAGE_5001.ToString());
            }

            var response = new UpdateMedicalRecordResponse
            {
                MedicalRecordId = state.MedicalRecord?.Id.ToString() ?? string.Empty,
                PatientName = state.PatientName,
                RecordTypeLabel = state.RecordTypeLabel ?? GetRecordTypeLabel(state.MedicalRecord?.RecordType ?? RecordType.MS23_FUNDUS),
                AppointmentDate = state.MedicalRecord?.Appointment?.AppointmentDate.ToString("dd/MM/yyyy"),
                DoctorName = state.DoctorName,
                UpdatedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                IsSuccess = true
            };

            return ApiResponse<UpdateMedicalRecordResponse>.Success(GeneralCode.APP_MESSAGE_2006.ToString(), response);
        }

        private static string GetRecordTypeLabel(RecordType recordType)
        {
            return recordType switch
            {
                RecordType.MS21_TRAUMA => "Bệnh án mắt (Chấn thương)",
                RecordType.MS22_ANTERIOR => "Bệnh án mắt (Bán phần trước)",
                RecordType.MS23_FUNDUS => "Bệnh án mắt (Đáy mắt)",
                RecordType.MS24_GLAUCOMA => "Bệnh án mắt (Glôcôm)",
                RecordType.MS25_STRABISMUS_PTOSIS => "Bệnh án mắt (Lác, sụp mi)",
                RecordType.MS26_PEDIATRIC => "Bệnh án mắt (Mắt trẻ em)",
                _ => recordType.ToString()
            };
        }
    }

    /// <summary>
    /// Internal execution state for the v1 update flow.
    /// </summary>
    internal class ExecutionState
    {
        public MedicalRecord? MedicalRecord { get; set; }
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
        public string? RecordTypeLabel { get; set; }
        public bool HasError { get; set; }
        public bool HasConcurrencyError { get; set; }
        public bool IsExecutionSuccess { get; set; }
        public string? ErrorCode { get; set; }
    }
}
