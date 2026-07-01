using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.GetClinicLookupServices
{
    /// <summary>
    /// Response presentation schema containing minimized metadata identifiers for system clinics.
    /// </summary>
    public class GetClinicLookupResponse
    {
        /// <summary>
        /// Unique Identifier tracking the system clinic record entry.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The explicit clean presentation text title tracking the target clinic name.
        /// </summary>
        public string Name { get; set; } = null!;
    }
}
