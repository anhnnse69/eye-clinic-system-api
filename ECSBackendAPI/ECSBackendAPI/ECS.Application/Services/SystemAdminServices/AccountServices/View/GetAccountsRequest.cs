using ECS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AccountServices.View
{
    /// <summary>
    /// Request criteria parameter object for retrieving and filtering a paged collection of user accounts.
    /// </summary>
    public class GetAccountsRequest
    {
        /// <summary>
        /// Optional keyword to search against users' full name or phone number.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Optional role filter constraint criteria parameter details.
        /// </summary>
        public UserRole? Role { get; set; }

        /// <summary>
        /// Optional status filter tracking active/inactive user flags.
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// The active index page position mapping boundary indicator.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// The maximum sizing boundary constraint capacity layout per page segment.
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}