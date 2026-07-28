using System;
using System.Collections.Generic;
using ECS.Application.Services.SystemAdminServices.AdminSystemCreateClinicAdminServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemUpdateClinicServices;
using ECS.Application.Services.SystemAdminServices.ClinicManagementServices;
using ECS.Application.Services.SystemAdminServices.ClinicRegisterServices;
using ECS.Application.Services.SystemAdminServices.GetClinicLookupServices;
using ECS.Domain.Entities.Clinics;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Reusable mock data for System Admin clinic approval, lookup, and management unit tests.
    /// </summary>
    public static class SystemAdminClinicMockData
    {
        public static Guid ApplicationId =>
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        public static Guid ClinicId =>
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        public static Guid SecondaryClinicId =>
            Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        public static Guid AdminUserId =>
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        public static Guid NotFoundClinicId =>
            Guid.Parse("99999999-9999-9999-9999-999999999999");

        public static ClinicRegistrationRequest GetPendingApplication() => new()
        {
            Id = ApplicationId,
            ClinicName = "Saigon Eye Clinic",
            ClinicAddress = "123 Nguyen Hue, District 1, HCMC",
            ContactName = "Nguyen Van A",
            ContactPhone = "0901234567",
            ContactEmail = "contact@saigoneye.vn",
            BusinessLicenseUrl = "https://example.com/license.pdf",
            Status = "PENDING",
            RequestedAt = DateTime.UtcNow.AddDays(-2)
        };

        public static ClinicRegistrationRequest GetApprovedApplication()
        {
            var application = GetPendingApplication();
            application.Status = "APPROVED";
            application.ReviewedBy = AdminUserId;
            application.ReviewedAt = DateTime.UtcNow.AddDays(-1);
            return application;
        }

        public static ClinicRegistrationRequest GetRejectedApplication()
        {
            var application = GetPendingApplication();
            application.Status = "REJECTED";
            application.ReviewNote = "Incomplete documents";
            application.ReviewedBy = AdminUserId;
            application.ReviewedAt = DateTime.UtcNow.AddDays(-1);
            return application;
        }

        public static Clinic GetClinicWithPublicationRequest() => new()
        {
            Id = ClinicId,
            Name = "Published Eye Clinic",
            Address = "456 Le Loi, District 1, HCMC",
            Phone = "02873001234",
            Email = "clinic@example.com",
            IsActive = true,
            IsPublished = false,
            IsPublicationRequested = true,
            PublicationRequestedAt = DateTime.UtcNow.AddDays(-1),
            RatingAvg = 4.5m,
            ReviewCount = 10,
            CreatedAt = DateTime.UtcNow.AddYears(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        public static Clinic GetPublishedClinic()
        {
            var clinic = GetClinicWithPublicationRequest();
            clinic.IsPublished = true;
            clinic.IsPublicationRequested = false;
            return clinic;
        }

        public static Clinic GetClinicWithoutPublicationRequest()
        {
            var clinic = GetClinicWithPublicationRequest();
            clinic.IsPublicationRequested = false;
            clinic.PublicationRequestedAt = null;
            return clinic;
        }

        public static Clinic GetInactiveClinic() => new()
        {
            Id = SecondaryClinicId,
            Name = "Closed Eye Clinic",
            Address = "789 Tran Hung Dao, District 5, HCMC",
            Phone = "02873009999",
            Email = "closed@example.com",
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddYears(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };

        public static CreateClinicAdminRequest GetValidCreateClinicAdminRequest() => new()
        {
            ClinicId = ClinicId,
            FullName = "Clinic Admin User",
            Phone = "0911555666",
            Email = "clinic-admin@ECS.vn"
        };

        public static Clinic GetClinicWithFullDetails()
        {
            var clinic = GetClinicWithPublicationRequest();
            clinic.Description = "Leading eye care provider in HCMC.";
            clinic.LogoUrl = "https://example.com/logo.png";
            return clinic;
        }

        public static Clinic GetClinicWithNullOptionalFields() => new()
        {
            Id = ClinicId,
            Name = "Minimal Eye Clinic",
            Address = "100 Test Street",
            Phone = "02870000000",
            Email = null,
            LogoUrl = null,
            Description = null,
            IsActive = true,
            RatingAvg = null,
            ReviewCount = null,
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        #region Update Clinic Mock Data

        /// <summary>
        /// Request cập nhật đầy đủ thông tin (chứa khoảng trắng ở đầu/cuối để test hàm .Trim()).
        /// </summary>
        public static UpdateClinicRequest GetValidUpdateClinicRequest() => new()
        {
            Name = "  Updated Eye Clinic Name  ",
            Address = "  999 New Street, District 1, HCMC  ",
            Phone = "  0988776655  ",
            Email = "  updated@saigoneye.vn  ",
            LogoUrl = "https://example.com/new-logo.png",
            Description = "  Updated description for eye care center.  "
        };

        /// <summary>
        /// Request với các trường tùy chọn bằng null (để kiểm tra nhánh ?.Trim()).
        /// </summary>
        public static UpdateClinicRequest GetUpdateClinicRequestWithNulls() => new()
        {
            Name = "Updated Minimal Clinic",
            Address = "100 Test Street Updated",
            Phone = "02870000000",
            Email = null,
            LogoUrl = null,
            Description = null
        };

        #endregion

        #region Get Clinics Mock Data

        public static GetClinicsRequest GetDefaultGetClinicsRequest() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            Status = null,
            SearchTerm = null
        };

        public static Clinic GetClinicWithNullPhoneAndEmail() => new()
        {
            Id = Guid.Parse("12345678-1234-1234-1234-1234567890ab"),
            Name = "No Contact Info Clinic",
            Address = "000 Unknown Street",
            Phone = null,
            Email = "",
            IsActive = true,
            IsPublished = false,
            IsPublicationRequested = false,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        public static List<Clinic> GetListOfClinicsForGetClinics() => new()
        {
            GetClinicWithFullDetails(),      // IsActive = true, Phone & Email hợp lệ
            GetInactiveClinic(),             // IsActive = false, Phone & Email hợp lệ
            GetClinicWithNullPhoneAndEmail() // IsActive = true, Phone = null, Email = ""
        };

        #endregion

        #region Get Clinic Applications Mock Data

        public static GetClinicApplicationsRequest GetDefaultGetClinicApplicationsRequest() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            Status = null,
            SearchTerm = null
        };

        public static List<ClinicRegistrationRequest> GetListOfClinicRegistrationRequests() => new()
        {
            // Application 1: PENDING, RequestedAt = -2 days, Name = "Saigon Eye Clinic"
            GetPendingApplication(),

            // Application 2: APPROVED, RequestedAt = -1 day (Mới nhất), Name = "Hanoi Dental Care"
            new ClinicRegistrationRequest
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ClinicName = "Hanoi Dental Care",
                ClinicAddress = "456 Hoan Kiem, Hanoi",
                ContactName = "Nguyen Van B",
                ContactPhone = "0908888999",
                ContactEmail = "contact@hanoidental.vn",
                BusinessLicenseUrl = "https://example.com/license2.pdf",
                Status = "APPROVED",
                RequestedAt = DateTime.UtcNow.AddDays(-1)
            },

            // Application 3: PENDING, RequestedAt = -3 days (Cũ nhất), Name = "Da Nang Health Center"
            new ClinicRegistrationRequest
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ClinicName = "Da Nang Health Center",
                ClinicAddress = "789 Hai Chau, Da Nang",
                ContactName = "Tran Van C",
                ContactPhone = "0907777666",
                ContactEmail = "info@dananghealth.vn",
                BusinessLicenseUrl = "https://example.com/license3.pdf",
                Status = "PENDING",
                RequestedAt = DateTime.UtcNow.AddDays(-3)
            }
        };

        #endregion

        #region Get Clinic Lookup Mock Data

        public static readonly Guid LookupClinic1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid LookupClinic2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static GetClinicLookupRequest GetLookupRequest() => new();

        /// <summary>
        /// Mock danh sách phòng khám phục vụ GetClinicLookupService bao gồm đầy đủ các nhánh điều kiện:
        /// - Clinic A: Active, StaffClinics = null (Thỏa mãn)
        /// - Clinic B: Active, StaffClinics = empty (Thỏa mãn)
        /// - Clinic C: Active, StaffClinics có nhân viên (Bị loại)
        /// - Clinic D: Inactive (Bị loại)
        /// </summary>
        public static List<Clinic> GetSampleClinicsForLookup() => new()
        {
            new Clinic
            {
                Id = LookupClinic2Id,
                Name = "B Clinic",
                IsActive = true,
                StaffClinics = new List<StaffClinic>()
            },
            new Clinic
            {
                Id = LookupClinic1Id,
                Name = "A Clinic",
                IsActive = true,
                StaffClinics = null
            },
            new Clinic
            {
                Id = Guid.NewGuid(),
                Name = "C Clinic",
                IsActive = true,
                StaffClinics = new List<StaffClinic> { new StaffClinic() }
            },
            new Clinic
            {
                Id = Guid.NewGuid(),
                Name = "D Clinic",
                IsActive = false,
                StaffClinics = new List<StaffClinic>()
            }
        };

        #endregion
    }
}