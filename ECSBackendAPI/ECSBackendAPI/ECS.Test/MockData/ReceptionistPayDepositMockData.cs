using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistPayDepositServices.Tests
{
    public static class ReceptionistPayDepositMockData
    {
        public static readonly Guid ValidAppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static DateTime GetTodayVnDateTime()
        {
            var todayVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;
            return todayVn.AddHours(9);
        }

        public static Appointment GetValidAppointment()
        {
            return new Appointment
            {
                Id = ValidAppointmentId,
                AppointmentDate = GetTodayVnDateTime(),
                DepositPaid = false,
                Status = AppointmentStatus.CONFIRMED,
                UpdatedAt = DateTime.UtcNow.AddHours(-1)
            };
        }

        public static ReceptionistPayDepositRequest GetValidRequest()
        {
            return new ReceptionistPayDepositRequest
            {
                AppointmentId = ValidAppointmentId
            };
        }
    }
}