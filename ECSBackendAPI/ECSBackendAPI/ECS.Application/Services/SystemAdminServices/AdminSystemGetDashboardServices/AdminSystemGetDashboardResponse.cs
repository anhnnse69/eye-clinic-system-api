using System.Text.Json.Serialization;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemGetDashboardServices
{
    /// <summary>
    /// Response object containing aggregated system metrics and data tracking structures.
    /// </summary>
    public class AdminSystemGetDashboardResponse
    {
        public TotalSystemAccountsDto TotalSystemAccounts { get; set; } = null!;
        public OperationalClinicsDto OperationalClinics { get; set; } = null!;
        public AppointmentsDto Appointments { get; set; } = null!;
        public int RegisteredPatients { get; set; }
        public List<PendingClinicDto> PendingClinics { get; set; } = null!;
        public List<TopServiceDto> TopServices { get; set; } = null!;

        /// <summary>
        /// Data transfer object for system user account metrics.
        /// </summary>
        public class TotalSystemAccountsDto
        {
            public int Total { get; set; }
            public int Doctor { get; set; }
            public int Receptionist { get; set; }
            public int ClinicAdmin { get; set; }
            public int SystemAdmin { get; set; }
        }

        /// <summary>
        /// Data transfer object for active and total clinic configurations.
        /// </summary>
        public class OperationalClinicsDto
        {
            public int Active { get; set; }
            public int Total { get; set; }
        }

        /// <summary>
        /// Data transfer object for detailed appointment metrics by status.
        /// </summary>
        public class AppointmentsDto
        {
            public int Total { get; set; }
            public int Pending { get; set; }
            public int DepositPaid { get; set; }
            public int Booked { get; set; }
            public int Arrived { get; set; }
            public int InProgress { get; set; }
            public int Completed { get; set; }
            public int Cancelled { get; set; }
            public int NoShow { get; set; }
        }

        /// <summary>
        /// Data transfer object for pending clinic application rows.
        /// </summary>
        public class PendingClinicDto
        {
            public string Id { get; set; } = null!;
            public string Name { get; set; } = null!;

            /// <summary>
            /// Property explicitly mapped to 'Owner' to align with frontend consumer expectations.
            /// </summary>
            [JsonPropertyName("Owner")]
            public string Owner { get; set; } = null!;
            public string Date { get; set; } = null!;
        }

        /// <summary>
        /// Data transfer object for the top-performing service distribution.
        /// </summary>
        public class TopServiceDto
        {
            public string Name { get; set; } = null!;
            public int Count { get; set; }
            public string Growth { get; set; } = "0%";
        }
    }
}