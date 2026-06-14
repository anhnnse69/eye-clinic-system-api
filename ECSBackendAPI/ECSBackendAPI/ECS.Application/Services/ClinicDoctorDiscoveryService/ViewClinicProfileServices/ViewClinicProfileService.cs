using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services
    .ClinicDoctorDiscoveryService
    .ViewClinicProfileServices
{
    /// <summary>
    /// Handles retrieving the full profile of a clinic,
    /// including doctors, services, and public feedbacks.
    /// </summary>
    public class ViewClinicProfileService
        : IViewClinicProfileService
    {
        private readonly IRepositoryQueryBase<
            Clinic,
            Guid,
            AppDbContext> _clinicRepository;

        private readonly IRepositoryQueryBase<
            DoctorProfile,
            Guid,
            AppDbContext> _doctorRepository;

        private readonly IRepositoryQueryBase<
            Service,
            Guid,
            AppDbContext> _serviceRepository;

        private readonly IRepositoryQueryBase<
            Feedback,
            Guid,
            AppDbContext> _feedbackRepository;

        /// <summary>
        /// Initializes a new instance of
        /// <see cref="ViewClinicProfileService"/>.
        /// </summary>
        public ViewClinicProfileService(
            IRepositoryQueryBase<
                Clinic,
                Guid,
                AppDbContext> clinicRepository,
            IRepositoryQueryBase<
                DoctorProfile,
                Guid,
                AppDbContext> doctorRepository,
            IRepositoryQueryBase<
                Service,
                Guid,
                AppDbContext> serviceRepository,
            IRepositoryQueryBase<
                Feedback,
                Guid,
                AppDbContext> feedbackRepository)
        {
            _clinicRepository = clinicRepository;
            _doctorRepository = doctorRepository;
            _serviceRepository = serviceRepository;
            _feedbackRepository = feedbackRepository;
        }

        /// <summary>
        /// Retrieves the clinic profile data and its related information,
        /// including doctors, services, and public feedbacks.
        /// </summary>
        /// <param name="clinicId">
        /// Identifier of the clinic.
        /// </param>
        /// <returns>
        /// A successful response containing the clinic profile.
        /// </returns>
        public async Task<ApiResponse<ViewClinicProfileResponse>> Process(
            Guid clinicId)
        {
            var clinic = await GetClinicOrThrowAsync(clinicId);
            var doctors = await FetchDoctorsAsync(clinicId);
            var services = await FetchServicesAsync(clinicId);
            var feedbacks = await FetchFeedbacksAsync(clinicId);
            var response = BuildResponse(
                clinic,
                doctors,
                services,
                feedbacks);
            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Retrieves an active clinic by identifier.
        /// Throws an exception when the clinic does not exist
        /// or has been deactivated.
        /// </summary>
        /// <param name="clinicId">
        /// Identifier of the clinic.
        /// </param>
        /// <returns>
        /// The clinic entity.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the clinic cannot be found.
        /// </exception>
        private async Task<Clinic> GetClinicOrThrowAsync(
            Guid clinicId)
        {
            var clinic = await _clinicRepository
                .FindByCondition(c =>
                    c.Id == clinicId &&
                    c.IsActive)
                .FirstOrDefaultAsync();
            return clinic
                ?? throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// Retrieves all active doctors belonging to
        /// the specified clinic.
        /// </summary>
        /// <param name="clinicId">
        /// Clinic identifier.
        /// </param>
        /// <returns>
        /// Collection of doctor items.
        /// </returns>
        private async Task<List<ClinicDoctorItem>>
            FetchDoctorsAsync(
                Guid clinicId)
        {
            return await _doctorRepository
                .FindByCondition(d =>
                    d.ClinicId == clinicId &&
                    d.IsActive)
                .OrderBy(d => d.User.FullName)
                .Select(d => new ClinicDoctorItem
                {
                    Id = d.Id,
                    FullName = d.User.FullName,
                    AvatarUrl = d.User.AvatarUrl,
                    Title = d.Title,
                    Specialty = d.Specialty != null
                        ? d.Specialty.Name
                        : null,
                    ExperienceYears = d.ExperienceYears,
                    RatingAvg = d.RatingAvg,
                    ReviewCount = d.ReviewCount
                })
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves all active services
        /// provided by the clinic.
        /// </summary>
        /// <param name="clinicId">
        /// Clinic identifier.
        /// </param>
        /// <returns>
        /// Collection of service items.
        /// </returns>
        private async Task<List<ClinicServiceItem>>
            FetchServicesAsync(
                Guid clinicId)
        {
            return await _serviceRepository
                .FindByCondition(s =>
                    s.ClinicId == clinicId &&
                    s.IsActive)
                .OrderBy(s => s.ServiceName)
                .Select(s => new ClinicServiceItem
                {
                    Id = s.Id,
                    ServiceName = s.ServiceName,
                    Price = s.Price,
                    DurationMinutes = s.DurationMinutes
                })
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves the latest public feedbacks
        /// of the clinic.
        /// </summary>
        /// <param name="clinicId">
        /// Clinic identifier.
        /// </param>
        /// <returns>
        /// Collection of feedback items.
        /// </returns>
        private async Task<List<ClinicFeedbackItem>>
            FetchFeedbacksAsync(
                Guid clinicId)
        {
            return await _feedbackRepository
                .FindByCondition(f =>
                    f.ClinicId == clinicId &&
                    f.IsPublic)
                .OrderByDescending(f => f.CreatedAt)
                .Take(20)
                .Select(f => new ClinicFeedbackItem
                {
                    Id = f.Id,
                    PatientName = f.Patient.FullName,
                    RatingDoctor = f.RatingDoctor,
                    RatingClinic = f.RatingClinic,
                    Comment = f.Comment,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();
        }

        /// <summary>
        /// Builds the clinic profile response object.
        /// </summary>
        /// <param name="clinic">
        /// Clinic entity.
        /// </param>
        /// <param name="doctors">
        /// Clinic doctors.
        /// </param>
        /// <param name="services">
        /// Clinic services.
        /// </param>
        /// <param name="feedbacks">
        /// Clinic feedbacks.
        /// </param>
        /// <returns>
        /// Clinic profile response.
        /// </returns>
        private static ViewClinicProfileResponse BuildResponse(
            Clinic clinic,
            List<ClinicDoctorItem> doctors,
            List<ClinicServiceItem> services,
            List<ClinicFeedbackItem> feedbacks)
        {
            return new ViewClinicProfileResponse
            {
                Id = clinic.Id,
                Name = clinic.Name,
                Address = clinic.Address,
                Phone = clinic.Phone,
                Email = clinic.Email,
                LogoUrl = clinic.LogoUrl,
                Description = clinic.Description,
                RatingAvg = clinic.RatingAvg,
                ReviewCount = clinic.ReviewCount,
                Doctors = doctors,
                Services = services,
                Feedbacks = feedbacks
            };
        }

        /// <summary>
        /// Creates a successful API response.
        /// </summary>
        /// <param name="response">
        /// Response payload.
        /// </param>
        /// <returns>
        /// Success API response.
        /// </returns>
        private static ApiResponse<ViewClinicProfileResponse>
            CreateSuccessResponse(
                ViewClinicProfileResponse response)
        {
            return ApiResponse<ViewClinicProfileResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}