namespace ECS.Domain.Enums
{
    /// <summary>
    /// Message code enum for system responses
    /// </summary>
    public enum GeneralCode
    {
        // Success codes (2xxx series)
        APP_MESSAGE_2000, // General success message
        APP_MESSAGE_2001, // Appointment booked successfully
        APP_MESSAGE_2002, // Appointment is pending confirmation
        APP_MESSAGE_2003, // Appointment has been confirmed
        APP_MESSAGE_2004, // Appointment cancelled successfully
        APP_MESSAGE_2005, // Patient record created successfully
        APP_MESSAGE_2006, // Patient information updated successfully
        APP_MESSAGE_2007, // Appointment status updated successfully
        APP_MESSAGE_2008, // Password changed successfully

        // Client error codes (4xxx series)
        APP_MESSAGE_4000, // General client error (considered ambiguous, use APP_MESSAGE_4019 for validation errors)
        APP_MESSAGE_4001, // Invalid phone number format or length
        APP_MESSAGE_4002, // Invalid date of birth (e.g., future date or incorrect format)
        APP_MESSAGE_4003, // Required field is missing in the request
        APP_MESSAGE_4004, // Appointment time is invalid (e.g., not within working hours)
        APP_MESSAGE_4005, // Appointment time is in the past
        APP_MESSAGE_4006, // Selected doctor is not available at the requested time
        APP_MESSAGE_4007, // The time slot is already booked by another appointment
        APP_MESSAGE_4008, // Specified facility not found in the system
        APP_MESSAGE_4009, // Specified specialty not found in the system
        APP_MESSAGE_4010, // Specified patient not found in the system
        APP_MESSAGE_4011, // Specified doctor not found in the system
        APP_MESSAGE_4012, // Specified appointment not found in the system
        APP_MESSAGE_4013, // Invalid status for the appointment (e.g., cannot change completed status)
        APP_MESSAGE_4014, // User does not have permission to access this resource or perform this action
        APP_MESSAGE_4015, // Duplicate appointment detected (e.g., same patient, time, doctor)
        APP_MESSAGE_4016, // Invalid login credentials (wrong username or password)
        APP_MESSAGE_4017, // Email address already exists in the system
        APP_MESSAGE_4018, // Phone number already in use by another user
        APP_MESSAGE_4019, // General validation error (Model state invalid)
        APP_MESSAGE_4020, // Specified user not found in the system
        APP_MESSAGE_4021, // Cannot delete active admin user
        APP_MESSAGE_4022, // Invalid role change
        APP_MESSAGE_4023, // NameVi address already exists in the system
        APP_MESSAGE_4024, // Input contains potentially dangerous HTML content (XSS attempt detected)
        APP_MESSAGE_4025, // Input contains potentially dangerous SQL/NoSQL injection patterns
        APP_MESSAGE_4026, // Password must be at least 8 characters and contain uppercase, lowercase, number and special character
        APP_MESSAGE_4027, // Medical record already exists for this appointment
        APP_MESSAGE_4028, // Medical record not found
        APP_MESSAGE_4029, // Clinic application request not found
        APP_MESSAGE_4030, // Invalid clinic application status for this action
        APP_MESSAGE_4031, // Invalid OTP code
        APP_MESSAGE_4032, // OTP code has expired
        APP_MESSAGE_4033, // Authenticated user information is invalid
        APP_MESSAGE_4034, // Clinic associated with current user not found
        APP_MESSAGE_4035, // Specified feedback not found
        APP_MESSAGE_4036, // Feedback does not belong to the authenticated clinic
        APP_MESSAGE_4037, // Feedback deleted successfully
        APP_MESSAGE_4039, // Current password is incorrect
        APP_MESSAGE_4040, // New password must be different from current password
        APP_MESSAGE_4041, // Service name already exists in this clinic
        APP_MESSAGE_4043, // Identity number already exists in the system.
        APP_MESSAGE_4042, // Password and confirm password do not match
        APP_MESSAGE_4044, // Specified service not found or does not belong to this clinic
        APP_MESSAGE_4045, // Clinic Not Found or Inactive
        APP_MESSAGE_4046, // Appointment not found in the system
        APP_MESSAGE_4047, // Appointment already cancelled
        APP_MESSAGE_4048, // Cannot cancel completed appointment
        APP_MESSAGE_4049, // Cannot cancel appointment in progress
        APP_MESSAGE_4050, // Cannot cancel appointment within 24 hours of appointment time
        APP_MESSAGE_4051, // Appointment cannot be cancelled in current status
        APP_MESSAGE_4052, // Slot not found
        APP_MESSAGE_4053, // Patient does not have permission to cancel this appointment
        APP_MESSAGE_4099, // Demographics record already exists for this patient (UC36 EX-01)
        // Server error codes (5xxx series)
        APP_MESSAGE_5000, // General internal server error (unexpected exception)
        APP_MESSAGE_5001, // Database operation failed (e.g., connection issue or query error)
        APP_MESSAGE_5002, // Service is temporarily unavailable (e.g., maintenance or overload)
        APP_MESSAGE_5003  // Error from external service (e.g., API integration failure)
    }
}
