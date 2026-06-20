using ECS.Application.Common.Response;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistPayDepositServices
{
    /// <summary>
    /// Handles the business orchestration flow for verifying eligibility bounds, executing structural persistence states, and formatting response telemetry models.
    /// </summary>
    public class ReceptionistPayDepositService : IReceptionistPayDepositService
    {
        private readonly IRepositoryBaseAsync<Appointment, Guid, AppDbContext> _appointmentRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistPayDepositService"/> with isolated data write-access gateways.
        /// </summary>
        /// <param name="appointmentRepo">The read-write transactional repository interface for managing appointment state aggregates.</param>
        public ReceptionistPayDepositService(IRepositoryBaseAsync<Appointment, Guid, AppDbContext> appointmentRepo)
        {
            _appointmentRepo = appointmentRepo;
        }

        /// <summary>
        /// Acts as the core execution orchestrator regulating the structural pipeline flow and sequencing validation tokens and data persistence blocks without inline branch operations.
        /// </summary>
        /// <param name="request">The filtration and context argument bundle containing the specific appointment identity.</param>
        /// <returns>A structured <see cref="ApiResponse{T}"/> packing matched transactional completion presentation nodes.</returns>
        public async Task<ApiResponse<ReceptionistPayDepositResponse>> Process(ReceptionistPayDepositRequest request)
        {
            // Step 1: Fetch the targeted aggregate root record from the backend database layer using the provided unique identifier
            var appointment = await _appointmentRepo.GetByIdAsync(request.AppointmentId);
            // Step 2: Evaluate strict financial eligibility boundaries, transaction statuses, and chronological constraints
            var validationError = ValidateEligibility(appointment);
            // Step 3: Commit the validated operational mutations into storage layers and map the final entity graph into flattened UI models
            return await ExecuteSavingAndMapping(appointment, validationError);
        }

        /// <summary>
        /// Evaluates strict domain constraints and tenancy rules across chronological targets, financial blocks, and progress milestone indicators.
        /// </summary>
        /// <param name="appointment">The targeted aggregate root database query context to be verified.</param>
        /// <returns>An optional <see cref="ApiResponse{T}"/> encapsulation representing broken validation constraints or null if criteria boundaries clear.</returns>
        private ApiResponse<ReceptionistPayDepositResponse>? ValidateEligibility(Appointment? appointment)
        {
            if (appointment == null)
                return ApiResponse<ReceptionistPayDepositResponse>.Fail("APPOINTMENT_NOT_FOUND");
            DateTime todayVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;
            if (appointment.AppointmentDate.Date != todayVn)
                return ApiResponse<ReceptionistPayDepositResponse>.Fail("ERROR_NOT_TODAY");
            if (appointment.DepositPaid)
                return ApiResponse<ReceptionistPayDepositResponse>.Fail("DEPOSIT_ALREADY_PAID");
            if (appointment.Status != AppointmentStatus.CONFIRMED && appointment.Status != AppointmentStatus.BOOKED)
                return ApiResponse<ReceptionistPayDepositResponse>.Fail("INVALID_STATUS_FOR_DEPOSIT");
            return null;
        }

        /// <summary>
        /// Commits verified logical updates to transactional repository aggregates and transforms internal data graphs into flattened presentation models.
        /// </summary>
        /// <param name="appointment">The mutable targeted entity structure tracking back-end storage layouts.</param>
        /// <param name="validationError">The short-circuit error payload generated from early evaluation pipelines.</param>
        /// <returns>A standardized API success packet wrapping operational confirmation timelines or short-circuited error logs.</returns>
        private async Task<ApiResponse<ReceptionistPayDepositResponse>> ExecuteSavingAndMapping(
            Appointment? appointment,
            ApiResponse<ReceptionistPayDepositResponse>? validationError)
        {
            if (validationError != null) return validationError;
            appointment!.DepositPaid = true;
            appointment.UpdatedAt = DateTime.UtcNow;
            await _appointmentRepo.UpdateAsync(appointment);
            await _appointmentRepo.SaveChangesAsync();
            var responseData = new ReceptionistPayDepositResponse
            {
                AppointmentId = appointment.Id.ToString(),
                DepositPaid = appointment.DepositPaid,
                UpdatedAt = appointment.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
            return ApiResponse<ReceptionistPayDepositResponse>.Success("APP_MESSAGE_2000", responseData);
        }
    }
}