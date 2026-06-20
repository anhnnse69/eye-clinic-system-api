namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorClinicRoomsServices
{
    /// <summary>
    /// Represents a clinic room available for doctor scheduling.
    /// </summary>
    public class ClinicRoomResponse
    {
        public Guid RoomId { get; set; }
        public string RoomName { get; set; } = null!;
        public string RoomType { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
