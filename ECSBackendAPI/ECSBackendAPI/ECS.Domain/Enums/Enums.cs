namespace ECS.Domain.Enums
{
    public enum UserRole { PATIENT, DOCTOR, CLINIC_ADMIN, RECEPTIONIST, SYSTEM_ADMIN }
    public enum StaffRole { DOCTOR, RECEPTIONIST, CLINIC_ADMIN }
    public enum Gender { MALE, FEMALE, OTHER }
    public enum AppointmentStatus { PENDING, DEPOSIT_PAID, CONFIRMED, BOOKED, ARRIVED, IN_PROGRESS, COMPLETED, CANCELLED, NOSHOW }
    public enum QueueStatus { WAITING, CALLING, COMPLETED }
    public enum ShiftType { MORNING, AFTERNOON, EVENING }
    public enum SlotStatus { AVAILABLE, BOOKED, BLOCKED }
    public enum EyeSide { RIGHT, LEFT, BOTH }
    public enum RecordType { MS21_TRAUMA, MS22_ANTERIOR, MS23_FUNDUS, MS24_GLAUCOMA, MS25_STRABISMUS_PTOSIS, MS26_PEDIATRIC }
}