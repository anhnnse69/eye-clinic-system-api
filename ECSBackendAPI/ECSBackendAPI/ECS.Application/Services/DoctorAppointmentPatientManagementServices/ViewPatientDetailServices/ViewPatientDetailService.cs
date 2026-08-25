using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDetailServices
{
    /// <summary>
    /// Handles retrieving the full profile and appointment
    /// history of a specific patient for the requesting doctor.
    /// **Refactored (2026-07-14)**: prescription navigation removed because
    /// Prescription entity was dropped — prescription data now lives in the
    /// medical record's Cloudinary JSON payload.
    /// </summary>
    public class ViewPatientDetailService : IViewPatientDetailService
    {
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>
            _doctorRepo;
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>
            _patientRepo;
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext>
            _appointmentRepo;

        public ViewPatientDetailService(
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepo,
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientRepo,
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepo)
        {
            _doctorRepo = doctorRepo;
            _patientRepo = patientRepo;
            _appointmentRepo = appointmentRepo;
        }

        public async Task<ApiResponse<ViewPatientDetailResponse>> Process(
            Guid userId,
            Guid patientId)
        {
            var doctor = await ResolveActiveDoctorProfileAsync(userId);
            await EnsurePatientExistsAsync(patientId);
            var patient = await FetchPatientProfileAsync(patientId);
            var appointments = await FetchAppointmentHistoryAsync(patientId);
            var response = BuildResponse(patient, appointments, doctor.ClinicId);
            return CreateSuccessResponse(response);
        }

        // ── Private helpers ───────────────────────────────────────────

        private async Task<DoctorProfile> ResolveActiveDoctorProfileAsync(Guid userId)
        {
            var doctor = await _doctorRepo
                .FindByCondition(d => d.UserId == userId && d.IsActive)
                .FirstOrDefaultAsync();
            if (doctor is null)
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4008.ToString());

            return doctor;
        }

        private async Task EnsurePatientExistsAsync(Guid patientId)
        {
            var exists = await _patientRepo
                .FindByCondition(p => p.Id == patientId)
                .AnyAsync();
            if (!exists)
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4004.ToString());
        }

        private async Task<PatientProfile> FetchPatientProfileAsync(Guid patientId)
        {
            var patient = await _patientRepo
                .FindByCondition(p => p.Id == patientId)
                .Include(p => p.User)
                .FirstOrDefaultAsync();
            if (patient is null)
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4004.ToString());
            return patient;
        }

        /// <summary>
        /// Fetches all appointments across all clinics for the given patient.
        /// </summary>
        private async Task<List<Appointment>> FetchAppointmentHistoryAsync(Guid patientId)
        {
            return await _appointmentRepo
                .FindByCondition(a => a.PatientId == patientId)
                .Include(a => a.Service)
                .Include(a => a.Doctor).ThenInclude(d => d.Clinic)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Doctor).ThenInclude(d => d.Specialty)
                .Include(a => a.MedicalRecord)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        private static ViewPatientDetailResponse BuildResponse(
            PatientProfile patient,
            List<Appointment> appointments,
            Guid requestingDoctorClinicId)
        {
            return new ViewPatientDetailResponse
            {
                PatientId = patient.Id,
                FullName = patient.FullName,
                Gender = patient.Gender,
                Dob = patient.Dob,
                IdentityNumber = patient.IdentityNumber,
                Address = patient.Address,
                PhoneNumber = patient.PhoneNumber,
                BhytNumber = patient.BhytNumber,
                BloodType = patient.BloodType,
                Allergies = patient.Allergies,
                MedicalHistory = patient.MedicalHistory,
                AvatarUrl = patient.User?.AvatarUrl,
                Appointments = appointments.Select(a => MapAppointmentItem(a, requestingDoctorClinicId)).ToList(),
            };
        }

        private static AppointmentHistoryItem MapAppointmentItem(
            Appointment appointment,
            Guid requestingDoctorClinicId)
        {
            var doctor = appointment.Doctor;
            var clinic = doctor?.Clinic;
            var doctorUser = doctor?.User;
            var specialty = doctor?.Specialty;
            var doctorClinicId = doctor?.ClinicId;
            var isOtherClinic = doctorClinicId.HasValue && doctorClinicId.Value != requestingDoctorClinicId;

            string? doctorTitle = !string.IsNullOrWhiteSpace(doctor?.Title) ? doctor.Title.Trim() : "BS.";
            string? doctorFullName = doctorUser?.FullName;
            string doctorName = !string.IsNullOrWhiteSpace(doctorFullName)
                ? $"{doctorTitle} {doctorFullName}"
                : "N/A";

            return new AppointmentHistoryItem
            {
                AppointmentId = appointment.Id,
                AppointmentDate = appointment.AppointmentDate,
                Status = appointment.Status.ToString(),
                Symptoms = appointment.Symptoms,
                NoteReason = appointment.NoteReason,
                ChiefComplaint = appointment.MedicalRecord?.ChiefComplaint,
                ServiceName = appointment.Service?.ServiceName,
                ClinicId = doctorClinicId,
                ClinicName = clinic?.Name,
                ClinicAddress = clinic?.Address,
                DoctorId = appointment.DoctorId,
                DoctorName = doctorName,
                SpecialtyName = specialty?.Name,
                BookingSource = appointment.BookingSource,
                IsOtherClinic = isOtherClinic,
                MedicalRecord = MapMedicalRecord(appointment.MedicalRecord),
            };
        }

        /// <summary>
        /// Maps a <see cref="MedicalRecord"/> to a summary DTO. The detailed
        /// form data lives on Cloudinary and is exposed via <c>RecordDataUrl</c>
        /// in a separate detail endpoint.
        /// </summary>
        private static MedicalRecordSummary? MapMedicalRecord(MedicalRecord? mr)
        {
            if (mr is null) return null;
            return new MedicalRecordSummary
            {
                Id = mr.Id,
                RecordType = mr.RecordType,
                ChiefComplaint = mr.ChiefComplaint,
                DiagnosisMain = mr.Summary,
                DiagnosisComorbid = null,
                TreatmentPlan = mr.Notes,
                Notes = mr.Notes,
                IsLocked = mr.IsLocked,
                CreatedAt = mr.CreatedAt,
                Prescriptions = new List<PrescriptionSummary>(),
            };
        }

        private static ApiResponse<ViewPatientDetailResponse> CreateSuccessResponse(
            ViewPatientDetailResponse response)
        {
            return ApiResponse<ViewPatientDetailResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(), response);
        }
    }
}
