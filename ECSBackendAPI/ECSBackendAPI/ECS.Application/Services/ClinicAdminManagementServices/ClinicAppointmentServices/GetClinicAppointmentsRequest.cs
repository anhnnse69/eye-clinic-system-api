using System;
using System.Collections.Generic;
using System.Text;
using ECS.Domain.Enums;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicAppointmentServices
{
    /// <summary>
    /// Request object containing parameters for filtering and paginating the appointments list for Clinic Admin.
    /// </summary>
    public class GetClinicAppointmentsRequest
    {
        // Search keyword (Patient Name, Doctor Name)
        public string? SearchTerm { get; set; }

        // Filter by Appointment Status (PENDING, BOOKED, COMPLETED, etc.)
        public AppointmentStatus? Status { get; set; }

        // Filter by specific Appointment Date
        public DateTime? AppointmentDate { get; set; }

        // Paging page index tracker
        public int PageNumber { get; set; } = 1;

        // Upper bound limit size of elements inside a solitary segment page boundary
        public int PageSize { get; set; } = 10;
    }
}
