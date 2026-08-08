namespace ECS.Application.Services.ClinicAdminManagementServices.ExportClinicReportServices
{
    /// <summary>
    /// Represents the full structured export dataset for a clinic.
    /// </summary>
    public class ExportClinicReportResponse
    {
        public ClinicProfileReportDto ClinicInfo { get; set; } = new();
        public List<ClinicStaffReportDto> Staffs { get; set; } = new();
        public List<ClinicMedicalRecordReportDto> MedicalRecords { get; set; } = new();
        public List<ClinicServiceReportDto> Services { get; set; } = new();
        public List<ClinicRoomReportDto> Rooms { get; set; } = new();
    }

    public class ClinicProfileReportDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public TimeOnly OpenTime { get; set; }
        public TimeOnly CloseTime { get; set; }
        public decimal? RatingAvg { get; set; }
        public int? ReviewCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalStaffs { get; set; }
        public int TotalMedicalRecords { get; set; }
        public int TotalServices { get; set; }
        public int TotalRooms { get; set; }
    }

    public class ClinicStaffReportDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ClinicMedicalRecordReportDto
    {
        public Guid RecordId { get; set; }
        public Guid AppointmentId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public string PatientGender { get; set; } = string.Empty;
        public string PatientDob { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string RecordType { get; set; } = string.Empty;
        public string? ChiefComplaint { get; set; }
        public string? Summary { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? FinalizedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ClinicServiceReportDto
    {
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsActive { get; set; }
    }

    public class ClinicRoomReportDto
    {
        public Guid RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string? RoomType { get; set; }
        public bool IsActive { get; set; }
    }
}
