namespace ECS.Application.Services.DoctorScheduleManagementServices.GetActiveRoomsServices
{
    /// <summary>
    /// Represents an active facility room available for the receptionist
    /// to assign when creating a doctor schedule, scoped to their clinic.
    /// </summary>
    public class ClinicRoomResponse
    {
        public Guid RoomId { get; set; }
        public string RoomName { get; set; } = null!;
        public string? RoomType { get; set; }
        public bool IsActive { get; set; }
    }
}
