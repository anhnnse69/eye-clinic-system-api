namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetDailyAppointmentsServices
{
    /// <summary>
    /// Core data structure representing an individual appointment entry row optimized for live grid viewport layout.
    /// </summary>
    public class ReceptionistGetDailyAppointmentsResponse
    {
        /// <summary>
        /// The unique identifier string tracking the specific appointment entry inside backend layers.
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// The localized system identifier tracking the specific patient identity record linkage.
        /// </summary>
        public string PatientId { get; set; } = null!;

        /// <summary>
        /// The professional system identifier tracking the designated practitioner assigned to the appointment.
        /// </summary>
        public string DoctorId { get; set; } = null!;

        /// <summary>
        /// The structural configuration identifier reference pointing to the reserved calendar shift block.
        /// </summary>
        public string SlotId { get; set; } = null!;

        /// <summary>
        /// The calendar validation chronological target date string tracking the occurrence using the 'yyyy-MM-dd' layout.
        /// </summary>
        public string AppointmentDate { get; set; } = null!; // yyyy-MM-dd

        /// <summary>
        /// The descriptive feedback payload documenting entry-level clinical expressions or medical complaints.
        /// </summary>
        public string? Symptoms { get; set; }

        /// <summary>
        /// The dynamic operation execution progress milestone indicator; standardized around configurations such as PENDING, BOOKED, or ARRIVED.
        /// </summary>
        public string Status { get; set; } = null!; // PENDING, BOOKED, ARRIVED, etc.

        /// <summary>
        /// The financial commitment value record processing downstream monetary transaction safety blocks.
        /// </summary>
        public decimal DepositAmount { get; set; }

        /// <summary>
        /// The logical boolean confirmation matrix tracking finalized payment collection processes for the reservation.
        /// </summary>
        public bool DepositPaid { get; set; }

        /// <summary>
        /// The inbound pipeline identifier trace marking the registration entry pathway origin.
        /// </summary>
        public string BookingSource { get; set; } = null!;

        /// <summary>
        /// The nested structural profile information graph describing the associated consumer actor.
        /// </summary>
        public PatientProfileRowDto Patient { get; set; } = null!;

        /// <summary>
        /// The synchronized assignment block exposing active identity telemetry for the evaluating clinician.
        /// </summary>
        public DoctorProfileRowDto Doctor { get; set; } = null!;

        /// <summary>
        /// The temporal timeline reservation metrics detail package enclosing specific schedule block coordinates.
        /// </summary>
        public TimeSlotRowDto Slot { get; set; } = null!;

        /// <summary>
        /// The inline dynamic sequence progression tracker tracking the real-time operational processing status.
        /// </summary>
        public QueueInlineRowDto? Queue { get; set; }
    }

    /// <summary>
    /// Brief structural feedback data confirming mapped patient identity records for presentation layouts.
    /// </summary>
    public class PatientProfileRowDto
    {
        /// <summary>
        /// The unique identity registry identifier key tracking the client record boundary.
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// The synchronized full name payload presentation mapping block.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// The physiological demographic classification identifier for identity verification layers.
        /// </summary>
        public string Gender { get; set; } = null!;

        /// <summary>
        /// The chronological date of birth configuration detail sequence tracking demographic validity.
        /// </summary>
        public string Dob { get; set; } = null!;

        /// <summary>
        /// The secondary telephone communication sequence line record mapping.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// The state-regulated health insurance identification token schema mapping.
        /// </summary>
        public string? BhytNumber { get; set; }
    }

    /// <summary>
    /// Brief structural feedback data confirming matched provider entity records for presentation layouts.
    /// </summary>
    public class DoctorProfileRowDto
    {
        /// <summary>
        /// The unique clinician directory matrix token tracing backend medical profile links.
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// The authenticated professional name presentation string tracking backend records.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// The clinical spatial node allocation identifier tracking the assigned examination zone.
        /// </summary>
        public string ClinicRoomName { get; set; } = null!;
    }

    /// <summary>
    /// Structural presentation data detail block mapping targeted operational execution shifts.
    /// </summary>
    public class TimeSlotRowDto
    {
        /// <summary>
        /// The discrete scheduling track line node identity reference token.
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// The master timeline orchestration record reference mapping daily structural schedules.
        /// </summary>
        public string ScheduleId { get; set; } = null!;

        /// <summary>
        /// The dynamic chronological boundary mark defining the active launch matrix using 'yyyy-MM-ddTHH:mm:ss' layout.
        /// </summary>
        public string StartTime { get; set; } = null!; // yyyy-MM-ddTHH:mm:ss

        /// <summary>
        /// The dynamic chronological boundary mark defining the final completion point using 'yyyy-MM-ddTHH:mm:ss' layout.
        /// </summary>
        public string EndTime { get; set; } = null!;

        /// <summary>
        /// The operational workflow block categorization detailing specific duty segments.
        /// </summary>
        public string ShiftType { get; set; } = null!;
    }

    /// <summary>
    /// Real-time sequential arrangement layout tracker mapping critical workflow execution items.
    /// </summary>
    public class QueueInlineRowDto
    {
        /// <summary>
        /// The individual queue sequence entity trace record registration key.
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// The localized numeric progression allocation integer pointing to the current waiting line order.
        /// </summary>
        public int QueueNumber { get; set; }

        /// <summary>
        /// The dynamic transition state indicator mapping standard progressions like WAITING, CALLING, or COMPLETED.
        /// </summary>
        public string Status { get; set; } = null!; // WAITING, CALLING, COMPLETED

        /// <summary>
        /// The specific system orchestration timestamp marking when the token context was called forward.
        /// </summary>
        public string? CalledAt { get; set; }
    }

    /// <summary>
    /// Internal transfer container storing telemetry metrics counters mapped for real-time dashboard widgets.
    /// </summary>
    public class LiveTrackingStatsDto
    {
        /// <summary>
        /// Total aggregate counter tracing all active entry records loaded within the daily processing scope.
        /// </summary>
        public int TotalDailyAppointments { get; set; }

        /// <summary>
        /// Summation of active transaction metrics tracking arrived consumers present on physical premises.
        /// </summary>
        public int TotalArrivedAndLive { get; set; }

        /// <summary>
        /// Terminal counter compiling finalized patient interaction processes that successfully passed evaluation layers.
        /// </summary>
        public int TotalCompletedExams { get; set; }

        /// <summary>
        /// Voided telemetry registry aggregation checking broken interaction sequences dropped prior to completion.
        /// </summary>
        public int TotalCancelledExams { get; set; }
    }
}