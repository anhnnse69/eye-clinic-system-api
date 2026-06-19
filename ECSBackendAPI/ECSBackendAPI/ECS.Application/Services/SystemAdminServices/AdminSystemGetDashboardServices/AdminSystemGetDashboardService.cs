using ECS.Application.Common.Response;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemGetDashboardServices
{
    /// <summary>
    /// Handles the business logic for retrieving and aggregating system-wide dashboard metrics.
    /// </summary>
    public class AdminSystemGetDashboardService : IAdminSystemGetDashboardService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes a new instance of <see cref="AdminSystemGetDashboardService"/> with database context.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public AdminSystemGetDashboardService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Orchestrates the aggregation of all dashboard components.
        /// </summary>
        /// <param name="request">The filtration criteria for the metrics.</param>
        /// <returns>An <see cref="ApiResponse{AdminSystemGetDashboardResponse}"/> wrapping the final data structure.</returns>
        public async Task<ApiResponse<AdminSystemGetDashboardResponse>> Process(AdminSystemGetDashboardRequest request)
        {
            // Step 1: Fetch operational and total clinics data (System-wide)
            var operationalClinics = await FetchOperationalClinics();
            // Step 2: Fetch pending clinic registration applications (System-wide)
            var pendingClinics = await FetchPendingApplications();
            // Step 3: Aggregate user accounts grouped by roles (Filterable by Clinic)
            var totalAccounts = await AggregateSystemAccounts(request.ClinicId);
            // Step 4: Extract appointment status metrics within optional date boundaries (Filterable by Clinic)
            var appointments = await ExtractAppointmentsMetrics(request);
            // Step 5: Tally total registered patients across the entire platform (Time filters excluded)
            var registeredPatients = await CountRegisteredPatients();
            // Step 6: Identify top requested healthcare services based on appointment volume (Filterable by Clinic)
            var topServices = await ResolveTopServices(request);
            // Step 7: Combine compiled metrics into a structured dashboard response payload
            var dashboardData = CombineMetricsToResponse(
                totalAccounts, operationalClinics, appointments, registeredPatients, pendingClinics, topServices);
            return CreateApiResponse(dashboardData);
        }

        /// <summary>
        /// Aggregates system accounts and counts them grouped by roles, optionally filtered by clinic.
        /// </summary>
        /// <param name="clinicId">The optional clinic identifier filter.</param>
        /// <returns>A configured <see cref="AdminSystemGetDashboardResponse.TotalSystemAccountsDto"/>.</returns>
        private async Task<AdminSystemGetDashboardResponse.TotalSystemAccountsDto> AggregateSystemAccounts(Guid? clinicId)
        {
            var baseQuery = _context.Users.AsNoTracking();
            if (clinicId.HasValue)
            {
                baseQuery = baseQuery.Where(u => u.StaffClinics!.Any(sc => sc.ClinicId == clinicId.Value));
            }
            var rawGroupedRoles = await baseQuery
                .GroupBy(u => u.Role)
                .Select(g => new { RawRole = g.Key, Count = g.Count() })
                .ToListAsync();
            var groupedRoles = rawGroupedRoles
                .Select(x => new
                {
                    NormalizedRole = NormalizeRoleName(x.RawRole),
                    x.Count
                })
                .Where(x => x.NormalizedRole != "PATIENT")
                .GroupBy(x => x.NormalizedRole)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
            return new AdminSystemGetDashboardResponse.TotalSystemAccountsDto
            {
                Total = groupedRoles.Values.Sum(),
                Doctor = groupedRoles.GetValueOrDefault("DOCTOR", 0),
                ClinicAdmin = groupedRoles.GetValueOrDefault("CLINIC_ADMIN", 0),
                Receptionist = groupedRoles.GetValueOrDefault("RECEPTIONIST", 0),
                SystemAdmin = groupedRoles.GetValueOrDefault("SYSTEM_ADMIN", 0)
            };
        }

        /// <summary>
        /// This helper function safely converts role values ​​from the database (whether it's the number "1" or the word "Doctor") to a standard uppercase string.
        /// </summary>
        private string NormalizeRoleName(object? role)
        {
            if (role == null) return string.Empty;
            string roleStr = role.ToString()!.Trim().ToUpper();
            return roleStr switch
            {
                "0" => "PATIENT",
                "1" => "DOCTOR",
                "2" => "CLINIC_ADMIN",
                "3" => "RECEPTIONIST",
                "4" => "SYSTEM_ADMIN",
                _ => roleStr 
            };
        }

        /// <summary>
        /// Fetches the count of active and total clinics registered in the system.
        /// </summary>
        /// <returns>A configured <see cref="AdminSystemGetDashboardResponse.OperationalClinicsDto"/>.</returns>
        private async Task<AdminSystemGetDashboardResponse.OperationalClinicsDto> FetchOperationalClinics()
        {
            var clinicsData = await _context.Clinics.AsNoTracking()
                .Select(c => new { c.IsActive })
                .ToListAsync();
            return new AdminSystemGetDashboardResponse.OperationalClinicsDto
            {
                Active = clinicsData.Count(c => c.IsActive),
                Total = clinicsData.Count
            };
        }

        /// <summary>
        /// Extracts and counts appointment metrics by status based on requested filters.
        /// </summary>
        /// <param name="request">The dashboard criteria containing filters.</param>
        /// <returns>A configured <see cref="AdminSystemGetDashboardResponse.AppointmentsDto"/>.</returns>
        private async Task<AdminSystemGetDashboardResponse.AppointmentsDto> ExtractAppointmentsMetrics(AdminSystemGetDashboardRequest request)
        {
            var query = _context.Appointments.AsNoTracking();
            if (request.ClinicId.HasValue)
            {
                query = query.Where(a => a.Doctor.ClinicId == request.ClinicId.Value);
            }
            if (request.StartDate.HasValue)
            {
                query = query.Where(a => a.AppointmentDate >= request.StartDate.Value);
            }
            if (request.EndDate.HasValue)
            {
                query = query.Where(a => a.AppointmentDate <= request.EndDate.Value);
            }
            var groupedStatuses = await query
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
            int pending = groupedStatuses.GetValueOrDefault(AppointmentStatus.PENDING, 0);
            int depositPaid = groupedStatuses.GetValueOrDefault(AppointmentStatus.DEPOSIT_PAID, 0);
            int booked = groupedStatuses.GetValueOrDefault(AppointmentStatus.BOOKED, 0);
            int arrived = groupedStatuses.GetValueOrDefault(AppointmentStatus.ARRIVED, 0);
            int inProgress = groupedStatuses.GetValueOrDefault(AppointmentStatus.IN_PROGRESS, 0);
            int completed = groupedStatuses.GetValueOrDefault(AppointmentStatus.COMPLETED, 0);
            int cancelled = groupedStatuses.GetValueOrDefault(AppointmentStatus.CANCELLED, 0);
            int noShow = groupedStatuses.GetValueOrDefault(AppointmentStatus.NOSHOW, 0);
            return new AdminSystemGetDashboardResponse.AppointmentsDto
            {
                Total = pending + depositPaid + booked + arrived + inProgress + completed + cancelled + noShow,
                Pending = pending,
                DepositPaid = depositPaid,
                Booked = booked,
                Arrived = arrived,
                InProgress = inProgress,
                Completed = completed,
                Cancelled = cancelled,
                NoShow = noShow
            };
        }

        /// <summary>
        /// Counts the total number of registered patient profiles across the system.
        /// </summary>
        /// <returns>The total number of records as an integer.</returns>
        private async Task<int> CountRegisteredPatients()
        {
            return await _context.PatientProfiles.AsNoTracking().CountAsync();
        }

        /// <summary>
        /// Retrieves the list of pending clinic registration requests ordered by date.
        /// </summary>
        /// <returns>A list of pending clinic registration data transfer objects.</returns>
        private async Task<List<AdminSystemGetDashboardResponse.PendingClinicDto>> FetchPendingApplications()
        {
            return await _context.ClinicRegistrationRequests.AsNoTracking()
                .Where(r => r.Status == "PENDING")
                .OrderByDescending(r => r.RequestedAt)
                .Select(r => new AdminSystemGetDashboardResponse.PendingClinicDto
                {
                    Id = r.Id.ToString(),
                    Name = r.ClinicName,
                    Owner = r.ContactName,
                    Date = r.RequestedAt.ToString("yyyy-MM-dd")
                })
                .ToListAsync();
        }

        /// <summary>
        /// Resolves and calculates the top 6 requested services with their popularity percentages.
        /// </summary>
        /// <param name="request">The dashboard criteria containing filters.</param>
        /// <returns>A list of top services with appointment count and calculated distribution ratio.</returns>
        private async Task<List<AdminSystemGetDashboardResponse.TopServiceDto>> ResolveTopServices(AdminSystemGetDashboardRequest request)
        {
            var query = _context.Appointments.AsNoTracking().Where(a => a.ServiceId != null);
            if (request.ClinicId.HasValue)
            {
                query = query.Where(a => a.Doctor.ClinicId == request.ClinicId.Value);
            }
            if (request.StartDate.HasValue)
            {
                query = query.Where(a => a.AppointmentDate >= request.StartDate.Value);
            }
            if (request.EndDate.HasValue)
            {
                query = query.Where(a => a.AppointmentDate <= request.EndDate.Value);
            }
            var servicesGrouped = await query
                .GroupBy(a => a.Service!.ServiceName)
                .Select(g => new
                {
                    Name = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(s => s.Count)
                .Take(6)
                .ToListAsync();
            int totalCount = servicesGrouped.Sum(s => s.Count);
            return servicesGrouped.Select(s =>
            {
                double percentage = totalCount > 0 ? ((double)s.Count / totalCount) * 100 : 0;
                return new AdminSystemGetDashboardResponse.TopServiceDto
                {
                    Name = s.Name,
                    Count = s.Count,
                    Growth = $"+{percentage:F1}%"
                };
            }).ToList();
        }

        /// <summary>
        /// Combines various metrics and data structures into a unified response object.
        /// </summary>
        /// <param name="totalAccounts">Account statistics data transfer object.</param>
        /// <param name="operationalClinics">Clinic status data transfer object.</param>
        /// <param name="appointments">Appointment metrics data transfer object.</param>
        /// <param name="registeredPatients">Total number of registered patients.</param>
        /// <param name="pendingClinics">List of pending clinic registrations.</param>
        /// <param name="topServices">List of top performing services data transfer objects.</param>
        /// <returns>A fully populated <see cref="AdminSystemGetDashboardResponse"/> instance.</returns>
        private AdminSystemGetDashboardResponse CombineMetricsToResponse(
            AdminSystemGetDashboardResponse.TotalSystemAccountsDto totalAccounts,
            AdminSystemGetDashboardResponse.OperationalClinicsDto operationalClinics,
            AdminSystemGetDashboardResponse.AppointmentsDto appointments,
            int registeredPatients,
            List<AdminSystemGetDashboardResponse.PendingClinicDto> pendingClinics,
            List<AdminSystemGetDashboardResponse.TopServiceDto> topServices)
        {
            return new AdminSystemGetDashboardResponse
            {
                TotalSystemAccounts = totalAccounts,
                OperationalClinics = operationalClinics,
                Appointments = appointments,
                RegisteredPatients = registeredPatients,
                PendingClinics = pendingClinics,
                TopServices = topServices
            };
        }

        /// <summary>
        /// Wraps the compiled dashboard payload into a success API response.
        /// </summary>
        /// <param name="data">The compiled dashboard data response instance.</param>
        /// <returns>A configured <see cref="ApiResponse{AdminSystemGetDashboardResponse}"/>.</returns>
        private ApiResponse<AdminSystemGetDashboardResponse> CreateApiResponse(AdminSystemGetDashboardResponse data)
        {
            return ApiResponse<AdminSystemGetDashboardResponse>.Success("APP_MESSAGE_2000", data);
        }
    }
}