namespace ECS.Application.Services.DoctorScheduleManagementServices.BlockUnblockSlotServices
{
    /// <summary>
    /// Request to block or unblock a doctor's time slot.
    /// </summary>
    public class BlockUnblockSlotRequest
    {
        public bool Block { get; set; }
    }
}
