namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetAvailableSlotsServices
{
    /// <summary>
    /// Data transfer object representing a row within the doctor shift and scheduler matrix.
    /// </summary>
    public class GetAvailableSlotsResponse
    {
        public string Id { get; set; } = null!;
        public string DoctorId { get; set; } = null!;
        public string? RoomId { get; set; }
        public string ShiftType { get; set; } = null!;
        public string DoctorName { get; set; } = null!;
        public string? Title { get; set; }
        public string SpecialtyName { get; set; } = null!;
        public string RoomName { get; set; } = null!;
        public List<TimeSlotResponse> Slots { get; set; } = new();
    }

    /// <summary>
    /// Data transfer object defining individual localized slot segments inside shifts.
    /// </summary>
    public class TimeSlotResponse
    {
        public string Id { get; set; } = null!;
        public string StartTime { get; set; } = null!;
        public string EndTime { get; set; } = null!;
        public int MaxPatients { get; set; }
        public int CurrentPatients { get; set; }
        public string Status { get; set; } = null!;
    }
}