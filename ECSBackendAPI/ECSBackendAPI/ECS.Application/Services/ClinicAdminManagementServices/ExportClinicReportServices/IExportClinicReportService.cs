using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicAdminManagementServices.ExportClinicReportServices
{
    /// <summary>
    /// Contract for service compiling full clinic report data for export.
    /// </summary>
    public interface IExportClinicReportService
    {
        /// <summary>
        /// Compiles full operational, staff, and medical records data for the authenticated clinic administrator.
        /// </summary>
        /// <returns>An <see cref="ApiResponse{ExportClinicReportResponse}"/> containing the clinic report dataset.</returns>
        Task<ApiResponse<ExportClinicReportResponse>> Process();
    }
}
