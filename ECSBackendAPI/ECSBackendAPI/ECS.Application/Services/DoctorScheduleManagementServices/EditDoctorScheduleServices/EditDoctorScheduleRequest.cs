namespace ECS.Application.Services.DoctorScheduleManagementServices.EditDoctorScheduleServices
{
    /// <summary>
    /// Request to edit an existing doctor schedule: change the work date
    /// and/or the assigned room.
    /// </summary>
    public class EditDoctorScheduleRequest
    {
        public DateOnly? WorkDate { get; set; }
        public Guid? RoomId { get; set; }
    }
}
