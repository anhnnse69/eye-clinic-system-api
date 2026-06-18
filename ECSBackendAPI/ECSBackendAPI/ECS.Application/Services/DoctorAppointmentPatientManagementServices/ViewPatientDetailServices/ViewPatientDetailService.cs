using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Prescriptions;
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
    /// </summary>
    public class ViewPatientDetailService : IViewPatientDetailService
    {
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>
            _doctorRepo;
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>
            _patientRepo;
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext>
            _appointmentRepo;

        /// <summary>
        /// Initializes a new instance of
        /// <see cref="ViewPatientDetailService"/>.
        /// </summary>
        public ViewPatientDetailService(
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepo,
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientRepo,
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepo)
        {
            _doctorRepo = doctorRepo;
            _patientRepo = patientRepo;
            _appointmentRepo = appointmentRepo;
        }

        /// <summary>
        /// Resolves the doctor profile, verifies the doctor–patient
        /// relationship, then returns the patient's full profile
        /// together with their appointment and medical record history.
        /// </summary>
        /// <param name="userId">
        /// Identifier of the user account linked to the doctor profile.
        /// </param>
        /// <param name="patientId">
        /// Identifier of the patient whose profile is being requested.
        /// </param>
        /// <returns>
        /// A successful response containing the patient detail.
        /// </returns>
        public async Task<ApiResponse<ViewPatientDetailResponse>> Process(
            Guid userId,
            Guid patientId)
        {
            var doctor = await ResolveActiveDoctorProfileAsync(userId);
            await EnsureDoctorPatientRelationshipAsync(doctor.Id, patientId);
            var patient = await FetchPatientProfileAsync(patientId);
            var appointments = await FetchAppointmentHistoryAsync(doctor.Id, patientId);
            var response = BuildResponse(patient, appointments);
            return CreateSuccessResponse(response);
        }

        // ── Private helpers ───────────────────────────────────────────

        /// <summary>
        /// Resolves the active doctor profile for the specified user.
        /// Throws when not found.
        /// </summary>
        /// <param name="userId">
        /// Identifier of the user account.
        /// </param>
        /// <returns>
        /// The resolved <see cref="DoctorProfile"/>.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when no active doctor profile is found
        /// for the given user.
        /// </exception>
        private async Task<DoctorProfile> ResolveActiveDoctorProfileAsync(
            Guid userId)
        {
            var doctor = await _doctorRepo
                .FindByCondition(d => d.UserId == userId && d.IsActive)
                .FirstOrDefaultAsync();
            if (doctor is null)
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4008.ToString());

            return doctor;
        }

        /// <summary>
        /// Verifies that at least one appointment exists between
        /// the doctor and the patient, establishing a valid
        /// access relationship.
        /// Throws when no relationship is found.
        /// </summary>
        /// <param name="doctorId">Identifier of the doctor profile.</param>
        /// <param name="patientId">Identifier of the patient profile.</param>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when no shared appointment is found.
        /// </exception>
        private async Task EnsureDoctorPatientRelationshipAsync(
            Guid doctorId,
            Guid patientId)
        {
            var hasRelation = await _appointmentRepo
                .FindByCondition(a =>
                    a.DoctorId == doctorId &&
                    a.PatientId == patientId)
                .AnyAsync();
            if (!hasRelation)
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4004.ToString());
        }

        /// <summary>
        /// Fetches the patient profile, including the linked user
        /// account for avatar resolution.
        /// Throws when the patient is not found.
        /// </summary>
        /// <param name="patientId">Identifier of the patient profile.</param>
        /// <returns>The resolved <see cref="PatientProfile"/>.</returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the patient profile does not exist.
        /// </exception>
        private async Task<PatientProfile> FetchPatientProfileAsync(
            Guid patientId)
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
        /// Fetches all appointments shared between the doctor and the
        /// patient, including the associated service, medical record,
        /// prescriptions, and prescription line items.
        /// Results are ordered from most recent to oldest.
        /// </summary>
        /// <param name="doctorId">Identifier of the doctor profile.</param>
        /// <param name="patientId">Identifier of the patient profile.</param>
        /// <returns>
        /// An ordered list of <see cref="Appointment"/> entities
        /// with their full navigation tree loaded.
        /// </returns>
        private async Task<List<Appointment>> FetchAppointmentHistoryAsync(
            Guid doctorId,
            Guid patientId)
        {
            return await _appointmentRepo
                .FindByCondition(a =>
                    a.DoctorId == doctorId &&
                    a.PatientId == patientId)
                .Include(a => a.Service)
                .Include(a => a.MedicalRecord)
                    .ThenInclude(mr => mr.Prescriptions)
                        .ThenInclude(rx => rx.Items)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        /// <summary>
        /// Maps the patient profile and appointment list
        /// into the API response DTO.
        /// </summary>
        /// <param name="patient">The resolved patient profile.</param>
        /// <param name="appointments">
        /// The ordered list of appointments with medical records.
        /// </param>
        /// <returns>
        /// A fully populated <see cref="ViewPatientDetailResponse"/>.
        /// </returns>
        private static ViewPatientDetailResponse BuildResponse(
            PatientProfile patient,
            List<Appointment> appointments)
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
                Appointments = appointments.Select(MapAppointmentItem).ToList(),
            };
        }

        /// <summary>
        /// Maps a single <see cref="Appointment"/> entity to an
        /// <see cref="AppointmentHistoryItem"/> DTO, including its
        /// medical record when present.
        /// </summary>
        /// <param name="appointment">The appointment entity to map.</param>
        /// <returns>A populated <see cref="AppointmentHistoryItem"/>.</returns>
        private static AppointmentHistoryItem MapAppointmentItem(
            Appointment appointment)
        {
            return new AppointmentHistoryItem
            {
                AppointmentId = appointment.Id,
                AppointmentDate = appointment.AppointmentDate,
                Status = appointment.Status.ToString(),
                Symptoms = appointment.Symptoms,
                ServiceName = appointment.Service?.ServiceName,
                MedicalRecord = MapMedicalRecord(appointment.MedicalRecord),
            };
        }

        /// <summary>
        /// Maps a <see cref="MedicalRecord"/> entity to a
        /// <see cref="MedicalRecordSummary"/> DTO.
        /// Returns <c>null</c> when no record exists.
        /// </summary>
        /// <param name="mr">
        /// The medical record entity, or <c>null</c>.
        /// </param>
        /// <returns>
        /// A populated <see cref="MedicalRecordSummary"/>,
        /// or <c>null</c>.
        /// </returns>
        private static MedicalRecordSummary? MapMedicalRecord(
            MedicalRecord? mr)
        {
            if (mr is null) return null;
            return new MedicalRecordSummary
            {
                Id = mr.Id,
                RecordType = mr.RecordType,
                ChiefComplaint = mr.ChiefComplaint,
                DiagnosisMain = mr.DiagnosisMain,
                DiagnosisComorbid = mr.DiagnosisComorbid,
                TreatmentPlan = mr.TreatmentPlan,
                Notes = mr.Notes,
                IsLocked = mr.IsLocked,
                CreatedAt = mr.CreatedAt,
                Prescriptions = mr.Prescriptions
                    .Select(MapPrescription)
                    .ToList(),
            };
        }

        /// <summary>
        /// Maps a <see cref="Prescription"/> entity to a
        /// <see cref="PrescriptionSummary"/> DTO.
        /// </summary>
        /// <param name="rx">The prescription entity to map.</param>
        /// <returns>A populated <see cref="PrescriptionSummary"/>.</returns>
        private static PrescriptionSummary MapPrescription(
            Prescription rx)
        {
            return new PrescriptionSummary
            {
                Id = rx.Id,
                Notes = rx.Notes,
                CreatedAt = rx.CreatedAt,
                Items = rx.Items.Select(MapPrescriptionItemResponse).ToList(),
            };
        }

        /// <summary>
        /// Maps a <see cref="Domain.Entities.Prescriptions.PrescriptionItem"/> entity to a
        /// <see cref="PrescriptionItem"/> DTO.
        /// </summary>
        /// <param name="item">The prescription line item to map.</param>
        /// <returns>A populated <see cref="PrescriptionItem"/>.</returns>
        private static PrescriptionItemResponse MapPrescriptionItemResponse(
            PrescriptionItem item)
        {
            return new PrescriptionItemResponse
            {
                Id = item.Id,
                MedicineName = item.MedicineName,
                Dosage = item.Dosage,
                Frequency = item.Frequency,
                DurationDays = item.DurationDays,
                Quantity = item.Quantity,
                Instruction = item.Instruction,
            };
        }

        /// <summary>
        /// Wraps the response DTO in a standard success
        /// <see cref="ApiResponse{T}"/>.
        /// </summary>
        /// <param name="response">The response DTO to wrap.</param>
        /// <returns>
        /// A successful <see cref="ApiResponse{ViewPatientDetailResponse}"/>.
        /// </returns>
        private static ApiResponse<ViewPatientDetailResponse> CreateSuccessResponse(
            ViewPatientDetailResponse response)
        {
            return ApiResponse<ViewPatientDetailResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(), response);
        }
    }
}