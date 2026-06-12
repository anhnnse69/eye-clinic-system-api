using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicAppointmentServices
{
    /// <summary>
    /// Response object representing an appointment's presentation layer data details.
    /// </summary>
    public class GetClinicAppointmentResponse
    {
        public string Id_appointment { get; set; } = null!;
        public string PatientName { get; set; } = null!;
        public string PatientPhone { get; set; } = null!;
        public string DoctorName { get; set; } = null!;
        public string ServiceName { get; set; } = "N/A";
        public string AppointmentDate { get; set; } = null!;
        public string TimeSlot { get; set; } = null!;
        public string Status { get; set; } = null!; 
        public decimal DepositAmount { get; set; }
        public bool DepositPaid { get; set; }
        public string BookingSource { get; set; } = null!;
        public string Symptoms { get; set; } = "N/A";
        public string CreatedAt { get; set; } = null!;
    }
}
