using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicDoctorDiscoveryService.SearchClinicDoctorServices
{
    /// <summary>
    /// Handles searching clinics and doctors by keyword.
    /// </summary>
    public class SearchClinicDoctorService
        : ISearchClinicDoctorService
    {
        private readonly IRepositoryQueryBase<
            Clinic,
            Guid,
            AppDbContext> _clinicQueryRepository;
        private readonly IRepositoryQueryBase<
            DoctorProfile,
            Guid,
            AppDbContext> _doctorQueryRepository;
        public SearchClinicDoctorService(
            IRepositoryQueryBase<
                Clinic,
                Guid,
                AppDbContext> clinicQueryRepository,
            IRepositoryQueryBase<
                DoctorProfile,
                Guid,
                AppDbContext> doctorQueryRepository)
        {
            _clinicQueryRepository = clinicQueryRepository;
            _doctorQueryRepository = doctorQueryRepository;
        }

        /// <inheritdoc/>
        public async Task<ApiResponse<SearchClinicDoctorResponse>> Process(
            SearchClinicDoctorRequest request)
        {
            var keyword = NormalizeKeyword(request.Keyword);
            var clinics = await SearchClinicsAsync(keyword);
            var doctors = await SearchDoctorsAsync(keyword);
            var response = BuildResponse(
                clinics,
                doctors);
            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Normalizes keyword for searching.
        /// </summary>
        private static string NormalizeKeyword(
            string? keyword)
        {
            return string.IsNullOrWhiteSpace(keyword)
                ? string.Empty
                : keyword.Trim().ToLower();
        }

        /// <summary>
        /// Searches active clinics by keyword.
        /// </summary>
        private async Task<List<ClinicSearchItem>>
            SearchClinicsAsync(
                string keyword)
        {
            return await _clinicQueryRepository
                .FindByCondition(c =>
                    c.IsActive &&
                    (keyword == string.Empty ||
                     c.Name.ToLower().Contains(keyword)))
                .OrderBy(c => c.Name)
                .Select(c => new ClinicSearchItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    Phone = c.Phone,
                    Email = c.Email,
                    LogoUrl = c.LogoUrl,
                    Description = c.Description,
                    RatingAvg = c.RatingAvg,
                    ReviewCount = c.ReviewCount ?? 0
                })
                .ToListAsync();
        }

        /// <summary>
        /// Searches active doctors by keyword.
        /// </summary>
        private async Task<List<DoctorSearchItem>>
            SearchDoctorsAsync(
                string keyword)
        {
            return await _doctorQueryRepository
                .FindByCondition(d => d.IsActive)
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Include(d => d.Clinic)
                .Where(d =>
                    keyword == string.Empty ||
                    d.User.FullName.ToLower().Contains(keyword))
                .OrderBy(d => d.User.FullName)
                .Select(d => new DoctorSearchItem
                {
                    Id = d.Id,
                    FullName = d.User.FullName,
                    AvatarUrl = d.User.AvatarUrl,
                    Title = d.Title,
                    Specialty = d.Specialty != null
                        ? d.Specialty.Name
                        : null,
                    ClinicName = d.Clinic.Name,
                    ExperienceYears = d.ExperienceYears,
                    Bio = d.Bio,
                    RatingAvg = d.RatingAvg,
                    ReviewCount = d.ReviewCount ?? 0
                })
                .ToListAsync();
        }

        /// <summary>
        /// Builds search response object.
        /// </summary>
        private static SearchClinicDoctorResponse BuildResponse(
            List<ClinicSearchItem> clinics,
            List<DoctorSearchItem> doctors)
        {
            return new SearchClinicDoctorResponse
            {
                Clinics = clinics,
                Doctors = doctors
            };
        }

        /// <summary>
        /// Creates success response.
        /// </summary>
        private static ApiResponse<SearchClinicDoctorResponse>
            CreateSuccessResponse(
                SearchClinicDoctorResponse response)
        {
            return ApiResponse<SearchClinicDoctorResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}